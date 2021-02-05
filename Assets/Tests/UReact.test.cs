#nullable enable
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UReact;

class TestMonoBehaviour : MonoBehaviour {
	public int value;
}

struct TestComponent : UReact.Component {
	private int value;

	public TestComponent(int value) {
		this.value = value;
	}

	public void Render(GameObject obj, UReact.Component? oldProps) {
		if (oldProps == null) {
			obj.AddComponent<TestMonoBehaviour>().value = value;
		} else {
			obj.GetComponent<TestMonoBehaviour>().value = value;
		}
	}

	public Type[] GetManagedBehaviourTypes() {
		return new Type[] { typeof(TestMonoBehaviour) };
	}
}

class Test2MonoBehaviour : MonoBehaviour {
	public string? value;
}

struct Test2Component : UReact.Component {
	private string value;

	public Test2Component(string value) {
		this.value = value;
	}

	public void Render(GameObject obj, UReact.Component? oldComp) {
		if (oldComp == null) {
			obj.AddComponent<Test2MonoBehaviour>().value = value;
		} else {
			obj.GetComponent<Test2MonoBehaviour>().value = value;
		}
	}

	public Type[] GetManagedBehaviourTypes() {
		return new Type[] { typeof(Test2MonoBehaviour) };
	}
}

public class UReactTests {
	[UnityTest]
	public IEnumerator CompElem() {
		CompElem compElem1 = new CompElem<TestComponent>(new TestComponent(value: 42));
		CompElem compElem2 = new CompElem<TestComponent>(new TestComponent(value: 7));
		var obj = new GameObject();

		compElem1.BuildComponent(null, obj);
		var monoBehaviour = obj.GetComponent<TestMonoBehaviour>();
		Assert.That(monoBehaviour, Is.Not.Null);
		Assert.That(monoBehaviour.value, Is.EqualTo(42));
		compElem2.BuildComponent(compElem1, obj);
		Assert.That(monoBehaviour.value, Is.EqualTo(7));
		compElem2.RemoveComponent(obj);
		yield return null;
		Assert.That(obj.GetComponent<TestMonoBehaviour>(), Is.Null);
	}

	[Test]
	public void NodeElem_Component() {
		var nodeElem = new NodeElem("test");
		Assert.That(nodeElem.compElems.ContainsKey(typeof(TestComponent)), Is.False);
		nodeElem.Component(new TestComponent(value: 42));
		Assert.That(nodeElem.compElems.ContainsKey(typeof(TestComponent)), Is.True);
	}

	[Test]
	public void NodeElem_Child() {
		var parentNode = new NodeElem("parent");
		Assert.That(parentNode.children.Count, Is.EqualTo(0));
		var childNode = new NodeElem("child");
		parentNode.Child(childNode);
		Assert.That(parentNode.children.Count, Is.EqualTo(1));
		Assert.That(parentNode.children[0].key, Is.EqualTo("child"));
	}

	[UnityTest]
	public IEnumerator NodeElem_Render_FirstTime() {
		var popNode = new NodeElem(
			key: "test",
			layer: 2,
			tag: "Player",
			active: true
		).Component(
			new TestComponent(value: 42)
		).Render(null);
		yield return null;
		Assert.That(popNode.elem.key, Is.EqualTo("test"));
		Assert.That(popNode.obj, Is.Not.Null);
		Assert.That(popNode.obj.name, Is.EqualTo("test"));
		Assert.That(popNode.obj.layer, Is.EqualTo(2));
		Assert.That(popNode.obj.tag, Is.EqualTo("Player"));
		Assert.That(popNode.obj.activeSelf, Is.True);
		var monoBehaviour = popNode.obj.GetComponent<TestMonoBehaviour>();
		Assert.That(monoBehaviour, Is.Not.Null);
		Assert.That(monoBehaviour.value, Is.EqualTo(42));
	}

	[UnityTest]
	public IEnumerator NodeElem_Render_SecondTime_NodeChanges() {
		var oldPopNode = new NodeElem(
			key: "test",
			layer: 2,
			tag: "Player",
			active: true
		).Component(
			new TestComponent(value: 42)
		).Render(null);
		var newPopNode = new NodeElem(
			key: "test",
			layer: 3,
			tag: "Untagged",
			active: false
		).Component(
			new TestComponent(value: 7)
		).Render(oldPopNode);
		yield return null;
		Assert.That(newPopNode.obj, Is.Not.Null);
		Assert.That(newPopNode.obj.layer, Is.EqualTo(2));
		Assert.That(newPopNode.obj.tag, Is.EqualTo("Player"));
		Assert.That(newPopNode.obj.activeSelf, Is.False);
		var monoBehaviour = newPopNode.obj.GetComponent<TestMonoBehaviour>();
		Assert.That(monoBehaviour, Is.Not.Null);
		Assert.That(monoBehaviour.value, Is.EqualTo(42));
	}

