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

    private bool buttonsInitialized = false;

    // Button dimensions
    private readonly Vector2 buttonSize = new Vector2(217.9854f, 322.7287f);

    // This function will be kept
    public void interactableHighlights()
    {
        if (buttonsInitialized) return;

        for (int i = 0; i < spritePositioning.activeEntities.Count; i++)
        {
            if (spritePositioning.activeEntities[i] == null)
            {
                Debug.LogError($"Placeholder at index {i} is null!");
                continue;
            }

            // Set RaycastTarget to false for the placeholder outline
            Image placeholderImage = spritePositioning.activeEntities[i].GetComponent<Image>();
            if (placeholderImage != null)
            {
                placeholderImage.raycastTarget = false;
            }

            // Create a new GameObject for the Button
            GameObject buttonObject = new GameObject($"Button_Outline_{i}");
            buttonObject.transform.SetParent(spritePositioning.activeEntities[i].transform, false); // Add as a child of the Placeholder
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

                    // Capture the outline's Image component
                    Image outlineImage = spritePositioning.activeEntities[temp_i].GetComponent<Image>();

                    // Spawn card on field
                    spawnPlayerCard(cardManager.currentSelectedCard.name, temp_i, outlineImage);

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

    public void spawnPlayerCard(string cardName, int whichOutline, Image outlineImage)
    {
        if (whichOutline < 0 || whichOutline >= spritePositioning.activeEntities.Count)
        {
            Debug.LogError($"Invalid outline index: {whichOutline}");
            return;
        }

        // Check if the placeholder is already populated
        EntityManager existingEntityManager = spritePositioning.activeEntities[whichOutline].GetComponent<EntityManager>();
        if (existingEntityManager != null && existingEntityManager.placed)
        {
            Debug.LogError("Cannot place a card in an already populated placeholder.");
            return;
        }

        int cardCost = 0;
        CardLibrary.CardData selectedCardData = null;
        foreach (CardLibrary.CardData cardData in cardLibrary.cardDataList)
        {
            if (cardName == cardData.CardName)
            {
                if (currentMana < cardData.ManaCost)
                {
                    Debug.Log($"Not enough mana. Card costs {cardData.ManaCost}, player has {currentMana}");
                    return; // Bail if there isn't enough mana
                }
                else
                {
                    cardCost = cardData.ManaCost;
                    selectedCardData = cardData;
                    break;
                }
            }
        }

        if (selectedCardData == null)
        {
            Debug.LogError($"Card data not found for card name: {cardName}");
            return;
        }

        // Remove outline/highlight on current card in hand
        cardOutlineManager.RemoveHighlight();

        // Remove card from hand
        Destroy(cardManager.currentSelectedCard);
        cardManager.currentSelectedCard = null;

        // Set monster attributes
        Image placeholderImage = spritePositioning.activeEntities[whichOutline].GetComponent<Image>();
        if (placeholderImage != null)
        {
            placeholderImage.sprite = cardLibrary.cardImageGetter(cardName);
        }

        // Get the placeholder GameObject
        GameObject placeholder = spritePositioning.activeEntities[whichOutline];

        // Add the EntityManager component to the placeholder
        EntityManager entityManager = placeholder.GetComponent<EntityManager>();
        if (entityManager == null)
        {
            entityManager = placeholder.AddComponent<EntityManager>();
        }

        // Find the health bar Slider component using transform.Find
        Transform healthBarTransform = placeholder.transform.Find("healthBar");
        Slider healthBarSlider = healthBarTransform != null ? healthBarTransform.GetComponent<Slider>() : null;

        entityManager.placed = true;

        // Initialize the monster with the appropriate type, attributes, and outline image
        entityManager.InitializeMonster(EntityManager._monsterType.Friendly, selectedCardData.Health, selectedCardData.AttackPower, outlineImage, healthBarSlider);

        // Rename the placeholder to the card name
        placeholder.name = cardName;

        // Display the health bar
        displayHealthBar(placeholder, true);

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

    public void spawnEnemy()
    {

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
        while (spritePositioning.activeEntities.Count == 0)
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
        for (int i = 0; i < spritePositioning.activeEntities.Count; i++)
        {
            if (spritePositioning.activeEntities[i] != null)
            {
                Image placeholderImage = spritePositioning.activeEntities[i].GetComponent<Image>();
                if (placeholderImage != null && placeholderImage.sprite != null)
                {
                    if (placeholderImage.sprite.name == "wizard_outline")
                    {
                        spritePositioning.activeEntities[i].SetActive(active);
                    }
                }
            }
        }
    }

    private void displayHealthBar(GameObject entity, bool active)
    {
        Transform healthBarTransform = entity.transform.Find("healthBar");
        if (healthBarTransform != null)
        {
            healthBarTransform.gameObject.SetActive(active);
        }
    }
}
