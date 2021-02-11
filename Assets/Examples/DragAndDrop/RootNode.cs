#nullable enable
using UnityEngine;
using UReact;

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