#nullable enable
using System;
using UnityEngine;

public class Dispatcher : MonoBehaviour {
	public Material? draggableMaterial;

	private State state = new State();
	private UReact.Renderer ureact = new UReact.Renderer();
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
			size = 1f,
		};

		cubeMesh = BuildCubeMesh();
	}

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

	private Mesh BuildCubeMesh() {
		var mesh = new Mesh();
		mesh.vertices = new Vector3[] {
			// top
			new Vector3(0.5f, 0.5f, 0.5f),
			new Vector3(0.5f, 0.5f, -0.5f),
			new Vector3(-0.5f, 0.5f, -0.5f),
			new Vector3(-0.5f, 0.5f, 0.5f),
			// bottom
			new Vector3(0.5f, -0.5f, 0.5f),
			new Vector3(-0.5f, -0.5f, 0.5f),
			new Vector3(-0.5f, -0.5f, -0.5f),
			new Vector3(0.5f, -0.5f, -0.5f),
			// left
			new Vector3(-0.5f, 0.5f, 0.5f),
			new Vector3(-0.5f, 0.5f, -0.5f),
			new Vector3(-0.5f, -0.5f, -0.5f),
			new Vector3(-0.5f, -0.5f, 0.5f),
			// right
			new Vector3(0.5f, 0.5f, 0.5f),
			new Vector3(0.5f, -0.5f, 0.5f),
			new Vector3(0.5f, -0.5f, -0.5f),
			new Vector3(0.5f, 0.5f, -0.5f),
			// front
			new Vector3(0.5f, 0.5f, 0.5f),
			new Vector3(-0.5f, 0.5f, 0.5f),
			new Vector3(-0.5f, -0.5f, 0.5f),
			new Vector3(0.5f, -0.5f, 0.5f),
			// back
			new Vector3(0.5f, 0.5f, -0.5f),
			new Vector3(0.5f, -0.5f, -0.5f),
			new Vector3(-0.5f, -0.5f, -0.5f),
			new Vector3(-0.5f, 0.5f, -0.5f),
		};
		mesh.normals = new Vector3[] {
			Vector3.up, Vector3.up, Vector3.up, Vector3.up,
			Vector3.down, Vector3.down, Vector3.down, Vector3.down,
			Vector3.left, Vector3.left, Vector3.left, Vector3.left,
			Vector3.right, Vector3.right, Vector3.right, Vector3.right,
			Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
			Vector3.back, Vector3.back, Vector3.back, Vector3.back,
		};
		mesh.triangles = new int[] {
			0, 1, 2,
			2, 3, 0,
			4, 5, 6,
			6, 7, 4,
			8, 9, 10,
			10, 11, 8,
			12, 13, 14,
			14, 15, 12,
			16, 17, 18,
			18, 19, 16,
			20, 21, 22,
			22, 23, 20
		};
		return mesh;
	}
}