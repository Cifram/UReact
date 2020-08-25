#nullable enable
using UnityEngine;

namespace UReact {
	public struct TransformProps {
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 localScale;
	}

	public static class TransformComponent {
		public static void Render(GameObject obj, TransformProps? oldProps, TransformProps props) {
			if (oldProps == null) {
				var transform = obj.transform;
				transform.position = props.position;
				transform.rotation = props.rotation;
				transform.localScale = props.localScale;
			} else {
				if (!oldProps.Value.Equals(props)) {
					var transform = obj.transform;
					if (oldProps.Value.position != props.position) {
						transform.position = props.position;
					}
					if (oldProps.Value.rotation != props.rotation) {
						transform.rotation = props.rotation;
					}
					if (oldProps.Value.localScale != props.localScale) {
						transform.localScale = props.localScale;
					}
				}
			}
		}
	}
}