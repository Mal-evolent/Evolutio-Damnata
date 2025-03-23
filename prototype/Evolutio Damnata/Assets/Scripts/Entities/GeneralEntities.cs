using UnityEngine;
using UnityEngine.UI;
using System.Linq;


public class GeneralEntities
{
    private SpritePositioning spritePositioning;
    private CardLibrary cardLibrary;
    private DamageVisualizer damageVisualizer;
    private GameObject damageNumberPrefab;
    private Sprite wizardOutlineSprite;
    private CombatStage combatStage;
    private AttackLimiter attackLimiter;
    private EntityManager._monsterType monsterType;
    private ManaChecker manaChecker;
    public bool enemyCardPlayed = false;

    public GeneralEntities(SpritePositioning spritePositioning, CardLibrary cardLibrary, DamageVisualizer damageVisualizer, GameObject damageNumberPrefab, Sprite wizardOutlineSprite, CombatStage combatStage, AttackLimiter attackLimiter, EntityManager._monsterType monsterType)
    {
        this.spritePositioning = spritePositioning;
        this.cardLibrary = cardLibrary;
        this.damageVisualizer = damageVisualizer;
        this.damageNumberPrefab = damageNumberPrefab;
        this.wizardOutlineSprite = wizardOutlineSprite;
        this.combatStage = combatStage;
        this.attackLimiter = attackLimiter;
        this.monsterType = monsterType;
        this.manaChecker = new ManaChecker(combatStage, combatStage.cardOutlineManager, combatStage.cardManager);
    }

    public void SpawnCards(string cardName, int whichOutline)
    {
        if (monsterType == EntityManager._monsterType.Friendly)
        {
            SpawnPlayerCard(cardName, whichOutline);
        }
        else if (monsterType == EntityManager._monsterType.Enemy)
        {
            SpawnEnemyCard(cardName, whichOutline);
        }
    }

    private void SpawnPlayerCard(string cardName, int whichOutline)
    {
        if (whichOutline < 0 || whichOutline >= spritePositioning.playerEntities.Count)
        {
            Debug.LogError($"Invalid outline index: {whichOutline}");
            return;
        }

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

        CardData selectedCardData = cardLibrary.cardDataList.FirstOrDefault(cardData => cardName == cardData.CardName);
        if (selectedCardData == null)
        {
            Debug.LogError($"Card data not found for card name: {cardName}");
            return;
        }

        if (!selectedCardData.IsSpellCard)
        {
            placeholderImage.sprite = cardLibrary.cardImageGetter(cardName);
        }

        EntityManager entityManager = placeholder.GetComponent<EntityManager>();
        if (entityManager == null)
        {
            entityManager = placeholder.AddComponent<EntityManager>();
        }

        Transform healthBarTransform = placeholder.transform.Find("healthBar");
        Slider healthBarSlider = healthBarTransform != null ? healthBarTransform.GetComponent<Slider>() : null;

        if (!selectedCardData.IsSpellCard || (existingEntityManager == null || !existingEntityManager.placed))
        {
            entityManager.InitializeMonster(EntityManager._monsterType.Friendly, selectedCardData.Health, selectedCardData.AttackPower, healthBarSlider, placeholderImage, damageVisualizer, damageNumberPrefab, wizardOutlineSprite, attackLimiter);
        }

        bool isOccupied = existingEntityManager != null && existingEntityManager.placed;

        if (!isOccupied)
        {
            entityManager.placed = !selectedCardData.IsSpellCard;
            if (entityManager.placed)
            {
                entityManager.dead = false;
            }
        }

        DisplayHealthBar(placeholder, !selectedCardData.IsSpellCard || !isOccupied);

        if (!selectedCardData.IsSpellCard)
        {
            placeholder.name = cardName;
        }

        PlaySummonSFX();
    }

    private void SpawnEnemyCard(string cardName, int whichOutline)
    {
        enemyCardPlayed = false;
        if (whichOutline < 0 || whichOutline >= spritePositioning.enemyEntities.Count)
        {
            Debug.LogError($"Invalid outline index: {whichOutline}");
            return;
        }

        GameObject placeholder = spritePositioning.enemyEntities[whichOutline];
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

        CardData selectedCardData = cardLibrary.cardDataList.FirstOrDefault(cardData => cardName == cardData.CardName);
        if (selectedCardData == null)
        {
            Debug.LogError($"Card data not found for card name: {cardName}");
            return;
        }

        if (!manaChecker.HasEnoughEnemyMana(selectedCardData))
        {
            return;
        }

        if (!selectedCardData.IsSpellCard)
        {
            placeholderImage.sprite = cardLibrary.cardImageGetter(cardName);
        }

        EntityManager entityManager = placeholder.GetComponent<EntityManager>();
        if (entityManager == null)
        {
            entityManager = placeholder.AddComponent<EntityManager>();
        }

        Transform healthBarTransform = placeholder.transform.Find("healthBar");
        Slider healthBarSlider = healthBarTransform != null ? healthBarTransform.GetComponent<Slider>() : null;

        entityManager.InitializeMonster(EntityManager._monsterType.Enemy, selectedCardData.Health, selectedCardData.AttackPower, healthBarSlider, placeholderImage, damageVisualizer, damageNumberPrefab, wizardOutlineSprite, attackLimiter);

        entityManager.placed = !selectedCardData.IsSpellCard;
        if (entityManager.placed)
        {
            entityManager.dead = false;
        }

        DisplayHealthBar(placeholder, !selectedCardData.IsSpellCard);

        if (!selectedCardData.IsSpellCard)
        {
            placeholder.name = cardName;
        }

        entityManager.gameObject.SetActive(true);

        manaChecker.DeductEnemyMana(selectedCardData);
        enemyCardPlayed = true;
    }

    private void DisplayHealthBar(GameObject entity, bool active)
    {
        Transform healthBarTransform = entity.transform.Find("healthBar");
        if (healthBarTransform != null)
        {
            healthBarTransform.gameObject.SetActive(active);
        }
    }

    private void PlaySummonSFX()
    {
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
}
