#nullable enable
using UnityEngine;

namespace UReact {
	public struct TransformProps {
		public Vector3 position;
		public Vector3? lookAt;
		public Quaternion rotation;
		public Vector3? localScale;
	}

	public static class TransformComponent {
		public static void Render(GameObject obj, TransformProps? oldProps, TransformProps props) {
			if (oldProps == null) {
				var transform = obj.transform;
				transform.position = props.position;
				if (props.lookAt != null) {
					transform.LookAt(props.lookAt.Value, Vector3.up);
				} else {
					transform.rotation = props.rotation;
				}
				transform.localScale = props.localScale ?? Vector3.one;
			} else {
				var transform = obj.transform;
				transform.position = props.position;
				if (props.lookAt == null) {
					transform.rotation = props.rotation;
				} else {
					transform.LookAt(props.lookAt.Value, Vector3.up);
				}
				transform.localScale = props.localScale ?? Vector3.one;
			}
		}
	}
}