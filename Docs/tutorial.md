So to start, let's say we want to put an `GameObject` in the scene graph with a mesh and a material on it. A Unity `GameObject` is represented by UReact with what we call a node. An example of a node that does this would be as simple as:

```c#
// MeshNode.cs
#nullable enable
using UnityEngine;

public static class MeshNode {
  public static UReact.NodeElem New(string id, Material material, Mesh mesh) {
    return new NodeElem(
      key: $"mesh-{id}"
    ).Component(
      new MeshRendererComponent(
        material: material
      )
    ).Component(
      new MeshFilterComponent(
        mesh: mesh
      )
    );
  }
}
```

A node can really be any function that returns a `UReact.NodeElem`, but by convention we usually make a static class to house that function, and call it `New`. So this node can be created by calling `MeshNode.New()`.

In order to turn this node into an actual `GameObject` in the scene, we need to use the `UReact.Renderer` class. This is typically housed in a class we call a dispatcher, which is a Unity `MonoBehaviour`. For example:

```c#
// Dispatcher.cs
#nullable enable
using UnityEngine

public class Dispatcher : MonoBehaviour {
  public Mesh mesh;
  public Material material;

  private UReact.Renderer renderer = new UReact.Renderer();

  void Update() {
    renderer.Render(MeshNode.New(
      key: "root",
      mesh: mesh,
      material: material
    ))
  }
}
```

Now, put these two files in a Unity project. Also add the `UReact` and `UReactUnityComponents` directories to the project, as per the installation instructions. Then, create an empty `GameObject` in the scene, add the `Dispatcher` component to it, and put whatever mesh and material you'd like on the `Mesh` and `Material` properties of the `Dispatcher`, and hit Play.

Congradulations! You've just creates the UReact equivalent of a Hello World.

So far, this doesn't seem very valuable. After all, wouldn't it have been easier to just directly put a `MeshRenderer` and `MeshFilter` on the `GameObject`, and set the mesh and material on those? Absolutely it would! But UReact's benefit is in mitigating complexity, so you need to have something big enough to actually have some complexity before it properly shines.

Before we scale up the example, though, let's talk a little bit more about what we just did.

A Node, as we mentioned, is just a function that returns a `NodeElem`. So what is a `NodeElem`? Well, it's a very simple representation of a `GameObject`. It's cheap and easy to construct, so we can rebuild it every frame. When you pass a `NodeElem` to `renderer.Render()`, it does one of two things:

1. If it's the first time that `Render` has been called on this `renderer`, it makes the `GameObject` that this `NodeElem` describes.
2. Otherwise, it compares this `NodeElem` to the one that it was given the last time it was called, and updates the `GameObject` and it's Unity components as necessary.

Over in `MeshNode.New`, the first couple lines call `new NodeElem`, and pass in a key. This key is important. It's a unique identifier for the specific node being constructed. When dealing with a UReact scene with more than one node in it, this key is how UReact will tell which `GameObject` is associated with which node from one frame to the next. So it's important that the key both be entirely unique, that is two nodes can't have the same key on the same frame, and that it persist, that is, that the same node is given the same key from one frame to the next.

After that, `Component` is called on `NodeElem` twice. Note that the `Component` function also returns the `NodeElem` it's called on, so the calls can be chained. `Component` takes one argument, which is a `UReact.Component`. Just as a UReact node represents a Unity `GameObject`, a UReact component represents a Unity component. So this creates lightweight representations of a `MeshRenderer` and a `MeshFilter`.

By convention, node functions, the `NodeElem` constructor and UReact component constructors are all called with explicit argument names, with one line per argument. The reason for this is that the number of arguments is often rather high, with many optional arguments, which make positional arguments unweildy.

Anyway, this example, while illustrative of some basic principles, is not very interesting. Let's build something a bit more complicated to really flex what UReact can do. We're going to build a system which has a plane and 3 boxes, with the ability to drag those boxes around the plane.

First, we need to have a state object to store the positions of our draggables. UReact works best when paired with a centralized system for tracking world state. This can be as simple as just a class with a bunch of public variables that you stick all the state in, and directly update in the `Update` event on your dispatcher, which is what we'll be doing. Or, it could be a much more involved system with a defined update system that synchronizes over the network. It's up to you to build whatever works best for your program.

Our state is going to look like this:

