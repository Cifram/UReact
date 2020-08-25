#nullable enable
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
				onClick = () => {
					if (props.state.heldObject == null) {
						props.state.heldObject = id;
					} else {
						props.state.heldObject = null;
					}
				},
				material = props.material,
				mesh = props.mesh,
			}));
		}
		return root;
	}
}