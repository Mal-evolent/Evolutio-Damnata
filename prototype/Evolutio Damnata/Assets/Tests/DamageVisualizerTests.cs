using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TMPro;

public class DamageVisualizerTests : MonoBehaviour
{
    private GameObject canvas;
    private GameObject damageVisualizerPrefab;
    private GameObject damageNumberPrefab;

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
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up after each test
        GameObject.Destroy(canvas);
        GameObject.Destroy(damageVisualizerPrefab);
        GameObject.Destroy(damageNumberPrefab);
    }

    [UnityTest]
    public IEnumerator DamageNumberIsCreatedAndAnimated()
    {
        // Instantiate the DamageVisualizer
        GameObject damageVisualizerObject = GameObject.Instantiate(damageVisualizerPrefab);
        DamageVisualizer damageVisualizer = damageVisualizerObject.GetComponent<DamageVisualizer>();

        // Call createDamageNumber
        Vector3 position = new Vector3(0, 0, 0);
        float damageNumber = 50f;
        damageVisualizer.createDamageNumber(this, damageNumber, position, damageNumberPrefab);

        // Wait for the next frame to ensure the damage number is created
        yield return null;

        // Verify the damage number is created
        TextMeshProUGUI text = GameObject.FindObjectOfType<TextMeshProUGUI>();
        Assert.IsNotNull(text, "Damage number text object was not created.");
        Assert.AreEqual("-50", text.text, "Damage number text is incorrect.");

        // Verify the initial position
        Assert.AreEqual(new Vector3(0, 0, 0), text.transform.position, "Initial position of damage number is incorrect.");

        // Wait for the animation to complete
        yield return new WaitForSeconds(1.01f);

        // Verify the damage number is destroyed after animation
        text = GameObject.FindObjectOfType<TextMeshProUGUI>();
        Assert.IsNull(text, "Damage number text object was not destroyed after animation.");
    }
}
