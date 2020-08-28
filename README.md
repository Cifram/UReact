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

It turns out that most UI systems, including HTML, are scene graphs, even if they use the scene graph a little differently.

Game scene graphs are usually (though not always) in 3D, and tend to be shallow with a few root nodes that either have no children or tons of children, and only a few places where the graph goes any deeper than that. The parent-child relationship tends to represent inherited transforms, or just simple organization to group lots of related objects together.

UI scene graphs are usually (though not always) in 2D, with very deep, complicated hierarchies. The parent-child relationship tends to represent nesting, with the idea of the child being "inside" the parent element, which also implies inherited transforms.

None the less, how they're structured is very similar, and they can both benefit from a React-like framework. In fact, there is a separate library planned of UReact components for working with the UnityUI system.

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

The example we're going to build up involves having three cubes, of different sizes, which you can click on to drag around. Before we dive into the UReact parts of this, let's define the centralized state structure:

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

In order to make anything actually happen, we need at least one actual `MonoBehaviour`, so we can initialize our state in `Start()`, and update it in `Update`. Something like this:

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

As you can see, this instantiates the state, adds three draggables to it in `Start()`, and in `Update()`, it checks if there is a draggable currently held, and if there is, it moves it to where the cursor's position intersects the ground plane.

Now, we want to have a UReact node to represent a draggable object. By convention, each node is composed of two data types:

* Props struct - A struct containing all of the properties the node needs. This traditionally has the suffix `Props`, like `DraggableProps`.
* Node class - A static class with a single static method on it called `New`. This class traditionally has the `Node` suffix, like `DraggableNode`. The `New` method takes as an argument the props struct, and possibly a string key, and returns a `NodeElem`, describing a `GameObject` with it's components and children.

Putting this together for our draggable object, we end up with something like this:

```c#
#nullable enable
using System;
using UnityEngine;
using UReact;

public struct DraggableProps {
  public string key;
  public Vector3 position;
  public float size;
  public Material material;
  public Mesh mesh;
}

public static class DraggableNode {
  public static NodeElem New(DraggableProps props) =>
    new NodeElem(
      props.key
    ).Component(
      typeof(Transform),
      TransformComponent.Render,
      new TransformProps { position = props.position, localScale = Vector3.one * props.size }
    ).Component(
      typeof(MeshRenderer),
      MeshRendererComponent.Render,
      new MeshRendererProps { material = props.material }
    ).Component(
      typeof(MeshFilter),
      MeshFilterComponent.Render,
      new MeshFilterProps { mesh = props.mesh }
    ).Component(
      typeof(BoxCollider),
      BoxColliderComponent.Render,
      new BoxColliderProps { center = Vector3.zero, size = Vector3.one * props.size }
    ).Component(
      typeof(Clickable),
      ClickableComponent.Render,
      new ClickableProps { onClick = props.onClick }
    );
}
```

Note that the argument passed to `new NodeElem()` must be a unique key, across the entire portion of the scene graph managed by this `UReact.Renderer`. This key is how UReact maintains the identity of the node from one frame to the next, even if it moves to a different place in the scene graph hierarchy.

Now, you might be wondering why we use a props struct, instead of just passing all this data in to `New` as arguments. Well, you can do that, if you like, and it'll work fine. This reason for this convention is for consistency with components, which need to have a props structure because that structure is saved, so they can be passed the props structure from the previous frame to diff, to decide what they need to change.

The UReactUnityComponents library comes with a set of UReact components representing some of the more commonly used Unity components, which is where `TransformComponent`, `MeshRendererComponent`, `MeshFilterComponent` and `BoxColliderComponent` above come from. It's by no means a complete set, and they do not provide access to the full set of properties on these components. It's guided by the set of things that are needed by the earliest consumers of the library. Contributions welcome.

Next, we need the root node. This will manage the list of `DraggableNode`s, as so:

```c#
#nullable enable
using System.Linq;
using UnityEngine;
using UReact;

public struct RootProps {
  public State.Store state;
  public Material material;
  public Mesh mesh;
}

public static class RootNode {
  public static NodeElem New(RootProps props) {
    var root = new NodeElem("Draggable Objects");
    foreach (var keyval in props.state.objects) {
      var id = keyval.Key;
      var draggable = keyval.Value;
      root.Child(DraggableNode.New(new DraggableProps {
        key = $"Draggable {id}",
        position = draggable.position,
        size = draggable.size,
        material = props.material,
        mesh = props.mesh,
      }));
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
  private UReact.Renderer? ureact;
  private Mesh? cubeMesh;

  void Start() {
    // clipped ... initialization state

    cubeMesh = BuildCubeMesh();
    ureact = new UReact.Renderer();
  }

  void Update() {
    // clipped ... updating state

    ureact.Render(RootNode.New(new RootProps {
      state = state,
      material = draggableMaterial ?? throw new Exception("Draggable material is null"),
      mesh = cubeMesh ?? throw new Exception("Draggable mesh is null"),
    }));
  }

  private Mesh BuildCubeMesh() {
    // clipped ... building a cube mesh, which is verbose but not interesting
    // see the actual example code if you want to see this function
  }
}
```

Now `Start()` also creates the renderer, and the cube mesh that our draggables will use. We also added a `Material` variable to the `UReactDispatcher`, since our draggables also need a `Material` to render, and that has to come from somewhere. And the `Update()` function now calls `Render` on our renderer, passing in a new `NodeElem` from our `RootNode`.

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