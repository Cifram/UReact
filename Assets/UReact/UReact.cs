#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UReact {
	public class Renderer {
		private PopulatedObjElem? oldGraph;

		public void Render(NodeElem newGraph) {
			if (oldGraph == null) {
				oldGraph = BuildFirstTimeGraph(newGraph, null);
			} else {
				// Build dictionaries of new and old elements indexed by key, to diff against
				var oldElemDict = new Dictionary<string, PopulatedObjElem>();
				var newElemDict = new Dictionary<string, (NodeElem, string?)>();
				FillOldElemDict(oldElemDict, oldGraph.Value);
				FillNewElemDict(newElemDict, newGraph);

				// Build the creation, move and update queues, by iterating over the new
				// elements and comparing to the old ones
				var creationQueue = new List<(NodeElem, string?)>();
				var moveQueue = new List<(string, string?)>();
				var updateQueue = new List<string>();
				foreach (var newElem in newElemDict) {
					if (oldElemDict.ContainsKey(newElem.Key)) {
						var (elem, parentKey) = newElem.Value;
						var oldElem = oldElemDict[newElem.Key];
						if (oldElem.parentKey != parentKey) {
							moveQueue.Add((elem.key, parentKey));
						}
						updateQueue.Add(elem.key);
					} else {
						creationQueue.Add(newElem.Value);
					}
				}

				// Build the destruction queue by iterating over the old elements and comparing
				// to the new ones
				var destructionQueue = new List<GameObject>();
				foreach (var oldElem in oldElemDict) {
					if (!newElemDict.ContainsKey(oldElem.Key)) {
						destructionQueue.Add(oldElem.Value.obj);
					}
				}

				// As we update and create elements, they will be converted to PopulatedElements.
				// The set of elements in the update and create queues should exactly emcompass all
				// the elements in the new graph, so by the time those are processed this dictionary
				// should contain all of the new elements, to allow us to rebuild the graph of
				// populated elements at the end.
				var newPopElemDict = new Dictionary<string, PopulatedObjElem>();

				// Execute the creation queue
				foreach (var (newElem, parentKey) in creationQueue) {
					var popElem = newElem.Render(null);
					if (parentKey != null) {
						popElem.obj.transform.SetParent(newPopElemDict[parentKey].obj.transform, false);
					}
					newPopElemDict[popElem.elem.key] = popElem;
				}
				// Execute the update queue
				foreach (var updateKey in updateQueue) {
					var oldElem = oldElemDict[updateKey];
					(var newElem, _) = newElemDict[updateKey];
					newPopElemDict[updateKey] = newElem.Render(oldElem);
				}
				// Execute the move queue
				// Do this after the creation queue, to make sure the parent objects exist.
				foreach (var (src, dst) in moveQueue) {
					if (dst == null) {
						newPopElemDict[src].obj.transform.SetParent(null);
					} else {
						newPopElemDict[src].obj.transform.SetParent(newPopElemDict[dst].obj.transform, false);
					}
				}
				// Execute the destruction queue
				// Do this last, so we don't inadvertently destroy children that got unparented from
				// this GameObject.
				foreach (var obj in destructionQueue) {
					GameObject.Destroy(obj);
				}

				// Build the new populated graph
				oldGraph = PopulateGraph(newGraph, newPopElemDict);
			}
		}

		private PopulatedObjElem PopulateGraph(NodeElem baseElem, Dictionary<string, PopulatedObjElem> popElems) {
			var popElem = popElems[baseElem.key];
			popElem.children = baseElem.children.Select(child => PopulateGraph(child, popElems)).ToArray();
			return popElem;
		}

		private void FillOldElemDict(Dictionary<string, PopulatedObjElem> dict, PopulatedObjElem elem) {
			dict[elem.elem.key] = elem;
			foreach (var child in elem.children) {
				FillOldElemDict(dict, child);
			}
		}

		private void FillNewElemDict(Dictionary<string, (NodeElem, string?)> dict, NodeElem elem, string? parentKey = null) {
			if (dict.ContainsKey(elem.key)) {
				throw new Exception($"Two UReact elements have the key {elem.key}");
			}
			dict[elem.key] = (elem, parentKey);
			foreach (var child in elem.children) {
				FillNewElemDict(dict, child, elem.key);
			}
		}

		private PopulatedObjElem BuildFirstTimeGraph(NodeElem elem, PopulatedObjElem? parent) {
			var newElem = elem.Render(null);
			if (parent != null) {
				newElem.obj.transform.SetParent(parent.Value.obj.transform, false);
				newElem.parentKey = parent.Value.elem.key;
			}
			newElem.children = elem.children.Select(child => BuildFirstTimeGraph(child, newElem)).ToArray();
			return newElem;
		}
	}

	public delegate void CompRender<PropT>(GameObject obj, PropT? oldProps, PropT props)
		where PropT : struct;

	public struct NodeElem {
		public Dictionary<Type, CompElem> compElems;
		public List<NodeElem> children;
		public string key;

		public NodeElem(string key) {
			this.key = key;
			this.compElems = new Dictionary<Type, CompElem>();
			this.children = new List<NodeElem>();
		}

		public NodeElem(string key, List<NodeElem> children) {
			this.key = key;
			this.compElems = new Dictionary<Type, CompElem>();
			this.children = children;
		}

		public NodeElem Component<PropT>(CompRender<PropT> render, PropT props)
			where PropT : struct {
			compElems[typeof(PropT)] = new CompElem<PropT>() { render = render, props = props };
			return this;
		}

		public NodeElem Child(NodeElem child) {
			children.Add(child);
			return this;
		}

		public PopulatedObjElem Render(PopulatedObjElem? old) {
			if (old == null) {
				var obj = new GameObject(key);
				foreach (var compElem in compElems) {
					compElem.Value.BuildComponent(null, obj);
				}
				return new PopulatedObjElem() {
					elem = this,
					obj = obj,
				};
			} else {
				foreach (var compElem in compElems) {
					if (old.Value.elem.compElems.ContainsKey(compElem.Key)) {
						compElem.Value.BuildComponent(old.Value.elem.compElems[compElem.Key], old.Value.obj);
					} else {
						compElem.Value.BuildComponent(null, old.Value.obj);
					}
				}
				foreach (var compElem in old.Value.elem.compElems) {
					if (!compElems.ContainsKey(compElem.Key)) {
						GameObject.Destroy(old.Value.obj.GetComponent(compElem.Key));
					}
				}
				return new PopulatedObjElem() {
					elem = this,
					obj = old.Value.obj,
				};
			}
		}
	}

	public interface CompElem {
		void BuildComponent(CompElem? old, GameObject obj);
	}

	public struct CompElem<PropT> : CompElem where PropT : struct {
		public CompRender<PropT> render;
		public PropT props;

		public CompElem(CompRender<PropT> render, PropT props) {
			this.render = render;
			this.props = props;
		}

		public void BuildComponent(CompElem? old, GameObject obj) {
			render(obj, old == null ? (PropT?)null : ((CompElem<PropT>)old).props, props);
		}
	}

	public struct PopulatedObjElem {
		public NodeElem elem;
		public GameObject obj;
		public PopulatedObjElem[] children;
		public string? parentKey;
	}
}