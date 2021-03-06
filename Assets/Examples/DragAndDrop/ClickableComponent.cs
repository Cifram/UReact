#nullable enable
using System;
using UnityEngine;

public struct ClickableComponent : UReact.Component {
	private Action onClick;

	public ClickableComponent(Action onClick) {
		this.onClick = onClick;
	}

	public void Render(GameObject obj, UReact.Component? oldComp) {
		if (oldComp == null) {
			var clickable = obj.AddComponent<Clickable>();
			clickable.onClick = onClick;
		} else if (oldComp is ClickableComponent old && !old.Equals(this)) {
			var clickable = obj.GetComponent<Clickable>();
			if (onClick != old.onClick) {
				clickable.onClick = onClick;
			}
		}
	}

	public Type[] GetManagedBehaviourTypes() {
		return new Type[] { typeof(Clickable) };
	}
}
