using System.Collections;
using TMPro;
using UnityEngine;

/**
 * This class is responsible for creating the damage and healing number visual effects.
 * It creates a text object with the number and animates it by moving it up and fading it out.
 */
public class DamageVisualizer : MonoBehaviour
{
    public GameObject CreateDamageNumber(MonoBehaviour callerMono, float damageNumber, Vector3 position, GameObject prefab)
    {
        return CreateNumber(callerMono, damageNumber, position, prefab, "-", Color.red);
    }

    public GameObject CreateHealingNumber(MonoBehaviour callerMono, float healNumber, Vector3 position, GameObject prefab)
    {
        return CreateNumber(callerMono, healNumber, position, prefab, "+", Color.green);
    }

    private GameObject CreateNumber(MonoBehaviour callerMono, float numberValue, Vector3 position, GameObject prefab, string prefix, Color color)
    {
        // Find the Canvas
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("Canvas not found! Ensure a Canvas exists in the scene.");
            return null;
        }

        // Instantiate the number under the Canvas
        GameObject numberObj = Instantiate(prefab, canvas.transform);
        numberObj.transform.position = position;

        // Set the number text
        TextMeshProUGUI text = numberObj.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = prefix + numberValue.ToString();
            text.color = color; // Set the appropriate color
        }
        else
        {
            Debug.LogError("Prefab is missing a TextMeshProUGUI component!");
        }

        // Start the animation coroutine
        callerMono.StartCoroutine(AnimateText(numberObj));

        return numberObj;
    }

    private IEnumerator AnimateText(GameObject visual)
    {
        float duration = 1f; // Animation duration in seconds
        float elapsedTime = 0f;
        TextMeshProUGUI text = visual.GetComponent<TextMeshProUGUI>();

        if (text == null)
        {
            Debug.LogError("Number object is missing TextMeshProUGUI component!");
            yield break;
        }

        Color startColor = text.color;
        Vector3 startPosition = visual.transform.position;
        Vector3 targetPosition = startPosition + new Vector3(0, 20f, 0);

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // Smoothly move the text upwards
            visual.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            // Gradually fade out the text
            text.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1, 0, t));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(visual);
    }
}