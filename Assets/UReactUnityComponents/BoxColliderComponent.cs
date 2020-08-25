#nullable enable
using UnityEngine;

namespace UReact {
	public struct BoxColliderProps {
		public Vector3 center;
		public Vector3 size;
	}

	public static class BoxColliderComponent {
		public static void Render(GameObject obj, BoxColliderProps? oldProps, BoxColliderProps props) {
			if (oldProps == null) {
				var boxCollider = obj.AddComponent<BoxCollider>();
			} else {
				if (!oldProps.Value.Equals(props)) {
					var boxCollider = obj.GetComponent<BoxCollider>();
					if (oldProps.Value.center != props.center) {
						boxCollider.center = props.center;
					}
					if (oldProps.Value.size != props.size) {
						boxCollider.size = props.size;
					}
				}
			}
		}
	}
}