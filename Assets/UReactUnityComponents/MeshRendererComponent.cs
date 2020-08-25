#nullable enable
using UnityEngine;

namespace UReact {
	public struct MeshRendererProps {
		public Material material;
	}

	public static class MeshRendererComponent {
		public static void Render(GameObject obj, MeshRendererProps? oldProps, MeshRendererProps props) {
			if (oldProps == null) {
				var meshRenderer = obj.AddComponent<MeshRenderer>();
				meshRenderer.material = props.material;
			} else {
				if (!oldProps.Value.Equals(props)) {
					var meshRenderer = obj.GetComponent<MeshRenderer>();
					meshRenderer.material = props.material;
				}
			}
		}
	}
}