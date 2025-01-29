using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
public class combatStage : MonoBehaviour
{
    [SerializeField]
    public GameObject manaBar;
    [SerializeField]
    public GameObject manaText;
    public int currentMana = 10;
    public TMP_Text turnText;

    [SerializeField]
    CardManager cardManager;
    [SerializeField]
    public CardLibrary cardLibrary;

    [SerializeField]
    CardOutlineManager cardOutlineManager;

    [SerializeField]
    Canvas battleField;

    [SerializeField]
    SpritePositioning spritePositioning;

    List<GameObject> playerMonsters = new List<GameObject>();

    private bool buttonsInitialized = false;

    // Button dimensions
    private readonly Vector2 buttonSize = new Vector2(217.9854f, 322.7287f);

    // This function will be kept
    public void interactableHighlights()
    {
        if (buttonsInitialized) return;

        for (int i = 0; i < spritePositioning.instantiatedPlaceHolders.Count; i++)
        {
            if (spritePositioning.instantiatedPlaceHolders[i] == null)
            {
                Debug.LogError($"Placeholder at index {i} is null!");
                continue;
            }

            // Set RaycastTarget to false for the placeholder outline
            Image placeholderImage = spritePositioning.instantiatedPlaceHolders[i].GetComponent<Image>();
            if (placeholderImage != null)
            {
                placeholderImage.raycastTarget = false;
            }

            // Create a new GameObject for the Button
            GameObject buttonObject = new GameObject($"Button_Outline_{i}");
            buttonObject.transform.SetParent(spritePositioning.instantiatedPlaceHolders[i].transform, false); // Add as a child of the Placeholder
            buttonObject.transform.localPosition = Vector3.zero; // Center the Button inside the Placeholder

            // Add required components to make it a Button
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = buttonSize; // Set the size of the Button to match the placeholder size

            Button buttonComponent = buttonObject.AddComponent<Button>();

            // Optional: Add an Image component to visualize the Button
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new UnityEngine.Color(1, 1, 1, 0); // Transparent background for the Button

            // Add onClick functionality
            int temp_i = i;

            buttonComponent.onClick.AddListener(() =>
            {
                Debug.Log($"Button inside Placeholder {temp_i} clicked!");

                if (cardManager.currentSelectedCard != null)
                {
                    Debug.Log($"Card {cardManager.currentSelectedCard.name} used on monster {temp_i}");

                    // Spawn card on field
                    spawnPlayerCard(cardManager.currentSelectedCard.name, temp_i);

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

                    cardManager.currentSelectedCard = null;

                    // Deactivate placeholders
                    placeHolderActiveState(false);
                }
                else
                {
                    Debug.Log("No card selected to use on monster!");
                }
            });
        }

        buttonsInitialized = true;
    }

    public void spawnPlayerCard(string cardName, int whichOutline)
    {
        if (whichOutline < 0 || whichOutline >= spritePositioning.instantiatedPlaceHolders.Count)
        {
            Debug.LogError($"Invalid outline index: {whichOutline}");
            return;
        }

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
        Image placeholderImage = spritePositioning.instantiatedPlaceHolders[whichOutline].GetComponent<Image>();
        if (placeholderImage != null)
        {
            placeholderImage.sprite = cardLibrary.cardImageGetter(cardName);
        }

        // Ensure playerMonsters list has enough elements
        while (playerMonsters.Count <= whichOutline)
        {
            GameObject newMonster = new GameObject($"Monster_{whichOutline}");
            newMonster.AddComponent<MonsterScript>(); // Add the MonsterScript component to the new monster
            playerMonsters.Add(newMonster);
        }

        MonsterScript monsterScript = playerMonsters[whichOutline].GetComponent<MonsterScript>();
        if (monsterScript == null)
        {
            Debug.LogError($"MonsterScript not found on playerMonsters[{whichOutline}]");
            return;
        }

        monsterScript.placed = true;

        // Setting monster's attributes using CardLibrary.CardData
        foreach (CardLibrary.CardData cardData in cardLibrary.cardDataList)
        {
            if (cardName == cardData.CardName)
            {
                monsterScript.setHealth(cardData.Health);
                monsterScript.SetAttackDamage(cardData.AttackPower);

                if (monsterScript._healthBar != null)
                {
                    monsterScript._healthBar.SetActive(true);
                }
                break;
            }
        }

        GameObject buttonObject = new GameObject($"Select_Button_{whichOutline}");
        buttonObject.transform.SetParent(playerMonsters[whichOutline].transform, false); // Add as a child of the Outline
        buttonObject.transform.localPosition = Vector3.zero; // Center the Button inside the Outline

        // Add required components to make it a Button
        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = buttonSize; // Use the defined button size

        Button buttonComponent = buttonObject.AddComponent<Button>();

        // Optional: Add an Image component to visualize the Button
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new UnityEngine.Color(1, 1, 1, 0); // Transparent background for the Button
        buttonComponent.onClick.AddListener(() =>
        {
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
        // Start the coroutine to wait for room selection
        StartCoroutine(spritePositioning.WaitForRoomSelection());

        // Set all placeholders to be inactive initially
        StartCoroutine(spritePositioning.SetAllPlaceHoldersInactive());

        // Initialize interactable highlights
        StartCoroutine(InitializeInteractableHighlights());
    }

    private IEnumerator InitializeInteractableHighlights()
    {
        // Wait until placeholders are instantiated
        while (spritePositioning.instantiatedPlaceHolders.Count == 0)
        {
            yield return null; // Wait for the next frame
        }

        // Initialize interactable highlights
        interactableHighlights();
    }

    private void Update()
    {
        // Check if a card is selected and update placeholder visibility
        if (cardManager.currentSelectedCard != null)
        {
            placeHolderActiveState(true);
        }
        else
        {
            placeHolderActiveState(false);
        }
    }

    private void placeHolderActiveState(bool active)
    {
        for (int i = 0; i < spritePositioning.instantiatedPlaceHolders.Count; i++)
        {
            if (spritePositioning.instantiatedPlaceHolders[i] != null)
            {
                Image placeholderImage = spritePositioning.instantiatedPlaceHolders[i].GetComponent<Image>();
                if (placeholderImage != null && placeholderImage.sprite != null)
                {
                    if (placeholderImage.sprite.name == "wizard_outline")
                    {
                        spritePositioning.instantiatedPlaceHolders[i].SetActive(active);
                    }
                }
            }
        }
    }
}
