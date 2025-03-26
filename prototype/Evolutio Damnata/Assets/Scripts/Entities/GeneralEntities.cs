using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GeneralEntities : ICardSpawner
{
    // Dependencies
    private readonly SpritePositioning _spritePositioning;
    private readonly CardLibrary _cardLibrary;
    private readonly IManaProvider _manaProvider;
    private readonly DamageVisualizer _damageVisualizer;
    private readonly AttackLimiter _attackLimiter;
    private readonly CombatStage _combatStage;

    // Visual/Audio
    private readonly GameObject _damageNumberPrefab;
    private readonly Sprite _wizardOutlineSprite;

    // State
    public bool EnemyCardPlayed { get; private set; }
    private readonly EntityManager._monsterType _monsterType;

    public GeneralEntities(
        SpritePositioning spritePositioning,
        CardLibrary cardLibrary,
        IManaProvider manaProvider,
        DamageVisualizer damageVisualizer,
        GameObject damageNumberPrefab,
        Sprite wizardOutlineSprite,
        CombatStage combatStage,
        AttackLimiter attackLimiter,
        EntityManager._monsterType monsterType)
    {
        _spritePositioning = spritePositioning;
        _cardLibrary = cardLibrary;
        _manaProvider = manaProvider;
        _damageVisualizer = damageVisualizer;
        _damageNumberPrefab = damageNumberPrefab;
        _wizardOutlineSprite = wizardOutlineSprite;
        _combatStage = combatStage;
        _attackLimiter = attackLimiter;
        _monsterType = monsterType;
    }

    public void SpawnCards(string cardName, int whichOutline)
    {
        if (string.IsNullOrEmpty(cardName))
        {
            Debug.LogError("Card name cannot be null or empty");
            return;
        }

        try
        {
            if (_monsterType == EntityManager._monsterType.Friendly)
            {
                SpawnPlayerCard(cardName, whichOutline);
            }
            else if (_monsterType == EntityManager._monsterType.Enemy)
            {
                SpawnEnemyCard(cardName, whichOutline);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to spawn card: {ex.Message}");
        }
    }

    private void SpawnPlayerCard(string cardName, int whichOutline)
    {
        ValidateOutlineIndex(whichOutline, _spritePositioning.playerEntities.Count);
        GameObject placeholder = GetValidPlaceholder(whichOutline, _spritePositioning.playerEntities);

        CardData cardData = GetCardData(cardName);
        EntityManager entityManager = InitializeEntity(placeholder, cardData);

        if (!cardData.IsSpellCard)
        {
            UpdateCardVisuals(placeholder, cardData);
            entityManager.placed = true;
            entityManager.dead = false;
        }

        DisplayHealthBar(placeholder, !cardData.IsSpellCard);
        PlaySummonSFX();
    }

    private void SpawnEnemyCard(string cardName, int whichOutline)
    {
        EnemyCardPlayed = false;
        ValidateOutlineIndex(whichOutline, _spritePositioning.enemyEntities.Count);
        GameObject placeholder = GetValidPlaceholder(whichOutline, _spritePositioning.enemyEntities);

        CardData cardData = GetCardData(cardName);

        if (_manaProvider.EnemyMana < cardData.ManaCost)
        {
            Debug.Log($"Not enough enemy mana: {cardData.ManaCost} needed");
            return;
        }

        EntityManager entityManager = InitializeEntity(placeholder, cardData);

        if (!cardData.IsSpellCard)
        {
            UpdateCardVisuals(placeholder, cardData);
            entityManager.placed = true;
            entityManager.dead = false;
        }

        DisplayHealthBar(placeholder, !cardData.IsSpellCard);
        _manaProvider.EnemyMana -= cardData.ManaCost;
        EnemyCardPlayed = true;
    }

    #region Helper Methods
    private void ValidateOutlineIndex(int index, int maxCount)
    {
        if (index < 0 || index >= maxCount)
            throw new System.ArgumentOutOfRangeException($"Invalid outline index: {index}");
    }

    private GameObject GetValidPlaceholder(int index, System.Collections.Generic.List<GameObject> entities)
    {
        GameObject placeholder = entities[index];
        if (placeholder == null)
            throw new System.NullReferenceException($"Placeholder at index {index} is null");

        if (placeholder.GetComponent<Image>() == null)
            throw new MissingComponentException("Image component not found on placeholder");

        return placeholder;
    }

    private CardData GetCardData(string cardName)
    {
        CardData cardData = _cardLibrary.cardDataList.FirstOrDefault(c => cardName == c.CardName);
        if (cardData == null)
            throw new System.ArgumentException($"Card data not found: {cardName}");

        return cardData;
    }

    private EntityManager InitializeEntity(GameObject placeholder, CardData cardData)
    {
        EntityManager entityManager = placeholder.GetComponent<EntityManager>() ??
                                    placeholder.AddComponent<EntityManager>();

        Slider healthBar = placeholder.transform.Find("healthBar")?.GetComponent<Slider>();
        Image placeholderImage = placeholder.GetComponent<Image>();

        entityManager.InitializeMonster(
            _monsterType,
            cardData.Health,
            cardData.AttackPower,
            healthBar,
            placeholderImage,
            _damageVisualizer,
            _damageNumberPrefab,
            _wizardOutlineSprite,
            _attackLimiter
        );

        return entityManager;
    }

    private void UpdateCardVisuals(GameObject placeholder, CardData cardData)
    {
        placeholder.GetComponent<Image>().sprite = _cardLibrary.cardImageGetter(cardData.CardName);
        placeholder.name = cardData.CardName;
    }
    #endregion

    private void DisplayHealthBar(GameObject entity, bool active)
    {
        Transform healthBar = entity.transform.Find("healthBar");
        healthBar?.gameObject.SetActive(active);
    }

    private void PlaySummonSFX()
    {
        if (_combatStage.TryGetComponent(out AudioSource audioSource))
        {
            if (audioSource.isPlaying) audioSource.Stop();
            audioSource.Play();
        }
        else
        {
            Debug.LogError("AudioSource missing on CombatStage");
        }
    }
}