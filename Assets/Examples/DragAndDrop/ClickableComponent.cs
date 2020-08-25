#nullable enable
using System;
using UnityEngine;

namespace UReact {
	public struct ClickableProps {
		public Action onClick;
	}

	public static class ClickableComponent {
		public static void Render(GameObject obj, ClickableProps? oldProps, ClickableProps props) {
			if (oldProps == null) {
				var clickable = obj.AddComponent<Clickable>();
				clickable.onClick = props.onClick;
			}
		}
	}
}