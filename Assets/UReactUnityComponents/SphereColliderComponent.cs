#nullable enable
using System;
using UnityEngine;

namespace UReact {
	public struct SphereColliderComponent : Component {
		private Vector3 center;
		private float radius;

		public SphereColliderComponent(Vector3? center = null, float radius = 0) {
			this.center = center ?? Vector3.zero;
			this.radius = radius;
		}

		public void Render(GameObject obj, Component? oldComp) {
			if (oldComp == null) {
				var SphereCollider = obj.AddComponent<SphereCollider>();
			} else if (oldComp is SphereColliderComponent old && !old.Equals(this)) {
				var SphereCollider = obj.GetComponent<SphereCollider>();
				if (old.center != center) {
					SphereCollider.center = center;
				}
				if (old.radius != radius) {
					SphereCollider.radius = radius;
				}
			}
		}

		public Type[] GetManagedBehaviourTypes() {
			return new Type[] { typeof(SphereCollider) };
		}
	}
}