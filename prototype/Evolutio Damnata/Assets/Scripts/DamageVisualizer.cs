using System.Collections;
using TMPro;
using UnityEngine;

public class DamageVisualizer
{

    public void createDamageNumber(MonoBehaviour callerMono, float damageNumber, Vector3 Position, GameObject prefab)
    {
        GameObject number = GameObject.Instantiate(prefab, GameObject.Find("Canvas").transform);
        number.transform.position = new Vector3(Position.x, Position.y, 0f);
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
            //yield return null;
        }
        GameObject.Destroy(visual);
    }
}
