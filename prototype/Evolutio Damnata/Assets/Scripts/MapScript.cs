using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JetBrains.Annotations;
using System;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MapScript : MonoBehaviour
{
    [SerializeField]
    Canvas mainCanvas;
    RectTransform rectTransform;
    public string currentSelectedRoom = "None";

    void generateMap()
    {
        rectTransform = mainCanvas.GetComponent<RectTransform>();
        Texture2D drawOnTex = new Texture2D((int)rectTransform.rect.width, (int)rectTransform.rect.height, TextureFormat.ARGB4444, true);
        Sprite textTex = Sprite.Create(drawOnTex, new Rect(0, 0, drawOnTex.width, drawOnTex.height), Vector2.zero);
        mainCanvas.GetComponent<Image>().sprite = textTex;

        // Set a random background image for the mainCanvas
        GlobalResources globalResources = GameObject.Find("ResourceManagaer")?.GetComponent<GlobalResources>();
        if (globalResources == null)
        {
            Debug.LogError("GlobalResources not found!");
            return;
        }

        if (globalResources.dungeonRooms == null || globalResources.dungeonRooms.Count == 0)
        {
            Debug.LogError("Dungeon rooms list is null or empty!");
            return;
        }

        Sprite newBackgroundImage = globalResources.dungeonRooms[Random.Range(0, globalResources.dungeonRooms.Count)];
        currentSelectedRoom = newBackgroundImage.name;
        mainCanvas.GetComponent<Image>().sprite = newBackgroundImage;
    }

    // Start is called before the first frame update
    void Start()
    {
        generateMap();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
