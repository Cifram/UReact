#nullable enable
using System;
using UnityEngine;

public class Clickable : MonoBehaviour {
	public Action? onClick;

	public void OnMouseUpAsButton() {
		if (onClick != null) {
			onClick();
		}
	}
}