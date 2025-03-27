using System.Collections;
using TMPro;
using UnityEngine;

public class DamageVisualizer : MonoBehaviour, IDamageVisualizer
{
    public GameObject CreateDamageNumber(EntityManager target, float damageNumber, Vector3 position, GameObject prefab)
    {
        return CreateNumber(target, damageNumber, position, prefab, "-", Color.red);
    }

    public GameObject CreateHealingNumber(EntityManager target, float healNumber, Vector3 position, GameObject prefab)
    {
        return CreateNumber(target, healNumber, position, prefab, "+", Color.green);
    }

    private GameObject CreateNumber(EntityManager target, float numberValue, Vector3 position, GameObject prefab, string prefix, Color color)
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
            text.color = color;
        }
        else
        {
            Debug.LogError("Prefab is missing a TextMeshProUGUI component!");
        }

        // Start the animation coroutine
        StartCoroutine(AnimateText(numberObj));

        return numberObj;
    }

    private IEnumerator AnimateText(GameObject visual)
    {
        float duration = 1f;
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
            visual.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            text.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1, 0, t));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(visual);
    }
}