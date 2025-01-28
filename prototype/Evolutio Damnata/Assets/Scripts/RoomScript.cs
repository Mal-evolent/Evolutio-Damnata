using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class RoomScript : MonoBehaviour
{
    [SerializeField]
    public GameObject manaBar;
    [SerializeField]
    public GameObject manaText;
    public int currentMana = 10;
    public TMP_Text turnText;

    [SerializeField]
    GameObject monsterPrefab;

    [SerializeField]
    Sprite placeHolderSprites;
    [SerializeField]
    int placeHolderSpriteCount = 3;

    [SerializeField]
    float _initalOffsetX = 153f;
    [SerializeField]
    float _initalOffsetY = 220f;

    [SerializeField]
    Canvas targetCanvas;

    [SerializeField]
    CardManager cardManager;
    [SerializeField]
    public CardLibrary cardLibrary;

    [SerializeField]
    CardOutlineManager cardOutlineManager;
    [SerializeField]
    List<Image> Outlines;

    List<GameObject> playerMonsters = new List<GameObject>();

    // This function will be kept
    public void interactableHighlights()
    {
        for (int i = 0; i < Outlines.Count; i++)
        {
            if (Outlines[i] == null)
            {
                Debug.LogError($"Outline at index {i} is null!");
                continue;
            }

            // Create a new GameObject for the Button
            GameObject buttonObject = new GameObject($"Button_Outline_{i}");
            buttonObject.transform.SetParent(Outlines[i].gameObject.transform, false); // Add as a child of the Outline
            buttonObject.transform.localPosition = Vector3.zero; // Center the Button inside the Outline

            // Add required components to make it a Button
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(25, 53); // Why 25x53?

            Button buttonComponent = buttonObject.AddComponent<Button>();

            // Optional: Add an Image component to visualize the Button
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new UnityEngine.Color(1, 1, 1, 0); // Transparent background for the Button

            // Add onClick functionality
            int temp_i = i;

            buttonComponent.onClick.AddListener(() =>
            {
                Debug.Log($"Button inside Outline {temp_i} clicked!");

                if (cardManager.currentSelectedCard != null)
                {
                    Debug.Log($"Card {cardManager.currentSelectedCard.name} used on monster {temp_i}");

                    // Remove card from hand
                    List<GameObject> handCardObjects = cardManager.getHandCardObjects();
                    foreach (GameObject cardObject in handCardObjects)
                    {
                        if (cardObject == cardManager.currentSelectedCard)
                        {
                            handCardObjects.Remove(cardObject);
                            Debug.Log("Removed card from hand.");
                            break;
                        }
                    }

                    // Spawn card on field
                    spawnPlayerCard(cardManager.currentSelectedCard.name, temp_i);

                    cardManager.currentSelectedCard = null;
                }
                else
                {
                    Debug.Log("No card selected to use on monster!");
                }
            });
        }
    }

    // This function will be kept
    public void spawnPlayerCard(string cardName, int whichOutline)
    {
        int cardCost = 0;
        foreach (CardLibrary.CardData cardData in cardLibrary.cardDataList)
        {
            if (cardName == cardData.CardName)
            {
                if (currentMana < cardData.ManaCost) { Debug.Log($"Not enough mana. Card costs {cardData.ManaCost}, player has {currentMana}"); return; } //bail if there isnt enough mana
                else { cardCost = cardData.ManaCost; break; }
            }
        }

        // Remove outline/highlight on current card in hand
        cardOutlineManager.RemoveHighlight();

        // Remove card from hand
        Destroy(cardManager.currentSelectedCard);
        cardManager.currentSelectedCard = null;

        // Set monster attributes
        Outlines[whichOutline].sprite = cardLibrary.cardImageGetter(cardName);

        playerMonsters[whichOutline].GetComponent<MonsterScript>().placed = true;

        // Setting monster's attributes using CardLibrary.CardData
        foreach (CardLibrary.CardData cardData in cardLibrary.cardDataList)
        {
            if (cardName == cardData.CardName)
            {
                playerMonsters[whichOutline].GetComponent<MonsterScript>().setHealth(cardData.Health);
                playerMonsters[whichOutline].GetComponent<MonsterScript>().SetAttackDamage(cardData.AttackPower);

                playerMonsters[whichOutline].GetComponent<MonsterScript>()._healthBar.SetActive(true);
                break;
            }
        }

        GameObject buttonObject = new GameObject($"Select_Button_{whichOutline}");
        buttonObject.transform.SetParent(playerMonsters[whichOutline].transform, false); // Add as a child of the Outline
        buttonObject.transform.localPosition = Vector3.zero; // Center the Button inside the Outline

        // Add required components to make it a Button
        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(25, 53); // Why 25x53?

        Button buttonComponent = buttonObject.AddComponent<Button>();

        // Optional: Add an Image component to visualize the Button
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new UnityEngine.Color(1, 1, 1, 0); // Transparent background for the Button
        buttonComponent.onClick.AddListener(() =>
        {
            //alternative needed!
            cardOutlineManager.RemoveHighlight();
            cardManager.currentSelectedCard = null;
        });

        // Decrease current mana
        currentMana -= cardCost;
        manaBar.GetComponent<Slider>().value = currentMana;
        manaText.GetComponent<TMP_Text>().text = manaBar.GetComponent<Slider>().value.ToString();

        // Play summon SFX
        AudioSource churchBells = GetComponent<AudioSource>();
        if (churchBells.isPlaying)
        {
            churchBells.Stop();
        }
        churchBells.Play();
    }

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < Outlines.Count; i++)
        {
            Outlines[i].enabled = false;
        }
        interactableHighlights();
    }
}
