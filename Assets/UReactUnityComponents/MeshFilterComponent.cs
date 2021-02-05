#nullable enable
using System;
using UnityEngine;

namespace UReact {
	public struct MeshFilterComponent : Component {
		private Mesh? mesh;
		private Func<Mesh>? meshConstructor;

		public MeshFilterComponent(Mesh? mesh = null, Func<Mesh>? meshConstructor = null) {
			this.mesh = mesh;
			this.meshConstructor = meshConstructor;
		}

		public void Render(GameObject obj, Component? oldComp) {
			if (oldComp == null) {
				var meshFilter = obj.AddComponent<MeshFilter>();
				if (meshConstructor != null) {
					meshFilter.mesh = meshConstructor();
				} else {
					meshFilter.mesh = mesh;
				}
			} else if (oldComp is MeshFilterComponent old && !old.Equals(this)) {
				var meshFilter = obj.GetComponent<MeshFilter>();
				if (old.meshConstructor == null && meshConstructor != null) {
					meshFilter.mesh = meshConstructor();
				} else if (meshConstructor == null && old.mesh != mesh) {
					meshFilter.mesh = mesh;
				}
			}
		}

		public Type[] GetManagedBehaviourTypes() {
			return new Type[] { typeof(MeshFilter) };
		}
	}
}