#nullable enable
using System;
using UnityEngine;

namespace UReact {
	public struct MeshRendererComponent : Component {
		private Material? material;
		private Func<Material>? materialConstructor;

		public MeshRendererComponent(Material? material = null, Func<Material>? materialConstructor = null) {
			this.material = material;
			this.materialConstructor = materialConstructor;
		}

		public void Render(GameObject obj, Component? oldComp) {
			if (oldComp == null) {
				var meshRenderer = obj.AddComponent<MeshRenderer>();
				if (materialConstructor != null) {
					meshRenderer.material = materialConstructor();
				} else {
					meshRenderer.material = material;
				}
			} else if (oldComp is MeshRendererComponent old && !old.Equals(this)) {
				var meshRenderer = obj.GetComponent<MeshRenderer>();
				if (old.materialConstructor == null && materialConstructor != null) {
					meshRenderer.material = materialConstructor();
				} else if (materialConstructor == null && old.material != material) {
					meshRenderer.material = material;
				}
			}
		}

		public Type[] GetManagedBehaviourTypes() {
			return new Type[] { typeof(MeshRenderer) };
		}
	}
}