```c#
// State.cs
#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

public class Draggable {
  public float size;
  public Vector3 position;
}

public class State {
  public Dictionary<Guid, Draggable> objects = new Dictionary<Guid, Draggable>();
  public Guid? heldObject;
}
```

This is pretty simple. Each draggable has a size and a position. The state maintains a list of draggables, indexed by a `Guid`, and an optional `Guid` for the draggable that's currently being dragged, if there is one.

GUID stands for Globally Unique IDentifier, and is a class provided by .NET that will generate a big alphanumeric string that's practically guaranteed to be unique. We could put the draggables in a `List<Draggable>` instead, but the downside of that is that if we ever remove a draggable, the index of the other draggables will change. Remember that every UReact node needs a unique identifier, which is how it maintains object identity. If a bunch of draggables have their indices change, UReact's association of which node goes to which `GameObject` will be changed as well, and it'll end up shuffling all those `GameObject`s around needlessly. Also, if this were to happen while the user was dragging an object, then the ID in `heldObject` might suddenly be wrong and they'd find themselves dragging the wrong thing. For this reason, it's best we have an ID that's actually assigned to the object, rather than implied through it's index in a list, and `Guid` is an easy way to get that.

Mind you, this tutorial will not include adding or removing draggables, but it's good to get into the habit of building your state in a way that's flexible for future changes.

Now, we need a node for the draggable. Let's first get rid of that `MeshNode.cs` we created above, as we won't need it anymore. Instead, we'll add a new `DraggableNode.cs`, which will be similar, but have a little more in it:

```c#
// DraggableNode.cs
#nullable enable
using UnityEngine;

public static class DraggableNode {
  public static UReact.NodeElem New(
    Guid id,
    Vector3 position,
    float size,
    Material material,
    Mesh mesh
  ) {
    return new UReact.NodeElem(
      key: $"draggable {id}"
    ).Component(
      new TransformComponent(
        localPosition: position,
        localScale: Vector3.one * size
      )
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
    );
  }
}
```

So along with being given a mesh and material, the `DraggableNode` is also given a size and position. It also has a `TransformComponent`, and a `BoxColliderComponent`, both of which utilize this size and position. It can't actually be dragged yet, of course. We'll get to that later.

We're going to need multiple `DraggableNode`s, of course. One for each draggable in the state. To make that happen, we'll need to create another node, which we'll call `RootNode`, to house them all as children:

```c#
// RootNode.cs
#nullable enable
using UnityEngine;

public static class RootNode {
  public static UReact.NodeElem New(State state, Material material, Mesh mesh) {
    // Create an empty node, as a parent object to organize all the draggable objects together
    var root = new NodeElem("Draggable Objects");

    // Iterate through each of the draggables in the state, to make a child node from each
    foreach (var keyval in state.objects) {
      var id = keyval.Key;
      var draggable = keyval.Value;

      // Use the `Child` function to add a child to the root node
      root.Child(
        DraggableNode.New(
          id: id,
          position: draggable.position,
          size: draggable.size,
          material: material,
          mesh: mesh
        )
      );
    }

    return root;
  }
}
```

Here we introduce a new function on `NodeElem`, called `Child`. It takes one argument, being another `NodeElem`. In some cases, you may want to instantiate a node elem directly there, or you may want to do as this example does and call another node. Like with the `Component` function, `Child` returns the original `NodeElem`, so when appropriate it can be chained, though that doesn't apply here.

Now let's expand our dispatcher a bit to work with these new nodes:

```c#
// Dispatcher.cs
#nullable enable
using UnityEngine;

public class Dispatcher : MonoBehaviour {
  public Material? draggableMaterial;

  private State state = new State();
  private UReact.Renderer renderer = new UReact.Renderer();
  private Mesh? cubeMesh;

  void Start() {
    state.objects[Guid.NewGuid()] = new Draggable() {
      position = new Vector3(0, 0, 0),
      size = 1.5f,
    };
    state.objects[Guid.NewGuid()] = new Draggable() {
      position = new Vector3(3, 0, 0),
      size = 2,
    };
    state.objects[Guid.NewGuid()] = new Draggable() {
      position = new Vector3(-3, 0, 0),
      size = 1,
    };

    cubeMesh = BuildCubeMesh();
  }

  void Update() {
    renderer.Render(RootNode.New(
      state: state,
      material: draggableMaterial ?? throw new Exception("Draggable material is null"),
      mesh: cubeMesh ?? throw new Exception("Draggable mesh is null")
    ));
  }

  void Mesh BuildCubeMesh() {
    // ... clipped ...
    // The code for building a cube mesh is rather verbose and not very interesting.
    // Look at Assets/Examples/DragAndDrop/Dispatcher.cs to see this function.
  }
}
```

