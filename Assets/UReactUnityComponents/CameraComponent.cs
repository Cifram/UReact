#nullable enable
using System;
using UnityEngine;

namespace UReact {
	public struct CameraComponent : Component {
		private Rect viewport;

		public CameraComponent(Rect viewport) {
			this.viewport = viewport;
		}

		public void Render(GameObject obj, Component? oldComp) {
			if (oldComp == null) {
				var camera = obj.AddComponent<Camera>();
				camera.pixelRect = viewport;
			} else if (oldComp is CameraComponent old && !old.Equals(this)) {
				var camera = obj.GetComponent<Camera>();
				if (old.viewport != viewport) {
					camera.pixelRect = viewport;
				}
			}
		}

		public Type[] GetManagedBehaviourTypes() {
			return new Type[] { typeof(CameraComponent) };
		}
	}
}