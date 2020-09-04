#nullable enable
using System;
using UnityEngine;

namespace UReact {
	public struct MeshFilterProps {
		public Mesh mesh;
		public Func<Mesh> meshConstructor;
	}

	public static class MeshFilterComponent {
		public static void Render(GameObject obj, MeshFilterProps? oldProps, MeshFilterProps props) {
			if (oldProps == null) {
				var meshFilter = obj.AddComponent<MeshFilter>();
				if (props.meshConstructor != null) {
					meshFilter.mesh = props.meshConstructor();
				} else {
					meshFilter.mesh = props.mesh;
				}
			} else {
				if (!oldProps.Value.Equals(props)) {
					var meshFilter = obj.GetComponent<MeshFilter>();
					if (oldProps.Value.meshConstructor == null && props.meshConstructor != null) {
						meshFilter.mesh = props.meshConstructor();
					} else if (props.meshConstructor == null && oldProps.Value.mesh != props.mesh) {
						meshFilter.mesh = props.mesh;
					}
				}
			}
		}
	}
}