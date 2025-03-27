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
        // Create a test EntityManager
        GameObject entityObject = new GameObject("TestEntity");
        EntityManager entityManager = entityObject.AddComponent<EntityManager>();

        // Instantiate the DamageVisualizer
        GameObject damageVisualizerObject = GameObject.Instantiate(damageVisualizerPrefab);
        DamageVisualizer damageVisualizer = damageVisualizerObject.GetComponent<DamageVisualizer>();

        // Call CreateDamageNumber with EntityManager
        Vector3 position = new Vector3(0, 1, 0);
        float damageNumber = 50f;

        GameObject damageNumberInstance = damageVisualizer.CreateDamageNumber(
            entityManager, damageNumber, position, damageNumberPrefab
        );

        // Rest of the test remains the same...
        yield return new WaitForSeconds(1.1f);

        // Clean up
        GameObject.Destroy(entityObject);
    }
}
