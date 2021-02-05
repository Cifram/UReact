#nullable enable
using System;
using UnityEngine;

namespace UReact {
	public struct BoxColliderComponent : Component {
		private Vector3 center;
		private Vector3 size;

		public BoxColliderComponent(Vector3 center, Vector3 size) {
			this.center = center;
			this.size = size;
		}

		public void Render(GameObject obj, Component? oldComp) {
			if (oldComp == null) {
				var boxCollider = obj.AddComponent<BoxCollider>();
			} else if (oldComp is BoxColliderComponent old && !old.Equals(this)) {
				var boxCollider = obj.GetComponent<BoxCollider>();
				if (old.center != center) {
					boxCollider.center = center;
				}
				if (old.size != size) {
					boxCollider.size = size;
				}
			}
		}

		public Type[] GetManagedBehaviourTypes() {
			return new Type[] { typeof(BoxCollider) };
		}
	}
}