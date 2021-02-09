# What UReact Is

UReact is a framework for creating and managing scene graphs declaratively in the Unity game engine.

### What does that mean?

Rather than explicitly creating `GameObject`s and putting a bunch of `MonoBehaviour`s on them to manage their state and transitions, you instead make a function which essentially takes, as arguments, the current state relevant to the GameObject, and returns how the GameObject should look given that state.

### Why is this better?

The traditional way of building the scene graph is extremely error prone. Since each GameObject is managing it's own state, and the various events that affect it could arrive with hard to predict timing, it's extremely difficult to make sure that it's always in a valid state. The logic often gets absurdly complicated, and it's very easy to miss cases, which result in bugs.

This system allows you to put all the game state in a centralized store, which is a definitive source of truth. There are many ways to manage this centralized state, and UReact isn't particularly opinionated about that, but the important part is that there is one source of truth for what the entire game world looks like at this moment, and UReact updates the scene graph every frame to match that source of truth.

One place where this really shines is with networking. Updates to the state store can come from local interactions, or from the network, and the scene graph will just be updated to match. Only the state update code has to care about the network. A clever state system can automatically synchronize the parts of the state store that are relevant to all users.

### Wait, that sounds like it's rebuilding everything every frame. How can that be performant?

It's not actually rebuilding everything every frame. Rather, it builds up a lightweight representation of the scene graph every frame, which can be done pretty quickly. Then, it compares that to the representation that was built up the previous frame, and applies any changes to the actual scene graph. The result is that only the minimal set of changes are actually applied to the scene graph.

That said, there is still a lot of room for optimization in the process. The UReact library is very new, and it's still quite easy to make things with it that will not perform well, if you're not careful. Contributions and suggestions welcome.

### This sounds a lot like the React web framework. Even the name is similar.

It does. It's loosely based on React. It does not attempt to exactly copy the React API, as a lot of how React works doesn't really translate that well into C#, rather than JavaScript, and Unity's `GameObject`s and `MonoBehaviour`s rather than HTML and CSS. But it does maintain the core principles of React.

That said, there's a lot of room for improvement in UReact's APIs. The library is very new, and some things could certainly be a lot better. Contributions and suggestions welcome.

### Wait, isn't React a UI framework? Why are you using it for the scene graph?

It turns out UI hierarchies and game scene graphs have a lot in common. They're both hierarchies of nodes thats group together related elements, which often are also spatially grouped. So a system like React that's designed to manage a UI hierarchy also works surprisingly well for managing a scene graph.

# How to Install UReact

First, UReact makes use of C#8 features, which means that it requires at least Unity 2020.2 or later.

There are two separate libraries included. They are:

* UReact - The core library that defines UReact itself, but includes no components.
* UReactUnityComponents - An incomplete set of UReact components representing some of the most commonly used Unity components. This is technically optional, but you probably want it.

If you just want UReact itself, copy the `Assets/UReact/` directory to somewhere inside your `Assets/` directory. If you also want UReactUnityComponents, then also copy the `Assets/UReactUnityComponents/` directory to the same place.

It's recommended that you create a directory for all third party libraries, like say `Assets/ThirdParty/`, and put these directories in there, but the exact organization is up to you.

# How to Use UReact

This section serves as a sort of tutorial overview for how to use the library, and will be gradually building up the example located in `Assets/Examples/DragAndDrop/`. The full source for this example is located there.

Core to UReact is the relationship between components, nodes, elements and `GameObject`s. A UReact component represents a Unity `Component`, and a UReact node represents a Unity `GameObject`. Components and nodes are the classes you will write that drive UReact. These are equivalent to React's components. Elements come in two varieties, the `NodeElem` and the `CompElem`, which are respectively the lightweight, intermediate representations of Unity's `GameObject`s and `Component`s.

A `NodeElem` stores the node's unique key, it's list of components (as `CompElem`s), and it's list of children (as more `NodeElem`s). A `CompElem` stores the component's `Render` function and properties for a component. That `Render` function serves the purpose of generating or updating the actual `Component` on the `GameObject`, based on changes to it's properties.

The first thing UReact does each frame is build the graph of elements based on the root node you pass it. Then it analyzes that graph to figure out which `GameObject`s should be created, destroyed or moved, and then calls the `Render` function for each `CompElem` on each `Node` to update every `GameObject` that remains in the scene.

The example we're going to build up involves having three cubes, of different sizes, which you can click on to drag around. Before we dive into the UReact parts of this, let's define the centralized state structure. It's generally recommended that any UReact project store the entire world state in one place. This structure may be large and complicated, but it's none the less very helpful to have it all centralized. Ideally, there's also a system for propogating updates into the state to maintain consistency, instead of just updating it directly, but that's beyond the scope of what UReact does so we'll skip that part in this example.

The state we'll be using is pretty simple:

```c#
#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace State {
  public class Draggable {
    public float size;
    public Vector3 position;
  }

  public class Store {
    public Dictionary<Guid, Draggable> objects = new Dictionary<Guid, Draggable>();
    public Guid? heldObject;
  }
}
```

