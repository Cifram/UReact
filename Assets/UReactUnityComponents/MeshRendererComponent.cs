#nullable enable
using System;
using UnityEngine;

namespace UReact {
	public struct MeshRendererProps {
		public Material? material;
		public Func<Material>? materialConstructor;
	}

	public static class MeshRendererComponent {
		public static void Render(GameObject obj, MeshRendererProps? oldProps, MeshRendererProps props) {
			if (oldProps == null) {
				var meshRenderer = obj.AddComponent<MeshRenderer>();
				if (props.materialConstructor != null) {
					meshRenderer.material = props.materialConstructor();
				} else {
					meshRenderer.material = props.material;
				}
			} else {
				if (!oldProps.Value.Equals(props)) {
					var meshRenderer = obj.GetComponent<MeshRenderer>();
					if (oldProps.Value.materialConstructor == null && props.materialConstructor != null) {
						meshRenderer.material = props.materialConstructor();
					} else if (props.materialConstructor == null && oldProps.Value.material != props.material) {
						meshRenderer.material = props.material;
					}
				}
			}
		}
	}
}