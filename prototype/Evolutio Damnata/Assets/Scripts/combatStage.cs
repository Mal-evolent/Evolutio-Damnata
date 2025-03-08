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
    public SpritePositioning spritePositioning;

    [SerializeField]
    DamageVisualizer damageVisualizer;

    [SerializeField]
    GameObject damageNumberPrefab;

    private bool buttonsInitialized = false;

    // Button dimensions
    private readonly Vector2 buttonSize = new Vector2(217.9854f, 322.7287f);
    private readonly Vector2 enemyButtonSize = new Vector2(114.2145f, 188.1686f);

    // This function will be kept
    public void interactableHighlights()
    {
        if (buttonsInitialized) return;

        AddButtonsToPlayerEntities();
        AddButtonsToEnemyEntities();

        buttonsInitialized = true;
    }

    private void AddButtonsToPlayerEntities()
    {
        for (int i = 0; i < spritePositioning.playerEntities.Count; i++)
        {
            if (spritePositioning.playerEntities[i] == null)
            {
                Debug.LogError($"Player placeholder at index {i} is null!");
                continue;
            }

            CreatePlayerButton(i);
        }
    }

    private void CreatePlayerButton(int index)
    {
        GameObject playerEntity = spritePositioning.playerEntities[index];

        // Set RaycastTarget to false for the placeholder outline
        Image placeholderImage = playerEntity.GetComponent<Image>();
        if (placeholderImage != null)
        {
            placeholderImage.raycastTarget = false;
        }

        // Create a new GameObject for the Button
        GameObject buttonObject = new GameObject($"Button_Outline_{index}");
        buttonObject.transform.SetParent(playerEntity.transform, false); // Add as a child of the Placeholder
        buttonObject.transform.localPosition = Vector3.zero; // Center the Button inside the Placeholder

        // Add required components to make it a Button
        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = buttonSize; // Set the size of the Button to match the placeholder size

        Button buttonComponent = buttonObject.AddComponent<Button>();

        // Optional: Add an Image component to visualize the Button
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new UnityEngine.Color(1, 1, 1, 0); // Transparent background for the Button

        // Add onClick functionality
        buttonComponent.onClick.AddListener(() => OnPlayerButtonClick(index));
    }

    private void OnPlayerButtonClick(int index)
    {
        Debug.Log($"Button inside Player Placeholder {index} clicked!");
        EntityManager entityManager = spritePositioning.playerEntities[index].GetComponent<EntityManager>();

        if (cardManager.currentSelectedCard != null && combatManager.playerTurn)
        {
            HandlePlayerCardSelection(index, entityManager);
        }
        else if (cardManager.currentSelectedCard == null)
        {
            if (entityManager != null && entityManager.placed)
            {
                cardManager.currentSelectedCard = spritePositioning.playerEntities[index];
            }
        }
        else
        {
            Debug.Log("No card selected or not the players turn!");
            cardOutlineManager.RemoveHighlight();
        }
    }

    private void HandlePlayerCardSelection(int index, EntityManager entityManager)
    {
        CardUI cardUI = cardManager.currentSelectedCard.GetComponent<CardUI>();
        if (cardUI == null && !entityManager.placed)
        {
            Debug.LogError("CardUI component not found on current selected card!");
            return;
        }

        Card cardComponent = cardUI?.card;
        if (cardComponent == null && !entityManager.placed)
        {
            Debug.LogError("Card component not found on current selected card!");
            return;
        }

        CardData cardData = cardComponent?.CardType;
        if (cardData == null && !entityManager.placed)
        {
            Debug.LogError("CardType is null on current selected card!");
            return;
        }

        if (cardData != null && cardData.IsMonsterCard)
        {
            HandleMonsterCardSelection(index);
        }
        else if (cardData != null && cardData.IsSpellCard)
        {
            HandleSpellCardSelection(index, entityManager);
        }
        else if (!entityManager.placed)
        {
            Debug.LogError("Card type not found!");
        }
    }

    private void HandleMonsterCardSelection(int index)
    {
        if (!combatManager.isPlayerPrepPhase)
        {
            Debug.LogError("Cannot spawn monster card outside of the preparation phase.");
            cardOutlineManager.RemoveHighlight();
            cardManager.currentSelectedCard = null;
            return;
        }

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

        if (currentMana < cardData.ManaCost)
        {
            Debug.LogError($"Not enough mana. Card costs {cardData.ManaCost}, player has {currentMana}");
            cardOutlineManager.RemoveHighlight();
            return; // Bail if there isn't enough mana
        }

        if (cardData.IsMonsterCard)
        {
            spawnPlayerCard(cardManager.currentSelectedCard.name, index);

            // Remove card from hand
            List<GameObject> handCardObjects = cardManager.getHandCardObjects();
            foreach (GameObject cardObject in handCardObjects)
            {
                if (cardObject == cardManager.currentSelectedCard)
                {
                    handCardObjects.Remove(cardObject);
                    Destroy(cardObject);
                    Debug.Log("Removed card from hand.");
                    break;
                }
            }

            // Also remove the card from the player's deck hand
            cardManager.playerDeck.Hand.Remove(cardComponent);

            cardManager.currentSelectedCard = null;

            // Deactivate placeholders
            placeHolderActiveState(false);
        }
    }


    private void HandleSpellCardSelection(int index, EntityManager entityManager)
    {
        if (combatManager.isCleanUpPhase)
        {
            Debug.LogError("Cannot play spell cards during the Clean Up phase.");
            cardOutlineManager.RemoveHighlight();
            cardManager.currentSelectedCard = null;
            return;
        }

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

        if (currentMana < cardData.ManaCost)
        {
            Debug.LogError($"Not enough mana. Card costs {cardData.ManaCost}, player has {currentMana}");
            cardOutlineManager.RemoveHighlight();
            return; // Bail if there isn't enough mana
        }

        if (entityManager != null && entityManager.placed)
        {
            // Apply spell effect to the placed monster
            Debug.Log($"Applying spell {cardManager.currentSelectedCard.name} to monster {index}");
            SpellCard spellCard = cardManager.currentSelectedCard.GetComponent<SpellCard>();
            if (spellCard == null)
            {
                Debug.LogWarning("SpellCard component not found on current selected card! Adding SpellCard component.");
                spellCard = cardManager.currentSelectedCard.AddComponent<SpellCard>();

                spellCard.CardType = cardData;
                spellCard.CardName = cardData.CardName;
                spellCard.CardImage = cardData.CardImage;
                spellCard.Description = cardData.Description;
                spellCard.ManaCost = cardData.ManaCost;
                spellCard.EffectTypes = cardData.EffectTypes;
                spellCard.EffectValue = cardData.EffectValue;
                spellCard.Duration = cardData.Duration;
            }
            spellCard.targetEntity = entityManager;
            spellCard.Play();

            // Remove card from hand
            List<GameObject> handCardObjects = cardManager.getHandCardObjects();
            foreach (GameObject cardObject in handCardObjects)
            {
                if (cardObject == cardManager.currentSelectedCard)
                {
                    handCardObjects.Remove(cardObject);
                    Destroy(cardObject);
                    Debug.Log("Removed card from hand.");
                    break;
                }
            }

            // Also remove the card from the player's deck hand
            cardManager.playerDeck.Hand.Remove(cardComponent);

            cardManager.currentSelectedCard = null;
            cardOutlineManager.RemoveHighlight();
        }
        else
        {
            Debug.Log("Spells cannot be placed on the field.");
            cardManager.currentSelectedCard = null;
            cardOutlineManager.RemoveHighlight();
        }
    }


    private void AddButtonsToEnemyEntities()
    {
        for (int i = 0; i < spritePositioning.enemyEntities.Count; i++)
        {
            if (spritePositioning.enemyEntities[i] == null)
            {
                Debug.LogError($"Enemy placeholder at index {i} is null!");
                continue;
            }

            CreateEnemyButton(i);
        }
    }

    private void CreateEnemyButton(int index)
    {
        GameObject enemyEntity = spritePositioning.enemyEntities[index];

        // Store the placeholder's world position before parenting
        Vector3 originalWorldPos = enemyEntity.transform.position;
        Vector3 originalScale = enemyEntity.transform.localScale;
        Quaternion originalRotation = enemyEntity.transform.rotation;

        // Create a new GameObject for the Button
        GameObject buttonObject = new GameObject($"Enemy_Button_Outline_{index}");

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
        enemyEntity.transform.SetParent(buttonObject.transform, false);

        // Restore the correct local position, scale, and rotation
        enemyEntity.transform.localPosition = localPos;
        enemyEntity.transform.localScale = originalScale;
        enemyEntity.transform.rotation = originalRotation;

        // Add onClick functionality
        buttonComponent.onClick.AddListener(() => OnEnemyButtonClick(index));

        Debug.Log($"Button {index} created, parented correctly, and position fixed.");
    }

    private void OnEnemyButtonClick(int index)
    {
        Debug.Log($"Button inside Enemy Placeholder {index} clicked!");
        EntityManager entityManager = spritePositioning.enemyEntities[index].GetComponent<EntityManager>();

        if (cardManager.currentSelectedCard != null && combatManager.playerTurn)
        {
            HandleEnemyCardSelection(index, entityManager);
        }
        else
        {
            Debug.Log("No card selected or not the players turn!");
            cardOutlineManager.RemoveHighlight();
        }

        // Check if a player monster is selected and handle the attack
        if (cardManager.currentSelectedCard != null)
        {
            EntityManager playerEntityManager = cardManager.currentSelectedCard.GetComponent<EntityManager>();
            if (playerEntityManager != null && playerEntityManager.placed)
            {
                if (combatManager.isPlayerCombatPhase)
                {
                    HandleMonsterAttack(playerEntityManager, entityManager);
                }
                else
                {
                    Debug.Log("Attacks are not allowed at this stage!");
                }
            }
            else
            {
                Debug.Log("Player monster not selected or not placed.");
            }
        }
    }


    private void HandleEnemyCardSelection(int index, EntityManager entityManager)
    {
        CardUI cardUI = cardManager.currentSelectedCard.GetComponent<CardUI>();
        if (cardUI == null && !entityManager.placed)
        {
            Debug.LogError("CardUI component not found on current selected card!");
            return;
        }

        Card cardComponent = cardUI?.card;
        if (cardComponent == null && !entityManager.placed)
        {
            Debug.LogError("Card component not found on current selected card!");
            return;
        }

        CardData selectedCardData = cardComponent?.CardType;
        if (selectedCardData == null && !entityManager.placed)
        {
            Debug.LogError("CardType is null on current selected card!");
            return;
        }

        if (selectedCardData != null && selectedCardData.IsSpellCard)
        {
            if (entityManager != null && entityManager.placed)
            {
                // Apply spell effect to the enemy monster
                Debug.Log($"Applying spell {cardManager.currentSelectedCard.name} to enemy monster {index}");
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
                    spellCard.EffectTypes = selectedCardData.EffectTypes;
                    spellCard.EffectValue = selectedCardData.EffectValue;
                    spellCard.Duration = selectedCardData.Duration;
                }
                spellCard.targetEntity = entityManager;
                spellCard.Play();

                // Remove card from hand
                List<GameObject> handCardObjects = cardManager.getHandCardObjects();
                foreach (GameObject cardObject in handCardObjects)
                {
                    if (cardObject == cardManager.currentSelectedCard)
                    {
                        handCardObjects.Remove(cardObject);
                        Destroy(cardObject);
                        Debug.Log("Removed card from hand.");
                        break;
                    }
                }

                cardManager.currentSelectedCard = null;
                cardOutlineManager.RemoveHighlight();
            }
            else
            {
                Debug.Log("Spells cannot be placed on the field.");
                cardManager.currentSelectedCard = null;
                cardOutlineManager.RemoveHighlight();
            }
        }
        else if (!entityManager.placed)
        {
            Debug.LogError("Card type not found!");
        }
    }


    public void spawnPlayerCard(string cardName, int whichOutline)
    {
        // Check if the player is in the preparation phase
        if (!combatManager.isPlayerPrepPhase)
        {
            Debug.LogError("Cannot spawn monster card outside of the preparation phase.");
            return;
        }

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
        updateManaUI();

        // Play summon SFX
        AudioSource churchBells = GetComponent<AudioSource>();
        if (churchBells.isPlaying)
        {
            churchBells.Stop();
        }
        churchBells.Play();
    }

    private void HandleMonsterAttack(EntityManager playerEntity, EntityManager enemyEntity)
    {
        if (playerEntity == null || enemyEntity == null)
        {
            Debug.LogError("One of the entities is null!");
            return;
        }

        // Both entities take damage according to their attack values
        float playerAttackDamage = playerEntity.getAttackDamage();
        float enemyAttackDamage = enemyEntity.getAttackDamage();

        playerEntity.takeDamage(enemyAttackDamage);
        enemyEntity.takeDamage(playerAttackDamage);

        Debug.Log($"Player monster attacked enemy monster. Player monster took {enemyAttackDamage} damage. Enemy monster took {playerAttackDamage} damage.");
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


    public void updateManaUI()
    {
        manaBar.GetComponent<Slider>().value = currentMana;
        manaText.GetComponent<TMP_Text>().text = currentMana.ToString();
    }

    private void Update()
    {
        // Check if a card is selected and update placeholder visibility
        if (cardManager.currentSelectedCard != null)
        {
            EntityManager selectedCardEntityManager = cardManager.currentSelectedCard.GetComponent<EntityManager>();
            if (selectedCardEntityManager != null && selectedCardEntityManager.placed)
            {
                placeHolderActiveState(false);
                availableEnemyTargets(true);

                ReactivateSelectedCardPlaceholder();
            }
            else
            {
                placeHolderActiveState(true);
                availableEnemyTargets(false);
            }
        }
        else
        {
            placeHolderActiveState(false);
            availableEnemyTargets(false);
        }
    }

    private void availableEnemyTargets(bool active)
    {
        for (int i = 0; i < spritePositioning.enemyEntities.Count; i++)
        {
            if (spritePositioning.enemyEntities[i] != null)
            {
                Image placeholderImage = spritePositioning.enemyEntities[i].GetComponent<Image>();
                if (placeholderImage != null && placeholderImage.sprite != null)
                {
                    if (placeholderImage.sprite.name != "wizard_outline")
                    {
                        //apply effect here
                    }
                }
            }
        }
    }

    private void ReactivateSelectedCardPlaceholder()
    {
        for (int i = 0; i < spritePositioning.playerEntities.Count; i++)
        {
            if (spritePositioning.playerEntities[i] == cardManager.currentSelectedCard)
            {
                //apply selection effect here
                break;
            }
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
