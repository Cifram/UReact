# Declarative Programming

A core principle of UReact is that it's declarative. Declarative programming is a big topic which you can read about elsewhere, but in this context it means the focus is on declaring how the world looks, right now, based on the provided state, and NOT on how the world is updating or changing.

This doesn't mean the world can't change or update, just that the state and presentation are broken into entirely separate layers. The state updates each frame, and the presentation is then constructed based on the current state. The presentation in this case is the Unity scene graph composed of `GameObject`s with `Component`s on them, and UReact is the system for building that presentation based on the current state.

The advantage of this is that it greatly simplifies what you have to think about, as the programmer. When building your presentation, you don't have to care about all the transitions and changes. You don't have to think about all the steps required to update each thing in the scene graph that cares about a given change. On one side, you just change the state. And on the other side, you just build a thing based on the current state.

In order to accomplish this, UReact creates the illusion that you're rebuilding the entire scene graph from scratch each frame. Under the hood, this isn't quite what's happening. Rather, you build up a lightweight representation of the scene graph each frame, and then UReact considers the difference between what you built last frame, and what you built this frame, and only updates the parts of the scene graph that actually changed. But when using it, you rarely have to think about this.

# High Level Structure

Core to UReact is the relationship between UReact componets, component elements and Unity components, and likewise UReact nodes, node elements and `GameObject`s.

A UReact component represents a Unity `Component`, and a UReact node represents a Unity `GameObject`. These components and nodes are the classes you will write that drive UReact. If you're familiar with React, these are equivalent to React components.

Component elements and node elements are the intermediate, internal representation used to build up the lightweight scene graph. They store references to your nodes and components in a hierarchy. The actual classes for these are `NodeElem` and `CompElem`. For the most part, you won't have to interact with the `CompElem` class at all when using UReact; that's all handled internally. However, you will have to know how to construct a `NodeElem` when you write a node.

The class that pulls this all together is the `Renderer`. Create a `UReact.Renderer` to represent a scene graph you want to manage with UReact, and call `Render` on it, passing in a `NodeElem` for the root node. It will build the actual Unity scene graph from that. When you call `Render` on this same renderer again, it will build up the updated scene graph of `NodeElem`s and `CompElem`s, and compare it to the one from the last time it was called, and use that comparison to update the actual Unity scene graph, only where required.

# Nodes

To build a node, you just need a function that returns a `NodeElem`. That's it. By convention, each node is a static class with a single `New` function on it.

A `NodeElem` thankfully is pretty simple. The main things it's generally needs are:
- A unique key to identify the node.
- A list of components on the node.
- A list of child nodes.

An example of a simple node:

```c#
public static class MyNode {
  public static UReact.NodeElem New(int id, Vector3 position, float size, Mesh mesh, Material material) {
    return new UReact.NodeElem(
      key: $"MyNode-{id}"
    ).Component(
      new UReact.TransformComponent(
        localPosition: position,
        localScale: Vector3.one * size
      )
    ).Component(
      new UReact.MeshFilter(
        mesh: mesh
      )
    ).Component(
      new UReact.MeshRenderer(
        material: material
      )
    ).Child(
      MyOtherNode.New(id, size)
    );
  }
}
```

Note the key passed in the original constructor for `NodeElem`. It's very important that this key is unique across all nodes in the same renderer. This is how it uniquely identifies a node, even if it moves to a different part of the scene graph. As long as it maintains the same key, it will continue to be associated with the same `GameObject`, and that `GameObject` will be reparented as necessary. So if two nodes have the same key, even if they're in wildly different parts of the scene graph, it won't know what to do with them.

There are a couple of other optional parameters to the `NodeElem` constructor:
- `bool active` - Whether the `GameObject` is currently active. Defaults to `true`.
- `int layer` - What Unity layer the `GameObject` goes on. Defaults to `0`.
- `string tag` - Which Unity tag the `GameObject` has. Defaults to `"Untagged"`.

After you construct a `NodeElem`, you can call `Component` on it to add a UReact component, or call `Child` to add a child. That's pretty much the whole `NodeElem` API.

# Components

Building a component is a little more complicated, but still pretty simple. You just need to make a `struct` that implements the `UReact.Component` interface, which looks like this:

```c#
public interface Component {
  void Render(GameObject obj, Component? oldComp);
  Type[] GetManagedBehaviourTypes();
}
```

