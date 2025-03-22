using UnityEngine;
using UnityEngine.UI;
using System.Linq;


public class PlayerEntities
{
    private SpritePositioning spritePositioning;
    private CardLibrary cardLibrary;
    private DamageVisualizer damageVisualizer;
    private GameObject damageNumberPrefab;
    private Sprite wizardOutlineSprite;
    private CombatStage combatStage;
    private AttackLimiter attackLimiter;

    public PlayerEntities(SpritePositioning spritePositioning, CardLibrary cardLibrary, DamageVisualizer damageVisualizer, GameObject damageNumberPrefab, Sprite wizardOutlineSprite, CombatStage combatStage, AttackLimiter attackLimiter)
    {
        this.spritePositioning = spritePositioning;
        this.cardLibrary = cardLibrary;
        this.damageVisualizer = damageVisualizer;
        this.damageNumberPrefab = damageNumberPrefab;
        this.wizardOutlineSprite = wizardOutlineSprite;
        this.combatStage = combatStage;
        this.attackLimiter = attackLimiter;
    }

    public void SpawnPlayerCard(string cardName, int whichOutline)
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

        Image placeholderImage = placeholder.GetComponent<Image>();
        if (placeholderImage == null)
        {
            Debug.LogError("Image component not found on placeholder!");
            return;
        }
        EntityManager existingEntityManager = placeholder.GetComponent<EntityManager>();

        // Find the selected card data
        CardData selectedCardData = cardLibrary.cardDataList.FirstOrDefault(cardData => cardName == cardData.CardName);
        if (selectedCardData == null)
        {
            Debug.LogError($"Card data not found for card name: {cardName}");
            return;
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
        DisplayHealthBar(placeholder, !selectedCardData.IsSpellCard || !isOccupied);

        // Rename the placeholder to the card name unless it's a spell card
        if (!selectedCardData.IsSpellCard)
        {
            placeholder.name = cardName;
        }

        // Decrease current mana
        combatStage.currentMana -= selectedCardData.ManaCost;
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
