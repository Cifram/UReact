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