Of course, this struct should also have a constructor, which takes whatever parameters the component needs, and stores those on private members. By convention, this constructor does no real work, leaving that entirely up to the `Render` function. It just stores the parameters.

The `Render` function does all the work. The `GameObject` passed in is the actual object this component will live on, and this function is expected to add or update the Unity component, as necessary. The `oldComp` parameter is the version of this component from the previous frame. If this is the first frame this `GameObject` has existed, or the first frame it's had this component on it, then `oldComp` will be null, and that's your signal that you need to create the component. Otherwise, you just need to update it based on the parameters passed in to the constructor.

The `GetManagedBehaviourTypes` method just returns an array of the `Type` for each Unity component that this UReact component manages. This is used to clean up after the component. If next frame, this `GameObject` no longer has this `UReact` component on it, it will remove these Unity components from the `GameObject`.

An important note here: You might notice that the `Render` function has full access to the `GameObject`, and could thus do anything with it. And it's effectively called every frame. So at first glance, it might look like it's just a replacement for the old `Update` function on a `MonoBehaviour`! And technically, you CAN use it that way, if your goal is to undermine every advantage of using UReact. Please don't, and see the "Rules of Good UReact" section at the end of this overview for more detail.

Anyway, that out of the way, on to a simple example component:

```c#
public struct SphereColliderComponent : Component {
  private Vector3 center;
  private float radius;

  public SphereColliderComponent(Vector3? center = null, float radius = 1) {
    this.center = center ?? Vector3.zero;
    this.radius = radius;
  }

  public void Render(GameObject obj, Component? oldComp) {
    if (oldComp == null) {
      var sphereCollider = obj.AddComponent<SphereCollider>();
      sphereCollider.center = center;
      sphereCollider.radius = radius;
    } else if (oldComp is SphereColliderComponent old && !old.Equals(this)) {
      var sphereCollider = obj.GetComponent<SphereCollider>();
      if (old.center != center) {
        sphereCollider.center = center;
      }
      if (old.radius != radius) {
        sphereCollider.radius = radius;
      }
    }
  }

  public Type[] GetManagedBehaviourTypes() {
    return new Type[] { typeof(SphereCollider) };
  }
}
```

This is actually the official UReact `SphereColliderComponent`, which wraps Unity's `SphereCollider` component. Note first that the `Render` function first checks whether `oldComp` is `null`, and if it is, it creates the `SphereCollider` and sets all it's parameters. If `oldComp` is not `null`, then it converts it to a `SphereColliderComponent` (which should always succeed) and checks `old.Equals(this)`. The ability to do this comparison is part of why UReact components must always be a `struct` and not a `class`. Class comparison checks if they're literally the same object, whereas struct comparison compares each element of the struct. If the old component's parameters match the current one's, there's no change so we don't need to do anything. If they don't match, then we fetch the underlying Unity component, and go through and check each parameter and update them on the Unity component as necessary.

Every UReact component should look pretty similar to this. The details may change, but the structure is pretty boilerplate, and thus easy to write.

# Renderer and Dispatcher

The renderer is pretty simple: Create and store a `UReact.Renderer`, and every frame call `Render` on it, passing in the `NodeElem` of the root node.

By convention, this is managed by what we call a dispatcher class. This is a `MonoBehaviour` which initializes our global state, and creates the renderer, in it's `Start` function, and updates that state, and calls `Render` on the renderer, in it's `Update` function. It's called a dispatcher because it dispatches these events, initialization and updating of the scene, to any other code that needs it. In a large, complicated program, it's not expected that the dispatcher manages all of these things directly, but rather that it's the entry point that delegates to everything else that needs to do initialization or updates.

A simple dispatcher might look something like this:

```c#
public class Dispatcher : MonoBehaviour {
  private StateStore state;
  private UReact.Renderer renderer;

  void Start() {
    state = new StateStore();
    renderer = new UReact.Renderer();
  }

  void Update() {
    state.Update();

    renderer.Render(RootNode.New(
      state: state,
    ));
  }
}
```

Where `StateStore` is a stand-in for whatever class you create for managing your global state.

It's quite possible and sometimes sensible to maintain more than one renderer. Each one will maintain it's own section of the Unity scene graph, off a root `GameObject`. An example of where this might make sense is having one renderer for the game geometry, and another one for the UI. Or, if you need to run two scenes in parallel on split screen, giving each one it's own renderer might make sense. It's up to you to decide what makes the most sense for your circumstances.

