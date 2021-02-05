#nullable enable
using System;
using UnityEngine;

namespace UReact {
	public struct TransformComponent : Component {
		private Vector3 localPosition;
		private Vector3? lookDirection;
		private Quaternion localRotation;
		private Vector3 localScale;

		public TransformComponent(
			Vector3? localPosition = null,
			Vector3? lookDirection = null,
			Quaternion? localRotation = null,
			Vector3? localScale = null
		) {
			this.localPosition = localPosition ?? Vector3.zero;
			this.lookDirection = lookDirection;
			this.localRotation = localRotation ?? Quaternion.identity;
			this.localScale = localScale ?? Vector3.one;
		}

		public void Render(GameObject obj, Component? oldComp) {
			if (oldComp == null) {
				var transform = obj.transform;
				transform.localPosition = localPosition;
				if (lookDirection != null) {
					transform.LookAt(transform.position + lookDirection.Value, Vector3.up);
				} else {
					transform.localRotation = localRotation;
				}
				transform.localScale = localScale;
			} else {
				var transform = obj.transform;
				transform.localPosition = localPosition;
				if (lookDirection == null) {
					transform.localRotation = localRotation;
				} else {
					transform.LookAt(transform.position + lookDirection.Value, Vector3.up);
				}
				transform.localScale = localScale;
			}
		}

		public Type[] GetManagedBehaviourTypes() {
			return new Type[] { typeof(Transform) };
		}
	}
}