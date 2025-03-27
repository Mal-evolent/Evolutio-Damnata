using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GeneralEntities : ICardSpawner
{
    // Dependencies
    private readonly ISpritePositioning _spritePositioning;
    private readonly CardLibrary _cardLibrary;
    private readonly IManaProvider _manaProvider;
    private readonly DamageVisualizer _damageVisualizer;
    private readonly AttackLimiter _attackLimiter;
    private readonly ICombatStage _combatStage;

    // Visual/Audio
    private readonly GameObject _damageNumberPrefab;
    private readonly Sprite _wizardOutlineSprite;

    // State
    public bool CardPlayed { get; private set; }
    private readonly EntityManager.MonsterType _monsterType;

    public GeneralEntities(
        ISpritePositioning spritePositioning,
        CardLibrary cardLibrary,
        IManaProvider manaProvider,
        DamageVisualizer damageVisualizer,
        GameObject damageNumberPrefab,
        Sprite wizardOutlineSprite,
        ICombatStage combatStage,
        AttackLimiter attackLimiter,
        EntityManager.MonsterType monsterType)
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

    public bool SpawnCard(string cardName, int positionIndex)
    {
        if (string.IsNullOrEmpty(cardName))
        {
            Debug.LogError("Card name cannot be null or empty");
            return false;
        }

        try
        {
            if (_monsterType == EntityManager.MonsterType.Friendly)
            {
                return SpawnPlayerCard(cardName, positionIndex);
            }
            else if (_monsterType == EntityManager.MonsterType.Enemy)
            {
                return SpawnEnemyCard(cardName, positionIndex);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to spawn card: {ex.Message}");
        }

        return false;
    }

    private bool SpawnPlayerCard(string cardName, int positionIndex)
    {
        var placeholder = GetValidPlaceholder(positionIndex, _spritePositioning.PlayerEntities);
        var cardData = GetCardData(cardName);
        var entityManager = InitializeEntity(placeholder, cardData);

        if (!cardData.IsSpellCard)
        {
            UpdateCardVisuals(placeholder, cardData);
            entityManager.SetPlaced(true);
        }

        DisplayHealthBar(placeholder, !cardData.IsSpellCard);
        PlaySummonSFX();

        return true;
    }

    private bool SpawnEnemyCard(string cardName, int positionIndex)
    {
        var placeholder = GetValidPlaceholder(positionIndex, _spritePositioning.EnemyEntities);
        var cardData = GetCardData(cardName);

        if (_manaProvider.EnemyMana < cardData.ManaCost)
        {
            Debug.Log($"Not enough enemy mana: {cardData.ManaCost} needed");
            return false;
        }

        var entityManager = InitializeEntity(placeholder, cardData);

        if (!cardData.IsSpellCard)
        {
            UpdateCardVisuals(placeholder, cardData);
            entityManager.SetPlaced(true);
        }

        DisplayHealthBar(placeholder, !cardData.IsSpellCard);
        _manaProvider.EnemyMana -= cardData.ManaCost;
        CardPlayed = true;

        return true;
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
        return _cardLibrary.CardDataList.FirstOrDefault(c => cardName == c.CardName)
            ?? throw new System.ArgumentException($"Card data not found: {cardName}");
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
        placeholder.GetComponent<Image>().sprite = _cardLibrary.GetCardImage(cardData.CardName); // Changed from cardImageGetter
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
        // Changed to use GetComponent through the interface
        var combatStageObj = _combatStage as MonoBehaviour;
        if (combatStageObj != null && combatStageObj.TryGetComponent(out AudioSource audioSource))
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