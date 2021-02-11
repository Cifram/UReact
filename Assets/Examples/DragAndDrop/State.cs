#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

public class Draggable {
	public float size;
	public Vector3 position;
}

public class State {
	public Dictionary<Guid, Draggable> objects = new Dictionary<Guid, Draggable>();
	public Guid? heldObject;
}