First, notice that we got rid of the public `mesh` member, and replaced with the private `cubeMesh` member, and now we build the cube mesh with `BuildCubeMesh()`, called in `Start`. Our draggables are always using `BoxCollider`, so for now we'll make them always use a cube mesh. We also renamed the public `material` member to `draggableMaterial`, just to be more descriptive. It's goot practice to make whatever assets need to be specified on the dispatcher descriptively labeled, because you may eventually need a bunch of them.

We also added the `state` to the dispatcher, and add a bunch of draggable objects to it in `Start`. This initializes our starting state.

Finally, in `Update`, our call to `renderer.Render` now passes in a `RootNode`, giving it the state, a mesh and a material.

Note that the mesh and material use `??` and throw exceptions. The mesh one should never be hit, but is necessary. Because `cubeMesh` is not initialized directly, it has to be marked as nullable, which means we haven't proven it's not null by the time it's passed into the `RootNode`. The `draggableMaterial`, on the other hand, can always be null because there's no guarantee the public member was actually set in the editor. Remember it's good practice, and not just in UReact, to always have `#nullable enable` on every file, and appropriately mark which variables could be null, so you don't forget to handle them properly.

Now if you hit play, you should see the three cubes sitting there!

Sadly, they don't live up to their name yet, as they aren't actually draggable. Let's fix that.

First, we need to make our draggables respond to clicks, so you can click on them once to pick them up, and click on them again to put them down. This means we need to respond to the `OnMouseUpAsButton` Unity event, which requires a `MonoBehaviour` to catch that event. Fortunately, it can be dirt simple: it just needs to store an `Action` to call when the event occurs, and then call it.

