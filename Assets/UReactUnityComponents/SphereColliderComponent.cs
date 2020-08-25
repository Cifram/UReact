#nullable enable
using UnityEngine;

namespace UReact {
	public struct SphereColliderProps {
		public Vector3 center;
		public float radius;
	}

	public static class SphereColliderWidget {
		public static void Render(GameObject obj, SphereColliderProps? oldProps, SphereColliderProps props) {
			if (oldProps == null) {
				var SphereCollider = obj.AddComponent<SphereCollider>();
			} else {
				if (!oldProps.Value.Equals(props)) {
					var SphereCollider = obj.GetComponent<SphereCollider>();
					if (oldProps.Value.center != props.center) {
						SphereCollider.center = props.center;
					}
					if (oldProps.Value.radius != props.radius) {
						SphereCollider.radius = props.radius;
					}
				}
			}
		}
	}
}