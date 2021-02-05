#nullable enable
using System;
using UnityEngine;

namespace UReact {
	public struct MeshColliderComponent : Component {
		private Mesh? mesh;
		private Func<Mesh>? meshConstructor;

		public MeshColliderComponent(Mesh? mesh = null, Func<Mesh>? meshConstructor = null) {
			this.mesh = mesh;
			this.meshConstructor = meshConstructor;
		}

		public void Render(GameObject obj, Component? oldComp) {
			if (oldComp == null) {
				var meshCollider = obj.AddComponent<MeshCollider>();
				if (meshConstructor != null) {
					meshCollider.sharedMesh = meshConstructor();
				} else {
					meshCollider.sharedMesh = mesh;
				}
			} else if (oldComp is MeshColliderComponent old && !old.Equals(this)) {
				var meshCollider = obj.GetComponent<MeshCollider>();
				if (old.meshConstructor == null && meshConstructor != null) {
					meshCollider.sharedMesh = meshConstructor();
				} else if (meshConstructor == null && old.mesh != mesh) {
					meshCollider.sharedMesh = mesh;
				}
			}
		}

		public Type[] GetManagedBehaviourTypes() {
			return new Type[] { typeof(MeshCollider) };
		}
	}
}