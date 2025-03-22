using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/**
 * The PlayerCardSpawner class is responsible for spawning player cards in the combat stage.
 * It handles the placement of monster cards and the casting of spell cards.
 */

public class PlayerCardSpawner
{
    private SpritePositioning spritePositioning;
    private CardLibrary cardLibrary;
    private CardOutlineManager cardOutlineManager;
    private CardManager cardManager;
    private CombatManager combatManager;
    private DamageVisualizer damageVisualizer;
    private GameObject damageNumberPrefab;
    private Sprite wizardOutlineSprite;
    private GameObject manaBar;
    private GameObject manaText;
    private CombatStage combatStage;
    private AttackLimiter attackLimiter;

    public PlayerCardSpawner(SpritePositioning spritePositioning, CardLibrary cardLibrary, CardOutlineManager cardOutlineManager, CardManager cardManager, CombatManager combatManager, DamageVisualizer damageVisualizer, GameObject damageNumberPrefab, Sprite wizardOutlineSprite, GameObject manaBar, GameObject manaText, CombatStage combatStage, AttackLimiter attackLimiter)
    {
        this.spritePositioning = spritePositioning;
        this.cardLibrary = cardLibrary;
        this.cardOutlineManager = cardOutlineManager;
        this.cardManager = cardManager;
        this.combatManager = combatManager;
        this.damageVisualizer = damageVisualizer;
        this.damageNumberPrefab = damageNumberPrefab;
        this.wizardOutlineSprite = wizardOutlineSprite;
        this.manaBar = manaBar;
        this.manaText = manaText;
        this.combatStage = combatStage;
        this.attackLimiter = attackLimiter;
    }

    public void SpawnPlayerCard(string cardName, int whichOutline)
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

        Image placeholderImage = placeholder.GetComponent<Image>();
        if (placeholderImage == null)
        {
            Debug.LogError("Image component not found on placeholder!");
            return;
        }
        EntityManager existingEntityManager = placeholder.GetComponent<EntityManager>();
        // Find the selected card data
        int cardCost = 0;
        CardData selectedCardData = null;
        foreach (CardData cardData in cardLibrary.cardDataList)
        {
            if (cardName == cardData.CardName)
            {
                if (combatStage.currentMana < cardData.ManaCost)
                {
                    Debug.LogError($"Not enough mana. Card costs {cardData.ManaCost}, player has {combatStage.currentMana}");
                    cardOutlineManager.RemoveHighlight();
                    if (cardManager != null)
                    {
                        cardManager.currentSelectedCard = null;
                        Debug.Log("Set currentSelectedCard to null due to insufficient mana.");
                    }
                    else
                    {
                        Debug.LogError("cardManager is null!");
                    }
                    return;
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
                    spellCard.EffectTypes = selectedCardData.EffectTypes;
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
            Object.Destroy(cardManager.currentSelectedCard);
            cardManager.currentSelectedCard = null;
            Debug.Log("Set currentSelectedCard to null after removing card from hand.");
        }

        // Set monster attributes
        if (!selectedCardData.IsSpellCard)
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
            entityManager.InitializeMonster(EntityManager._monsterType.Friendly, selectedCardData.Health, selectedCardData.AttackPower, healthBarSlider, placeholderImage, damageVisualizer, damageNumberPrefab, wizardOutlineSprite, attackLimiter);
        }

        // Check if the placeholder is already occupied by a placed monster card
        bool isOccupied = existingEntityManager != null && existingEntityManager.placed;

        // Set entity.placed to false only if the placeholder is empty
        if (!isOccupied)
        {
            entityManager.placed = !selectedCardData.IsSpellCard;
            if (entityManager.placed)
            {
                entityManager.dead = false;
            }
        }

        // Display or hide the health bar based on whether the placeholder is occupied by a placed monster card
        if (selectedCardData.IsSpellCard && !isOccupied)
        {
            DisplayHealthBar(placeholder, false);
        }
        else
        {
            DisplayHealthBar(placeholder, true);
        }

        // Rename the placeholder to the card name unless it's a spell card
        if (!selectedCardData.IsSpellCard)
        {
            placeholder.name = cardName;
        }

        // Decrease current mana
        combatStage.currentMana -= cardCost;
        combatStage.updateManaUI();

        // Play summon SFX
        AudioSource churchBells = combatStage.GetComponent<AudioSource>();
        if (churchBells != null)
        {
            if (churchBells.isPlaying)
            {
                churchBells.Stop();
            }
            churchBells.Play();
        }
        else
        {
            Debug.LogError("AudioSource component not found on CombatStage!");
        }
    }

    private void DisplayHealthBar(GameObject entity, bool active)
    {
        Transform healthBarTransform = entity.transform.Find("healthBar");
        if (healthBarTransform != null)
        {
            healthBarTransform.gameObject.SetActive(active);
        }
    }
}