	[Test]
	public void NodeElem_Render_SecondTime_PropChanges() {
		var oldPopNode = new NodeElem(
			"test"
		).Component(
			new TestComponent(value: 42)
		).Render(null);
		var newPopNode = new NodeElem(
			"test"
		).Component(
			new TestComponent(value: 7)
		).Render(oldPopNode);
		var monoBehaviour = newPopNode.obj.GetComponent<TestMonoBehaviour>();
		Assert.That(monoBehaviour, Is.Not.Null);
		Assert.That(monoBehaviour.value, Is.EqualTo(7));
	}

	[UnityTest]
	public IEnumerator NodeElem_Render_SecondTime_ComponentsChange() {
		var oldPopNode = new NodeElem(
			"test"
		).Component(
			new TestComponent(value: 42)
		).Render(null);
		var newPopNode = new NodeElem(
			"test"
		).Component(
			new Test2Component(value: "foo")
		).Render(oldPopNode);
		yield return null;
		var test1Behaviour = newPopNode.obj.GetComponent<TestMonoBehaviour>();
		var test2Behaviour = newPopNode.obj.GetComponent<Test2MonoBehaviour>();
		Assert.That(test1Behaviour, Is.Null);
		Assert.That(test2Behaviour, Is.Not.Null);
		Assert.That(test2Behaviour.value, Is.EqualTo("foo"));
	}

	[UnityTest]
	public IEnumerator Renderer_Render() {
		var renderer = new UReact.Renderer();
		GameObject obj1, obj2;
		TestMonoBehaviour comp1;
		Test2MonoBehaviour comp2;

		renderer.Render(new NodeElem(
			"node1"
		).Component(
			new TestComponent(value: 42)
		));

		obj1 = GameObject.Find("node1");
		Assert.That(obj1, Is.Not.Null);
		comp1 = obj1.GetComponent<TestMonoBehaviour>();
		Assert.That(comp1, Is.Not.Null);
		Assert.That(comp1.value, Is.EqualTo(42));

		renderer.Render(new NodeElem(
			"node1"
		).Component(
			new TestComponent(value: 42)
		).Child(
			new NodeElem(
				"node2"
			).Component(
				new Test2Component(value: "foo")
			)
		));

		obj1 = GameObject.Find("node1");
		Assert.That(obj1, Is.Not.Null);
		Assert.That(obj1.transform.childCount, Is.EqualTo(1));
		obj2 = obj1.transform.GetChild(0).gameObject;
		Assert.That(obj2, Is.Not.Null);
		Assert.That(obj2.name, Is.EqualTo("node2"));
		comp2 = obj2.GetComponent<Test2MonoBehaviour>();
		Assert.That(comp2, Is.Not.Null);
		Assert.That(comp2.value, Is.EqualTo("foo"));

		renderer.Render(new NodeElem(
			"node2"
		).Component(
			new Test2Component(value: "bar")
		).Child(
			new NodeElem(
				"node1"
			).Component(
				new TestComponent(value: 42)
			)
		));

		obj1 = GameObject.Find("node2");
		Assert.That(obj1, Is.Not.Null);
		comp2 = obj1.GetComponent<Test2MonoBehaviour>();
		Assert.That(comp2, Is.Not.Null);
		Assert.That(comp2.value, Is.EqualTo("bar"));
		Assert.That(obj1.transform.childCount, Is.EqualTo(1));
		obj2 = obj1.transform.GetChild(0).gameObject;
		Assert.That(obj2, Is.Not.Null);
		Assert.That(obj2.name, Is.EqualTo("node1"));
		comp1 = obj2.GetComponent<TestMonoBehaviour>();
		Assert.That(comp1, Is.Not.Null);
		Assert.That(comp1.value, Is.EqualTo(42));

		renderer.Render(new NodeElem(
			"node1"
		).Component(
			new Test2Component(value: "bar")
		));
		yield return null;

		obj1 = GameObject.Find("node1");
		Assert.That(obj1, Is.Not.Null);
		comp2 = obj1.GetComponent<Test2MonoBehaviour>();
		Assert.That(comp2, Is.Not.Null);
		Assert.That(comp2.value, Is.EqualTo("bar"));
		Assert.That(obj1.transform.childCount, Is.EqualTo(0));
		obj2 = GameObject.Find("node2");
		Assert.That(obj2, Is.Null);
	}
}