#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace State {
	public class Draggable {
		public float size;
		public Vector3 position;
	}

	public class Store {
		public Dictionary<Guid, Draggable> objects = new Dictionary<Guid, Draggable>();
		public Guid? heldObject;
	}
}