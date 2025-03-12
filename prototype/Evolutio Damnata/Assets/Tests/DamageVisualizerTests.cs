using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TMPro;

public class DamageVisualizerTests
{
    private GameObject canvas;
    private GameObject damageVisualizerPrefab;
    private GameObject damageNumberPrefab;
    private TestMonoBehaviour testMonoBehaviour;

    private class TestMonoBehaviour : MonoBehaviour { }

    [SetUp]
    public void SetUp()
    {
        // Create a Canvas for the test
        canvas = new GameObject("Canvas");
        canvas.AddComponent<Canvas>();

        // Create a prefab for DamageVisualizer
        damageVisualizerPrefab = new GameObject("DamageVisualizer");
        damageVisualizerPrefab.AddComponent<DamageVisualizer>();

        // Create a prefab for the damage number
        damageNumberPrefab = new GameObject("DamageNumber");
        damageNumberPrefab.AddComponent<TextMeshProUGUI>();

        // Create a GameObject with the TestMonoBehaviour
        GameObject testMonoBehaviourObject = new GameObject("TestMonoBehaviour");
        testMonoBehaviour = testMonoBehaviourObject.AddComponent<TestMonoBehaviour>();
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up after each test
        GameObject.Destroy(canvas);
        GameObject.Destroy(damageVisualizerPrefab);
        GameObject.Destroy(damageNumberPrefab);
        GameObject.Destroy(testMonoBehaviour.gameObject);
    }

    [UnityTest]
    public IEnumerator DamageNumberIsCreatedAndDestroyed()
    {
        // Instantiate the DamageVisualizer
        GameObject damageVisualizerObject = GameObject.Instantiate(damageVisualizerPrefab);
        DamageVisualizer damageVisualizer = damageVisualizerObject.GetComponent<DamageVisualizer>();

        // Call CreateDamageNumber and store the instance
        Vector3 position = new Vector3(0, 1, 0);
        float damageNumber = 50f;

        GameObject damageNumberInstance = damageVisualizer.CreateDamageNumber(
            testMonoBehaviour, damageNumber, position, damageNumberPrefab
        );

        // Ensure the damage number instance was created
        Assert.IsNotNull(damageNumberInstance, "Damage number instance was not created.");

        // Get the TextMeshProUGUI from the instance
        TextMeshProUGUI text = damageNumberInstance.GetComponent<TextMeshProUGUI>();
        Assert.IsNotNull(text, "Damage number text object was not created.");
        Assert.AreEqual("-50", text.text, "Damage number text is incorrect.");
        Assert.AreEqual(position, damageNumberInstance.transform.position, "Initial position of damage number is incorrect.");

        // Wait for the animation duration + buffer time
        yield return new WaitForSeconds(1.1f);

        // Ensure the specific damage number instance is destroyed
        Assert.IsTrue(damageNumberInstance == null || !damageNumberInstance, "Damage number instance was not destroyed.");
    }
}
