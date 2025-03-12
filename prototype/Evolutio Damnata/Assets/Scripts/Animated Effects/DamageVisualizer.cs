using System.Collections;
using TMPro;
using UnityEngine;

/**
 * This class is responsible for creating the damage number visual effect.
 * It creates a text object with the damage number and animates it by moving it up and fading it out.
 */

public class DamageVisualizer : MonoBehaviour
{
    public GameObject CreateDamageNumber(MonoBehaviour callerMono, float damageNumber, Vector3 position, GameObject prefab)
    {
        // Find the Canvas
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("Canvas not found! Ensure a Canvas exists in the scene.");
            return null;
        }

        // Instantiate the damage number under the Canvas
        GameObject number = Instantiate(prefab, canvas.transform);
        number.transform.position = position;

        // Set the damage number text
        TextMeshProUGUI text = number.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = "-" + damageNumber.ToString();
        }
        else
        {
            Debug.LogError("Prefab is missing a TextMeshProUGUI component!");
        }

        // Start the animation coroutine
        callerMono.StartCoroutine(AnimateText(number));

        return number; // Return the instance
    }

    private IEnumerator AnimateText(GameObject visual)
    {
        float duration = 1f; // Animation duration in seconds
        float elapsedTime = 0f;
        TextMeshProUGUI text = visual.GetComponent<TextMeshProUGUI>();

        if (text == null)
        {
            Debug.LogError("Damage number object is missing TextMeshProUGUI component!");
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
