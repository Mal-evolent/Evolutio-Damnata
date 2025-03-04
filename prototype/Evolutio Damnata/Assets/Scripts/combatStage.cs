using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class combatStage : MonoBehaviour
{
    [SerializeField]
    public GameObject manaBar;
    [SerializeField]
    public GameObject manaText;
    public int currentMana;
    public TMP_Text turnText;

    [SerializeField]
    CardManager cardManager;
    [SerializeField]
    public CardLibrary cardLibrary;

    [SerializeField]
    CardOutlineManager cardOutlineManager;
    [SerializeField]
    CombatManager combatManager;

    [SerializeField]
    Canvas battleField;

    [SerializeField]
    SpritePositioning spritePositioning;

    [SerializeField]
    DamageVisualizer damageVisualizer;

    [SerializeField]
    GameObject damageNumberPrefab;

    private bool buttonsInitialized = false;

    // Button dimensions
    private readonly Vector2 buttonSize = new Vector2(217.9854f, 322.7287f);
    private readonly Vector2 enemyButtonSize = new Vector2(114.2145f, 188.1686f); // Enemy placeholder button dimensions

    // This function will be kept
    public void interactableHighlights()
    {
        if (buttonsInitialized) return;

        // Add buttons to player entities
        for (int i = 0; i < spritePositioning.playerEntities.Count; i++)
        {
            if (spritePositioning.playerEntities[i] == null)
            {
                Debug.LogError($"Player placeholder at index {i} is null!");
                continue;
            }

            // Set RaycastTarget to false for the placeholder outline
            Image placeholderImage = spritePositioning.playerEntities[i].GetComponent<Image>();
            if (placeholderImage != null)
            {
                placeholderImage.raycastTarget = false;
            }

            // Create a new GameObject for the Button
            GameObject buttonObject = new GameObject($"Button_Outline_{i}");
            buttonObject.transform.SetParent(spritePositioning.playerEntities[i].transform, false); // Add as a child of the Placeholder
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
                Debug.Log($"Button inside Player Placeholder {temp_i} clicked!");

                if (cardManager.currentSelectedCard != null && combatManager.playerTurn)
                {
                    // Get the CardUI component to access the actual card object
                    CardUI cardUI = cardManager.currentSelectedCard.GetComponent<CardUI>();
                    if (cardUI == null)
                    {
                        Debug.LogError("CardUI component not found on current selected card!");
                        return;
                    }

                    Card cardComponent = cardUI.card;
                    if (cardComponent == null)
                    {
                        Debug.LogError("Card component not found on current selected card!");
                        return;
                    }

                    CardData cardData = cardComponent.CardType;
                    if (cardData == null)
                    {
                        Debug.LogError("CardType is null on current selected card!");
                        return;
                    }

                    if (cardData.IsMonsterCard)
                    {
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
                    else if (cardData.IsSpellCard)
                    {
                        Debug.Log("Spells cannot be placed on the field.");
                    }
                    else
                    {
                        Debug.LogError("Card type not found!");
                    }
                }
                // Makes placed cards selectable
                else if (cardManager.currentSelectedCard == null)
                {
                    EntityManager entityManager = spritePositioning.playerEntities[temp_i].GetComponent<EntityManager>();
                    if (entityManager != null && entityManager.placed)
                    {
                        cardManager.currentSelectedCard = spritePositioning.playerEntities[temp_i];
                    }
                }
                else
                {
                    Debug.Log("No card selected or not the players turn!");
                    cardOutlineManager.RemoveHighlight();
                }
            });


        }

        // Add buttons to enemy placeholders
        for (int i = 0; i < spritePositioning.enemyEntities.Count; i++)
        {
            if (spritePositioning.enemyEntities[i] == null)
            {
                Debug.LogError($"Enemy placeholder at index {i} is null!");
                continue;
            }

            // Store the placeholder's world position before parenting
            Vector3 originalWorldPos = spritePositioning.enemyEntities[i].transform.position;
            Vector3 originalScale = spritePositioning.enemyEntities[i].transform.localScale;
            Quaternion originalRotation = spritePositioning.enemyEntities[i].transform.rotation;

            // Create a new GameObject for the Button
            GameObject buttonObject = new GameObject($"Enemy_Button_Outline_{i}");

            // Set the button's parent to the top-level canvas (so it's clickable)
            buttonObject.transform.SetParent(battleField.transform, false);

            // Restore the button's world position
            buttonObject.transform.position = originalWorldPos;

            // Add required UI components
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = enemyButtonSize; // Use the enemy button size

            Button buttonComponent = buttonObject.AddComponent<Button>();

            // Optional: Add an Image component for debugging (can be made transparent)
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new UnityEngine.Color(1, 1, 1, 0);
            buttonImage.raycastTarget = true;

            // Convert the placeholder's world position into the button's local space
            Vector3 localPos = buttonObject.transform.InverseTransformPoint(originalWorldPos);

            // Now make the placeholder a child of the button
            spritePositioning.enemyEntities[i].transform.SetParent(buttonObject.transform, false);

            // Restore the correct local position, scale, and rotation
            spritePositioning.enemyEntities[i].transform.localPosition = localPos;
            spritePositioning.enemyEntities[i].transform.localScale = originalScale;
            spritePositioning.enemyEntities[i].transform.rotation = originalRotation;

            // Add onClick functionality
            int temp_i = i;
            buttonComponent.onClick.AddListener(() =>
            {
                Debug.Log($"Button inside Enemy Placeholder {temp_i} clicked!");
                // Add attack logic here
            });

            Debug.Log($"Button {i} created, parented correctly, and position fixed.");
        }

        buttonsInitialized = true;
    }

    public void checkCardType(string cardName, int whichOutline)
    {
        MonsterCard selectedMonsterCard = cardManager.currentSelectedCard.GetComponent<MonsterCard>();
        SpellCard selectedSpellCard = cardManager.currentSelectedCard.GetComponent<SpellCard>();

        if (selectedMonsterCard != null)
        {
            spawnPlayerCard(cardName, whichOutline);
        }
        else if (selectedSpellCard != null)
        {
            Debug.Log("Spells cannot be placed on the field.");
        }
        else
        {
            Debug.LogError("Card type not found!");
        }
    }

    public void spawnPlayerCard(string cardName, int whichOutline)
    {
        if (whichOutline < 0 || whichOutline >= spritePositioning.playerEntities.Count)
        {
            Debug.LogError($"Invalid outline index: {whichOutline}");
            return;
        }

        // Check if the placeholder is already populated
        GameObject placeholder = spritePositioning.playerEntities[whichOutline];
        if (placeholder == null)
        {
            Debug.LogError($"Placeholder at index {whichOutline} is null!");
            return;
        }

        EntityManager existingEntityManager = placeholder.GetComponent<EntityManager>();
        if (existingEntityManager == null)
        {
            Debug.LogError($"EntityManager component not found on placeholder at index {whichOutline}!");
        }

        // Find the selected card data
        int cardCost = 0;
        CardData selectedCardData = null;
        foreach (CardData cardData in cardLibrary.cardDataList)
        {
            if (cardName == cardData.CardName)
            {
                if (currentMana < cardData.ManaCost)
                {
                    Debug.LogError($"Not enough mana. Card costs {cardData.ManaCost}, player has {currentMana}");
                    cardOutlineManager.RemoveHighlight();
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

        // If the card is not a spell card and the placeholder is already populated, return
        if (!selectedCardData.IsSpellCard && existingEntityManager != null && existingEntityManager.placed)
        {
            Debug.LogError("Cannot place a card in an already populated placeholder.");
            cardOutlineManager.RemoveHighlight();
            return;
        }

        // Remove outline/highlight on current card in hand
        cardOutlineManager.RemoveHighlight();

        // If it's a spell card, set the target entity and apply the effect
        if (selectedCardData.IsSpellCard)
        {
            if (cardManager.currentSelectedCard == null)
            {
                Debug.LogError("Current selected card is null when trying to cast spell!");
            }
            else
            {
                Debug.Log($"Checking card: {cardManager.currentSelectedCard.name}, Components: {string.Join(", ", cardManager.currentSelectedCard.GetComponents<Component>().Select(c => c.GetType().Name))}");
                SpellCard spellCard = cardManager.currentSelectedCard.GetComponent<SpellCard>();
                if (spellCard == null)
                {
                    Debug.LogWarning("SpellCard component not found on current selected card! Adding SpellCard component.");
                    spellCard = cardManager.currentSelectedCard.AddComponent<SpellCard>();

                    // Copy properties from CardData to SpellCard
                    spellCard.CardName = selectedCardData.CardName;
                    spellCard.CardImage = selectedCardData.CardImage;
                    spellCard.Description = selectedCardData.Description;
                    spellCard.ManaCost = selectedCardData.ManaCost;
                    spellCard.EffectTypes = selectedCardData.EffectTypes; // Updated to use EffectTypes
                    spellCard.EffectValue = selectedCardData.EffectValue;
                    spellCard.Duration = selectedCardData.Duration;
                }
                Debug.Log("Applying spell effect to target entity.");
                spellCard.targetEntity = existingEntityManager;
                spellCard.Play();
            }
        }

        // Remove card from hand
        if (cardManager.currentSelectedCard == null)
        {
            Debug.LogError("Current selected card is null!");
        }
        else
        {
            Destroy(cardManager.currentSelectedCard);
            cardManager.currentSelectedCard = null;
        }

        // Set monster attributes
        Image placeholderImage = placeholder.GetComponent<Image>();
        if (placeholderImage == null)
        {
            Debug.LogError("Image component not found on placeholder!");
        }
        else if (!selectedCardData.IsSpellCard)
        {
            placeholderImage.sprite = cardLibrary.cardImageGetter(cardName);
        }

        // Apply positioning, scale, and rotation
        RectTransform rectTransform = placeholder.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("RectTransform component not found on placeholder!");
        }
        else
        {
            PositionData positionData = spritePositioning.GetPlayerPositionsForCurrentRoom()[whichOutline];
            rectTransform.anchoredPosition = positionData.Position;
            rectTransform.sizeDelta = positionData.Size;
            rectTransform.localScale = positionData.Scale;
            rectTransform.rotation = positionData.Rotation;
        }

        // Add the EntityManager component to the placeholder
        EntityManager entityManager = placeholder.GetComponent<EntityManager>();
        if (entityManager == null)
        {
            entityManager = placeholder.AddComponent<EntityManager>();
        }

        // Find the health bar Slider component using transform.Find
        Transform healthBarTransform = placeholder.transform.Find("healthBar");
        if (healthBarTransform == null)
        {
            Debug.LogError("Health bar transform not found on placeholder!");
        }
        Slider healthBarSlider = healthBarTransform != null ? healthBarTransform.GetComponent<Slider>() : null;
        if (healthBarSlider == null)
        {
            Debug.LogError("Slider component not found on health bar transform!");
        }

        // Initialize the monster with the appropriate type, attributes, and outline image only if it's not a spell card or the placeholder is empty
        if (!selectedCardData.IsSpellCard || (existingEntityManager == null || !existingEntityManager.placed))
        {
            entityManager.InitializeMonster(EntityManager._monsterType.Friendly, selectedCardData.Health, selectedCardData.AttackPower, healthBarSlider, placeholderImage, damageVisualizer, damageNumberPrefab);
        }

        // Check if the placeholder is already occupied by a placed monster card
        bool isOccupied = existingEntityManager != null && existingEntityManager.placed;

        // Set entity.placed to false only if the placeholder is empty
        if (!isOccupied)
        {
            entityManager.placed = !selectedCardData.IsSpellCard;
        }

        // Display or hide the health bar based on whether the placeholder is occupied by a placed monster card
        if (selectedCardData.IsSpellCard && !isOccupied)
        {
            displayHealthBar(placeholder, false);
        }
        else
        {
            displayHealthBar(placeholder, true);
        }

        // Rename the placeholder to the card name unless it's a spell card
        if (!selectedCardData.IsSpellCard)
        {
            placeholder.name = cardName;
        }

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

    public void spawnEnemy(string cardName, int whichOutline)
    {
        if (whichOutline < 0 || whichOutline >= spritePositioning.enemyEntities.Count)
        {
            Debug.LogError($"Invalid outline index: {whichOutline}");
            return;
        }

        // Check if the placeholder is already populated
        GameObject enemyPlaceholder = spritePositioning.enemyEntities[whichOutline];
        if (enemyPlaceholder == null)
        {
            Debug.LogError($"Placeholder at index {whichOutline} is null!");
            return;
        }

        EntityManager existingEntityManager = enemyPlaceholder.GetComponent<EntityManager>();
        if (existingEntityManager != null && existingEntityManager.placed)
        {
            Debug.LogError("Cannot place a card in an already populated placeholder.");
            return;
        }

        CardData selectedCardData = null;
        foreach (CardData cardData in cardLibrary.cardDataList)
        {
            if (cardName == cardData.CardName)
            {
                selectedCardData = cardData;
                break;
            }
        }

        if (selectedCardData == null)
        {
            Debug.LogError($"Card data not found for card name: {cardName}");
            return;
        }

        // Set monster attributes
        Image placeholderImage = enemyPlaceholder.GetComponent<Image>();
        if (placeholderImage != null)
        {
            placeholderImage.sprite = cardLibrary.cardImageGetter(cardName);
        }


        // Add the EntityManager component to the placeholder
        EntityManager entityManager = enemyPlaceholder.GetComponent<EntityManager>();
        if (entityManager == null)
        {
            entityManager = enemyPlaceholder.AddComponent<EntityManager>();
        }

        // Find the health bar Slider component using transform.Find
        Transform healthBarTransform = enemyPlaceholder.transform.Find("healthBar");
        Slider healthBarSlider = healthBarTransform != null ? healthBarTransform.GetComponent<Slider>() : null;

        entityManager.placed = true;

        // Initialize the monster with the appropriate type, attributes, and outline image
        entityManager.InitializeMonster(EntityManager._monsterType.Enemy, selectedCardData.Health, selectedCardData.AttackPower, healthBarSlider, placeholderImage, damageVisualizer, damageNumberPrefab);

        // Rename the placeholder to the card name
        enemyPlaceholder.name = cardName;

        // Display the health bar
        displayHealthBar(enemyPlaceholder, true);

        // Activate the placeholder game object
        enemyPlaceholder.SetActive(true);
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
        while (spritePositioning.playerEntities.Count == 0)
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
        for (int i = 0; i < spritePositioning.playerEntities.Count; i++)
        {
            if (spritePositioning.playerEntities[i] != null)
            {
                Image placeholderImage = spritePositioning.playerEntities[i].GetComponent<Image>();
                if (placeholderImage != null && placeholderImage.sprite != null)
                {
                    if (placeholderImage.sprite.name == "wizard_outline")
                    {
                        spritePositioning.playerEntities[i].SetActive(active);
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
