#nullable enable
using System;
using UnityEngine;
using UReact;

public struct DraggableProps {
	public string key;
	public Vector3 position;
	public float size;
	public Action onClick;
	public Material material;
	public Mesh mesh;
}

public static class DraggableNode {
	public static NodeElem New(DraggableProps props) =>
		new NodeElem(
			props.key
		).Component(
			TransformComponent.Render,
			new TransformProps { position = props.position, localScale = Vector3.one * props.size }
		).Component(
			MeshRendererComponent.Render,
			new MeshRendererProps { material = props.material }
		).Component(
			MeshFilterComponent.Render,
			new MeshFilterProps { mesh = props.mesh }
		).Component(
			BoxColliderComponent.Render,
			new BoxColliderProps { center = Vector3.zero, size = Vector3.one * props.size }
		).Component(
			ClickableComponent.Render,
			new ClickableProps { onClick = props.onClick }
		);
}