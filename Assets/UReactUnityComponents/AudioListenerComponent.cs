#nullable enable
using UnityEngine;

namespace UReact {
	public struct AudioListenerProps {
	}

	public static class AudioListenerComponent {
		public static void Render(GameObject obj, AudioListenerProps? oldProps, AudioListenerProps props) {
			if (oldProps == null) {
				obj.AddComponent<AudioListener>();
			}
		}
	}
}