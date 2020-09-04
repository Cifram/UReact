#nullable enable
using System;
using UnityEngine;

namespace UReact {
	public struct MeshColliderProps {
		public Mesh mesh;
		public Func<Mesh> meshConstructor;
	}

	public static class MeshColliderComponent {
		public static void Render(GameObject obj, MeshColliderProps? oldProps, MeshColliderProps props) {
			if (oldProps == null) {
				var meshCollider = obj.AddComponent<MeshCollider>();
				if (props.meshConstructor != null) {
					meshCollider.sharedMesh = props.meshConstructor();
				} else {
					meshCollider.sharedMesh = props.mesh;
				}
			} else {
				if (!oldProps.Value.Equals(props)) {
					var meshCollider = obj.GetComponent<MeshCollider>();
					if (oldProps.Value.meshConstructor == null && props.meshConstructor != null) {
						meshCollider.sharedMesh = props.meshConstructor();
					} else if (props.meshConstructor == null && oldProps.Value.mesh != props.mesh) {
						meshCollider.sharedMesh = props.mesh;
					}
				}
			}
		}
	}
}