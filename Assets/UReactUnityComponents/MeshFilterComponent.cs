#nullable enable
using UnityEngine;

namespace UReact {
	public struct MeshFilterProps {
		public Mesh mesh;
	}

	public static class MeshFilterComponent {
		public static void Render(GameObject obj, MeshFilterProps? oldProps, MeshFilterProps props) {
			if (oldProps == null) {
				var meshRenderer = obj.AddComponent<MeshFilter>();
				meshRenderer.mesh = props.mesh;
			} else {
				if (!oldProps.Value.Equals(props)) {
					var meshRenderer = obj.GetComponent<MeshFilter>();
					meshRenderer.mesh = props.mesh;
				}
			}
		}
	}
}