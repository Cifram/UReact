#nullable enable
using System;
using UnityEngine;

namespace UReact {
	public struct AudioListenerComponent : Component {
		public void Render(GameObject obj, Component? oldComp) {
			if (oldComp == null) {
				obj.AddComponent<AudioListener>();
			}
		}

		public Type[] GetManagedBehaviourTypes() {
			return new Type[] { typeof(AudioListenerComponent) };
		}
	}
}