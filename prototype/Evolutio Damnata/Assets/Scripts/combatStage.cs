using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class CombatStage : MonoBehaviour
{
    public Sprite wizardOutlineSprite;

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

    private CardSelectionHandler cardSelectionHandler;
    private ButtonCreator buttonCreator;
    private AttackHandler attackHandler;
    private EnemySpawner enemySpawner;

    private void Awake()
    {
        cardSelectionHandler = gameObject.AddComponent<CardSelectionHandler>();
        cardSelectionHandler.Initialize(cardManager, combatManager, cardOutlineManager, spritePositioning, this);

        buttonCreator = gameObject.AddComponent<ButtonCreator>();
        buttonCreator.Initialize(battleField, spritePositioning, cardSelectionHandler);

        attackHandler = new AttackHandler();
        enemySpawner = new EnemySpawner(spritePositioning, cardLibrary, damageVisualizer, damageNumberPrefab, wizardOutlineSprite);
    }

    // This function will be kept
    public void interactableHighlights()
    {
        if (buttonsInitialized) return;

        buttonCreator.AddButtonsToPlayerEntities();
        buttonCreator.AddButtonsToEnemyEntities();

        buttonsInitialized = true;
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
            entityManager.InitializeMonster(EntityManager._monsterType.Friendly, selectedCardData.Health, selectedCardData.AttackPower, healthBarSlider, placeholderImage, damageVisualizer, damageNumberPrefab, wizardOutlineSprite);
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

    public void HandleMonsterAttack(EntityManager playerEntity, EntityManager enemyEntity)
    {
        attackHandler.HandleMonsterAttack(playerEntity, enemyEntity);
    }

    public void spawnEnemy(string cardName, int whichOutline)
    {
        enemySpawner.SpawnEnemy(cardName, whichOutline);
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

    public void placeHolderActiveState(bool active)
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