```c#
// Clickable.cs
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

But to actually use this with UReact, we need a matching `ClickableComponent`. Now, we'll learn how to make our own UReact components. It's not hard. You just need to make a struct with implements the `UReact.Component` interface, like so:

```c#
// ClickableComponent.cs
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
    } else if (oldComp is ClickableComponent old && !old.Equals(this)) {
      var clickable = obj.GetComponent<Clickable>();
      if (onClick != old.onClick) {
        clickable.onClick = onClick;
      }
    }
  }

  public Type[] GetManagedBehaviourTypes() {
    return new Type[] { typeof(Clickable) };
  }
}
```

First, by convention, all the properties we need are just passed into the constructor and stored on private members. It's perfectly appropriate for an arbitrary subset of these properties to be made optional, by providing default values, but we only have one property and there's no reason for it to be optional.

Next, the `Render` function is part of the `UReact.Component` interface. It's given the `GameObject` that the Unity component will be put on, and version of this UReact component from the last frame, as `oldComp`.

The first thing every `Render` function does is check if `oldComp` is null. If it is, that means this is the first frame this component has existed, and we need to create the component. Hence, the call to `obj.AddComponent<Clickable>()`.

If it's not null, then we need to update the existing component instead. But first, we need to convert the `UReact.Component` reference to an actual `ClickableComponent` that we can work with (hence `oldComp is ClickableComponent old`), and confirm that it's actually changed at all (hence `!old.Equals(this)`). Note that this comparison is the reason we make UReact components structs instead of classes. Calling `Equals` on a class will check if these are literally the same object, which will never be true when comparing `oldComp` to `this` in `Render`. But calling `Equals` on a struct will compare the value of each member variable, which is what we really want.

So now that we have a `ClickableComponent`, and know that it's changed, we need to call `obj.GetComponent<Clickable>()` to get the existing Unity component, and update it's fields. Since there is only one field, we don't technically need to check that it's changed, but it's useful to illustrate the overall structure of a typical `Render` function which checks each property and sets any that have changed. The main reason for checking is that many of the fields on Unity components are not simple member variables, but are actually properties that have much greater overhead when they're set. There are a few reasons that's not technically needed here, but it doesn't particularly hurt and it's good to be consistent.

The `GetManagedBehaviourTypes` function returns a list of the types of the Unity components this UReact component manages. This is needed so that if the UReact component is ever removed (that is, it's specified on a node on frame, and not specified on the same node the next frame) UReact knows which Unity components to remove from the `GameObject`.

Now, we have the tools we need to make the draggables actually interactive. First, let's update the `DraggableNode` to include a `ClickableComponent`. (We're just going to show the updated `New` function rather than the whole file.)

```c#
  public static NodeElem New(
    Guid id,
    Vector3 position,
    float size,
    Action onClick,
    Material material,
    Mesh mesh
  ) {
    return new NodeElem(
      key: $"draggable {id}"
    ).Component(
      new TransformComponent(
        localPosition: position,
        localScale: Vector3.one * size
      )
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

The key things here are that it now has a new argument, the `Action onClick`, and passes that into the `ClickableComponent`. It doesn't really define the interactivity here, though. It just adds the functionality that the `RootNode` needs to define it. So let's update the `RootNode` to actually use that `onClick`. (Again, just showing the updated `New` function.)

```c#
  public static UReact.NodeElem New(State state, Material material, Mesh mesh) {
    // Create an empty node, as a parent object to organize all the draggable objects together
    var root = new NodeElem("Draggable Objects");

    // Iterate through each of the draggables in the state, to make a child node from each
    foreach (var keyval in state.objects) {
      var id = keyval.Key;
      var draggable = keyval.Value;

      // Use the `Child` function to add a child to the root node
      root.Child(
        DraggableNode.New(
          id: id,
          position: draggable.position,
          size: draggable.size,
          onClick: () => {
            Debug.Log($"Clicked on draggable {id}");
          },
          material: material,
          mesh: mesh
        )
      );
    }

    return root;
  }
```

Now, if you hit play, it'll output a debug message every time you click on one of the cubes. That's progress! But it's not draggable yet.

To make it actually draggable, we need to first make the `onClick` set it as the `heldObject` on the state, and then we need to make the dispatcher update the position of the draggable on the state each frame when there is a `heldObject`. The first part is simple, just replace the `onClick` above with this:

```c#
          onClick: () => {
            if (state.heldObject == null) {
              state.heldObject = id;
            } else {
              state.heldObject = null;
            }
          },
```

Now, if nothing is held, then clicking on this draggable will make it held. If something is held, then clicking on it will make it no longer held.

Next, we need to add a new section to the `Update` event on the dispatcher, to move the held object around. This is also pretty simple:

```c#
  void Update() {
    if (state.heldObject != null) {
      var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
      var groundPos = ray.origin - ray.direction * (ray.origin.y / ray.direction.y);
      state.objects[state.heldObject.Value].position = groundPos;
    }

    ureact.Render(RootNode.New(
      state: state,
      material: draggableMaterial ?? throw new Exception("Draggable material is null"),
      mesh: cubeMesh ?? throw new Exception("Draggable mesh is null")
    ));
  }
```

This needs a little bit of vector math. All it's doing is getting the ray from the cursor into the 3D world, and then do some simple math to figure out where that ray would intersect the ground plane (the horizontal plane at elevation 0). Then, it sets the draggable's position to that spot.

Now, if you go hit play, you'll find you can click on any of the cubes, move it around, and click again to let it go. We have our drag-and-drop!

That concludes the main tutorial. Look at `Assets/Examples/DragAndDrop` for the scene and all the source code that should result from this tutorial.

So, what did we really accomplish here? Well, the big thing is that our state and presentation are now definitively separated, and shouldn't be stepping on each other. All frame-by-frame updates to the world state are now done out of one centralized location in `Dispatcher.Update`. As the program gets more complicated, this likely wants to delegate that work out to other classes, but the important part is that the code that's updating the state is ONLY concerned about the state itself, and not about the presentation. And likewise, the code that's doing the presentation (the UReact nodes and components) are not concerned about state transitions. They don't have update functions, they're just focused on how to build the scene as it needs to be right now, given the provided state.

All of this requires a little bit more up-front work when you start, but you'll find that as your project gets bigger and more complicated, you can handle that complexity much more easily as long as this divide is maintained.

If you want to get a more rigorous and less guided discussion of all this, check out [the overview](overview.md).