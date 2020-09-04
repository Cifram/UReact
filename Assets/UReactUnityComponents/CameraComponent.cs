#nullable enable
using UnityEngine;

namespace UReact {
	public struct CameraProps {
	}

	public static class CameraComponent {
		public static void Render(GameObject obj, CameraProps? oldProps, CameraProps props) {
			if (oldProps == null) {
				obj.AddComponent<Camera>();
			}
		}
	}
}