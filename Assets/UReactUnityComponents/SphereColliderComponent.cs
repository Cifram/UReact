#nullable enable
using System;
using UnityEngine;

namespace UReact {
	public struct SphereColliderComponent : Component {
		private Vector3 center;
		private float radius;

		public SphereColliderComponent(Vector3? center = null, float radius = 1) {
			this.center = center ?? Vector3.zero;
			this.radius = radius;
		}

		public void Render(GameObject obj, Component? oldComp) {
			if (oldComp == null) {
				var sphereCollider = obj.AddComponent<SphereCollider>();
				sphereCollider.center = center;
				sphereCollider.radius = radius;
			} else if (oldComp is SphereColliderComponent old && !old.Equals(this)) {
				var sphereCollider = obj.GetComponent<SphereCollider>();
				if (old.center != center) {
					sphereCollider.center = center;
				}
				if (old.radius != radius) {
					sphereCollider.radius = radius;
				}
			}
		}

		public Type[] GetManagedBehaviourTypes() {
			return new Type[] { typeof(SphereCollider) };
		}
	}
}