The `Draggable` class represents one draggable object, with a size and a position. The `Store` class is the root of our state, and has a dictionary of `Draggable`s indexed by ID, and also stores the ID of the object currently being dragged, if there is one.

In order to make anything actually happen, we need at least one actual `MonoBehaviour`, so we can initialize our state in `Start`, and update it in `Update`. It's recommended that you have exactly one such `MonoBehaviour` which serves as the entry point for all initialization and updates. We call this class a dispatcher, because it recieves all Unity events and dispatches them where they're needed. All per-frame updates should go through this dispatcher, rather than being handled in the scene graph. In this example, though, the updates are so simple that the dispatcher will handle them directly.

Something like this:

```c#
#nullable enable
using System;
using UnityEngine;

public class UReactDispatcher : MonoBehaviour {
  private State.Store state = new State.Store();

  void Start() {
    state.objects[Guid.NewGuid()] = new State.Draggable() {
      position = new Vector3(0, 0, 0),
      size = 1.5f,
    };
    state.objects[Guid.NewGuid()] = new State.Draggable() {
      position = new Vector3(3, 0, 0),
      size = 2,
    };
    state.objects[Guid.NewGuid()] = new State.Draggable() {
      position = new Vector3(-3, 0, 0),
      size = 1f,
    };
  }

  void Update() {
    if (state.heldObject != null) {
      var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
      var groundPos = ray.origin - ray.direction * (ray.origin.y / ray.direction.y);
      state.objects[state.heldObject.Value].position = groundPos;
    }
  }
}
```

As you can see, this instantiates the state, adds three draggables to it in `Start`, and in `Update`, it checks if there is a draggable currently held, and if there is, it moves it to where the cursor's position intersects the ground plane.

By convention, a node is a static class with a single `New` function on it. That function takes any parameters it needs to construct it's portion of the scene graph, and returns a new `NodeElem`. Here's the `DraggableNode` for this example:

```c#
#nullable enable
using System;
using UnityEngine;
using UReact;

public static class DraggableNode {
	public static NodeElem New(
		string key,
		Vector3 position,
		float size,
		Action onClick,
		Material material,
		Mesh mesh
	) =>
		new NodeElem(
			key
		).Component(
			new TransformComponent(
				localPosition: position,
				localScale: Vector3.one * size)
		).Component(
			new MeshRendererComponent(
				material: material
			)
		).Component(
			new MeshFilterComponent(
				mesh: mesh
			)
		).Component(
			new BoxColliderComponent(
				center: Vector3.zero,
				size: Vector3.one * size
			)
		).Component(
			new ClickableComponent(
				onClick: onClick
			)
		);
}
```

Note that the argument passed to `new NodeElem()` must be a unique key, across the entire portion of the scene graph managed by this `UReact.Renderer`. This key is how UReact maintains the identity of the node from one frame to the next, even if it moves to a different place in the scene graph hierarchy.

The UReactUnityComponents library comes with a set of UReact components representing some of the more commonly used Unity components, which is where `TransformComponent`, `MeshRendererComponent`, `MeshFilterComponent` and `BoxColliderComponent` above come from. It's by no means a complete set, and they do not provide access to the full set of properties on these components. It's guided by the set of things that are needed by the earliest consumers of the library. Contributions welcome.

The `ClickableComponent` is one we have to implement ourselves for this example. It wraps a custom `MonoBehaviour` called, unsurprisingly, `Clickable`. The `Clickable` class is very simple:

```c#
#nullable enable
using System;
using UnityEngine;

public class Clickable : MonoBehaviour {
	public Action? onClick;

	public void OnMouseUpAsButton() {
		if (onClick != null) {
			onClick();
		}
	}
}
```

As you can see, it's just a `MonoBehavior` that stores an `Action` and calls it when it's `OnMouseUpAsButton` event is triggered. But to use this from our UReact scene graph, we need a UReact component to represent it. Hence we make the `ClickableComponent`:

```c#
#nullable enable
using System;
using UnityEngine;

public struct ClickableComponent : UReact.Component {
	private Action onClick;

	public ClickableComponent(Action onClick) {
		this.onClick = onClick;
	}

	public void Render(GameObject obj, UReact.Component? oldComp) {
		if (oldComp == null) {
			var clickable = obj.AddComponent<Clickable>();
			clickable.onClick = onClick;
		}
	}

	public Type[] GetManagedBehaviourTypes() {
		return new Type[] { typeof(Clickable) };
	}
}
```

A UReact component must implement the `UReact.Component` interface, which includes the `Render` and `GetManagedBehaviourTypes` functions you see here. The `Render` function is where the real work happens. It's given the `GameObject` on which this component operates, and another `UReact.Component`, called `oldComp`, which is the version of the component from the previous frame. If `oldComp` is `null`, that means this is the first frame on which it's existed, and the Unity component needs to be initialized.

