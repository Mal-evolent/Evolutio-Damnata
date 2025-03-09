using System.Collections;
using TMPro;
using UnityEngine;


/**
 * This class is responsible for creating the damage number visual effect.
 * It creates a text object with the damage number and animates it by moving it up and fading it out.
 */

public class DamageVisualizer : MonoBehaviour
{
    public void createDamageNumber(MonoBehaviour callerMono, float damageNumber, Vector3 position, GameObject prefab)
    {
        GameObject number = GameObject.Instantiate(prefab, GameObject.Find("Canvas").transform);
        number.transform.position = new Vector3(position.x, position.y, 0f);
        TextMeshProUGUI text = number.GetComponent<TextMeshProUGUI>();
        text.text = "-" + damageNumber.ToString();
        callerMono.StartCoroutine(AnimateText(number));
    }

    private IEnumerator AnimateText(GameObject visual)
    {
        int end = 100;
        for (int i = 0; i < end; i++)
        {
            visual.transform.position += new Vector3(0, 1, 0);
            visual.GetComponent<TextMeshProUGUI>().color -= new Color(0, 0, 0, 0.01f);
            yield return new WaitForSeconds(0.01f);
        }
        GameObject.Destroy(visual);
    }
}