# Custom MonoBehaviours

There's still occassionally reason, aside from the dispatcher, to write your own `MonoBehaviour` when using UReact. You shouldn't likely need very many of them, because your state system and the various UReact components can cover most of what `MonoBehaviour`s do in a traditional Unity program, but there are a few cases where they're still needed.

A couple of examples:
- Handling mouse events. It's common to add a `Clickable` component or similar, which will capture things like Unity's `OnMouseUpAsButton` event to call a closure passed in by UReact. Or perhaps just store closures for mouse events, which will be called by external code doing raycasts. However you code it, you'll need a custom `MonoBehaviour` to capture mouse interactions with a `GameObject`.
- Procedural meshes. If a `GameObject` needs to have a mesh on it which is built based on the parameters passed into your UReact component, you probably don't want the component's `Render` function rebuilding that mesh every frame. However, there's a new instance of the component every frame, so it can't save the mesh either. To handle this, you can build a custom `MonoBehaviour` that stores the mesh, which takes all of the necessary parameters to build the mesh as properties. Those then mark the mesh as dirty, to be rebuilt on the next `Update`.

The second case also raises an important circumstance: Sometimes, a custom `MonoBehaviour` needs to manage other Unity components on the same `GameObject`. This is fine, provided it has sole knowledge and control of those Unity components. So your custom mesh `MonoBehaviour` may need a `MeshFilter` and possibly a `MeshCollider`. It should make sure they're created by using the `RequireComponent` attribute, and destroy them in it's `OnDestroy` event. And the UReact component that manages this `MonoBehaviour` should not directly interact with that `MeshFilter` or `MeshCollider` at all, only managing the custom `MonoBehaviour` and counting on it to do what it needs to do to.

# Rules of Good UReact

Now that you've got the structure of UReact down, there are some very important rules to understand for any code that interacts with UReact. Breaking these rules breaks the UReact paradigm, and will tend to cause things to not function properly.

## 1. UReact components and nodes should never read from the Unity scene graph

The UReact paradigm means that nodes and components are given all the data they need to create their portion of the scene graph from scratch. Nodes in particular should always pretend they're doing that. And if you're always making an entirely new scene graph from scratch, why would you look at the old one?

UReact components are given the `GameObject` they're attached to, and have to branch on whether they're creating or modifying the Unity components they manage on that `GameObject`, so they can't entirely pretend to always be creating everything from scratch. That said, the ONLY reason they have to read anything off the `GameObject` is to fetch the Unity component they're managing, and there's no reason they should ever have to read anything off that component. That Unity component is solely managed by this UReact component. Every field that has ever been set on it was set by this UReact component, based on state that was passed in exactly like the state you have. You have access to the relevant state from the previous frame in the form of the `oldComp` variable, but even that should only be used to determine what has changed. You can compare whether the new values are equal to the old values, so you know if you need to set the new values on the Unity component, but even with `oldComp` you should never do anything beyond that with the values.

So this rule can be restated as:

**The previous frame's state ONLY matters for deciding which values need to be updated in the scene graph**

If you're using anything read off of `oldComp` for any purpose beyond that, or if you're reading data off the scene graph at all inside a UReact component or node, you're breaking the UReact paradigm and asking for trouble.

## 2. Unity components managed by UReact components must NOT be written to by anything else

If a UReact component is managing a Unity component, it needs to know it has total and sole control over that component. The `Render` function is given some parameters that determine the state of the Unity components it manages, and when it's done, the Unity component needs to entirely match what those parameters indicate. But, it's comparing against the old state in `oldComp` to avoid changing things it doesn't need to. If something else has been messing with the Unity component in the meantime, some of those values may no longer be what's indicated by `oldComp`, and the `Render` function may not overwrite things it needs to.

In short, if you write to any component managed by a UReact component, you're breaking the UReact paradigm and asking for trouble.

Note that when you write your own custom `MonoBehaviour`s with the intent of having them managed to a UReact component, those `MonoBehaviour`s can still write to themselves. Also, if they create and manage some of their own Unity components, as described above, they also have full control of those, being able to read from or write to them however they need. However, nothing ELSE should ever write to those components. This excemption applies entirely to the `MonoBehaviour` responsible for managing them.

# What Next?

Try out [the tutorial](tutorial.md) to see all of this in action!