If `oldComp` is not `null`, then we can compare the fields from last frame to the ones on this frame, and update the actual component if necessary. In this case, the only field is the `onClick`, which we won't be updating, partially because there's no useful way to compare an `Action` to another `Action`. Unless they're literally the same object, they will always compare as different, even if they're effectively identical. And since the `onClick` function will be created in the UReact scene graph, it will always be a different object.

Next, we need the root node. This will manage the list of `DraggableNode`s, as so:

```c#
#nullable enable
using UnityEngine;
using UReact;

public static class RootNode {
	public static NodeElem New(State.Store state, Material material, Mesh mesh) {
		// Create an empty node, as a parent object to organize all the draggable objects together
		var root = new NodeElem("Draggable Objects");

		// Iterate through each of the draggables in the state, to make a child node from each
		foreach (var keyval in state.objects) {
			var id = keyval.Key;
			var draggable = keyval.Value;

			// Use the `Child` function to add a child to the root node
			root.Child(
				DraggableNode.New(
					key: $"Draggable {id}",
					position: draggable.position,
					size: draggable.size,
					onClick: () => {
						if (state.heldObject == null) {
							state.heldObject = id;
						} else {
							state.heldObject = null;
						}
					},
					material: material,
					mesh: mesh
				)
			);
		}

		return root;
	}
}
```

This comes out a little bit less clean than the `DraggableNode`, because we need to build the list of children dynamically from another list. First, we create an empty `NodeElem`, representing a `GameObject` with no components on it, called `Draggable Objects`. We're using this purely for organizational purposes. Then, we iterate through each draggable object state on the `State.Store`, and add a child for it with the `Child` function on `NodeElem`, which adds a child node, and pass in a newly created `NodeElem` from `DraggableNode.New`, which we defined above.

The final piece of code which makes this all work is the `Renderer`. This is the UReact class which does the heavy lifting of turning our tree of nodes into the actual Unity scene graph. It's usage is very simple. When the scene is first created, you create your renderer by calling `new Renderer()`. Then, each frame, simply call `renderer.Render(rootNode)`, where `rootNode` is the root `ElemNode` of your scene graph.

Note that UReact can absolutely have multiple `Renderer`s handling different parts of the scene graph in parallel, or one `Renderer` handling the whole thing. It can also handle owning just a portion of the scene graph, while other parts of the scene graph are run in other ways. Note that our example does not hand control over the light or camera to UReact. You absolutely could do that, and in many cases that would be appropriate. However, in this case the light and camera never move, so it's easier to just create them in the Unity editor.

That said, any part of the scene graph that a particular `Renderer` is managing should never be manipulated by anything other than that `Renderer`. The `Renderer` assumes it has total control and ownership of it's `GameObject`s, and if they start changing from the outside things may get very confused.

The job of instantiating and updating the `Renderer` falls to our `UReactDispatcher`. We'll need to add a few things to it to make this work.

```c#
#nullable enable
using System;
using UnityEngine;

public class UReactDispatcher : MonoBehaviour {
  public Material? draggableMaterial;

  private State.Store state = new State.Store();
  private UReact.Renderer ureact = new UReact.Renderer();
  private Mesh? cubeMesh;

  void Start() {
    // clipped ... initializing state

    cubeMesh = BuildCubeMesh();
  }

  void Update() {
    // clipped ... updating state

		ureact.Render(RootNode.New(
			state: state,
			material: draggableMaterial ?? throw new Exception("Draggable material is null"),
			mesh: cubeMesh ?? throw new Exception("Draggable mesh is null")
		));
  }

  private Mesh BuildCubeMesh() {
    // clipped ... building a cube mesh, which is verbose but not interesting
    // see the actual example code if you want to see this function
  }
}
```

Now `Start()` also creates the cube mesh that our draggables will use. We also added a `Material` variable to the `UReactDispatcher`, since our draggables also need a `Material` to render, and that has to come from somewhere, and the `UReact.Renderer` is stored here as well. And the `Update()` function now calls `Render` on our renderer, passing in a new `NodeElem` from our `RootNode`.

The final step is to setup the Unity scene. Make an empty scene, make sure the camera and light are positioned in a reasonable place, and then create an empty `GameObject` and put the `UReactDispatcher` on it, and set it's "Draggable Material" field to some material, probably just "Default-Material". Then hit play, and drag some cubes around!

Hopefully this sufficiently illustrates how to build things with UReact. If you want more help, check out UReact Discord at https://discord.gg/rdtBM77

# Contributing

Pull requests welcome!

To contribute to UReact, follow the standard GitHub process: create a fork, make your changes, and then make a pull request. It will be reviewed, and some alterations may be request, but if and when it's aproved, it'll be integrated.

Please follow the coding style already in use.

Any substantial new functionality or redesigns should get an issue first, so it can be opened to discussion before you put in a lot of work on it. Nothing is worse than putting a ton of work into a change only to have it refused because it's contrary to the vision of the project.

Bug fixes or small backward-compatible improvements you can just submit.

# License

This is licensed under the MIT license. See the LICENSE file.

This should be plenty permissive for any reasonable use case, but if it's a problem for you, please make an issue describing why, so we can discuss alternatives.