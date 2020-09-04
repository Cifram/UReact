#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UReact;

class TestMonoBehaviour : MonoBehaviour {
	public int value;
}

struct TestProps {
	public int value;
}

static class TestComponent {
	public static void Render(GameObject obj, TestProps? oldProps, TestProps props) {
		if (oldProps == null) {
			obj.AddComponent<TestMonoBehaviour>().value = props.value;
		} else {
			obj.GetComponent<TestMonoBehaviour>().value = props.value;
		}
	}
}

class Test2MonoBehaviour : MonoBehaviour {
	public string? value;
}

struct Test2Props {
	public string value;
}

static class Test2Component {
	public static void Render(GameObject obj, Test2Props? oldProps, Test2Props props) {
		if (oldProps == null) {
			obj.AddComponent<Test2MonoBehaviour>().value = props.value;
		} else {
			obj.GetComponent<Test2MonoBehaviour>().value = props.value;
		}
	}
}

public class UReactTests {
	[UnityTest]
	public IEnumerator CompElem() {
		CompElem compElem1 = new CompElem<TestProps>(typeof(TestMonoBehaviour), TestComponent.Render, new TestProps { value = 42 });
		CompElem compElem2 = new CompElem<TestProps>(typeof(TestMonoBehaviour), TestComponent.Render, new TestProps { value = 7 });
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
		Assert.That(nodeElem.compElems.ContainsKey(typeof(TestProps)), Is.False);
		nodeElem.Component(typeof(TestMonoBehaviour), TestComponent.Render, new TestProps { value = 42 });
		Assert.That(nodeElem.compElems.ContainsKey(typeof(TestProps)), Is.True);
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

	[Test]
	public void NodeElem_Render_FirstTime() {
		var popNode = new NodeElem(
			"test"
		).Component(
			typeof(TestMonoBehaviour), TestComponent.Render, new TestProps { value = 42 }
		).Render(null);
		Assert.That(popNode.elem.key, Is.EqualTo("test"));
		Assert.That(popNode.obj, Is.Not.Null);
		Assert.That(popNode.obj.name, Is.EqualTo("test"));
		var monoBehaviour = popNode.obj.GetComponent<TestMonoBehaviour>();
		Assert.That(monoBehaviour, Is.Not.Null);
		Assert.That(monoBehaviour.value, Is.EqualTo(42));
	}

	[Test]
	public void NodeElem_Render_SecondTime_OnlyPropChanges() {
		var oldPopNode = new NodeElem(
			"test"
		).Component(
			typeof(TestMonoBehaviour), TestComponent.Render, new TestProps { value = 42 }
		).Render(null);
		var newPopNode = new NodeElem(
			"test"
		).Component(
			typeof(TestMonoBehaviour), TestComponent.Render, new TestProps { value = 7 }
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
			typeof(TestMonoBehaviour), TestComponent.Render, new TestProps { value = 42 }
		).Render(null);
		var newPopNode = new NodeElem(
			"test"
		).Component(
			typeof(Test2MonoBehaviour), Test2Component.Render, new Test2Props { value = "foo" }
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
			typeof(TestMonoBehaviour),
			TestComponent.Render,
			new TestProps { value = 42 }
		));

		obj1 = GameObject.Find("node1");
		Assert.That(obj1, Is.Not.Null);
		comp1 = obj1.GetComponent<TestMonoBehaviour>();
		Assert.That(comp1, Is.Not.Null);
		Assert.That(comp1.value, Is.EqualTo(42));

		renderer.Render(new NodeElem(
			"node1"
		).Component(
			typeof(TestMonoBehaviour),
			TestComponent.Render,
			new TestProps { value = 42 }
		).Child(
			new NodeElem(
				"node2"
			).Component(
				typeof(Test2MonoBehaviour),
				Test2Component.Render,
				new Test2Props { value = "foo" }
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
			typeof(Test2MonoBehaviour),
			Test2Component.Render,
			new Test2Props { value = "bar" }
		).Child(
			new NodeElem(
				"node1"
			).Component(
				typeof(TestMonoBehaviour),
				TestComponent.Render,
				new TestProps { value = 42 }
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
			typeof(Test2MonoBehaviour),
			Test2Component.Render,
			new Test2Props { value = "bar" }
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