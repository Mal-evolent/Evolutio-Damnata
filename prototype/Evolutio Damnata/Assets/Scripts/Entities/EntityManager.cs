using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Resources;
using TMPro;
using System.Collections.Generic;

public class EntityManager : MonoBehaviour, IDamageable, IAttacker
{
    // Public Variables
    public Sprite outlineSprite;
    public bool dead = false;
    public bool placed = false;
    public bool IsFadingOut { get; private set; } = false;
    // HasAttacked property is a secondary tracking mechanism - prefer using AttackLimiter
    // through the GetRemainingAttacks(), UseAttack(), and ResetAttacks() methods for attack management
    public bool HasAttacked { get; set; } = false;

    // Serialized Fields
    [Header("Resource Management")]
    [SerializeField] private ResourceManager resourceManager;

    [Header("Monster Attributes")]
    [SerializeField] protected float health;
    [SerializeField] protected float maxHealth;
    [SerializeField] private float atkDamage;
    [SerializeField] private float atkDamageMulti = 1.0f;
    [SerializeField, ReadOnly] private string _keywordsDisplay;

    [Header("Card Data")]
    [SerializeField] private CardData _cardData;


    [Header("UI Elements")]
    [SerializeField] private Image spriteImage;
    [SerializeField] private Slider healthBar;
    [SerializeField] private DamageVisualizer damageVisualizer;
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private Image attackStatImage;
    [SerializeField] private TMP_Text attackStatText;
    [SerializeField] private Image defenceStatImage;
    [SerializeField] private TMP_Text defenceStatText;

    [Header("Keyword Icons")]
    [SerializeField] private Image tauntIcon;
    [SerializeField] private Image rangedIcon;
    [SerializeField] private Image overwhelmIcon;
    [SerializeField] private Image toughIcon;
    [SerializeField] private RectTransform keywordIconsContainer;
    [SerializeField] private float keywordIconSpacing = 1f;

    [Header("Attack Settings")]
    [SerializeField] private int allowedAttacks = 1;
    private int remainingAttacks;

    // Private Variables
    private MonsterType monsterType;
    private OngoingEffectApplier ongoingEffectApplier;
    private AttackLimiter attackLimiter;
    private float turnDuration = 1.0f;
    private EntityManager killedBy;
    private float lastDamageTaken;
    private Dictionary<Keywords.MonsterKeyword, Image> keywordIconMap;
    private List<Image> activeKeywordIcons = new List<Image>();

    public enum MonsterType { Friendly, Enemy }

    #region Initialization
    private void Awake()
    {
        InitializeKeywordSystem();
    }

    private void InitializeKeywordSystem()
    {
        keywordIconMap = new Dictionary<Keywords.MonsterKeyword, Image>()
        {
            { Keywords.MonsterKeyword.Taunt, tauntIcon },
            { Keywords.MonsterKeyword.Ranged, rangedIcon },
            { Keywords.MonsterKeyword.Overwhelm, overwhelmIcon },
            { Keywords.MonsterKeyword.Tough, toughIcon }
        };

        // Configure container if it exists
        if (keywordIconsContainer != null)
        {
            var layoutGroup = keywordIconsContainer.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = keywordIconsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layoutGroup.spacing = keywordIconSpacing;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        }
    }

    #region Attack Management
    public int GetRemainingAttacks() => remainingAttacks;

    public void UseAttack()
    {
        if (dead || IsFadingOut)
        {
            Debug.Log($"[{name}] Cannot use attack: entity is dead or fading out");
            return;
        }

        if (remainingAttacks > 0)
        {
            remainingAttacks--;
            HasAttacked = true;
            Debug.Log($"[{name}] Used attack. Remaining attacks: {remainingAttacks}/{allowedAttacks}");
        }
    }

    public void ResetAttacks()
    {
        remainingAttacks = allowedAttacks;
        HasAttacked = false;
        Debug.Log($"[{name}] Reset attacks. Remaining attacks: {remainingAttacks}/{allowedAttacks}");
    }

    public void SetAllowedAttacks(int newAllowedAttacks)
    {
        allowedAttacks = newAllowedAttacks;
        remainingAttacks = allowedAttacks;
        Debug.Log($"[{name}] Set allowed attacks to {allowedAttacks}. Remaining attacks: {remainingAttacks}/{allowedAttacks}");
    }
    #endregion

    public void InitializeMonster(MonsterType monsterType, float maxHealth, float atkDamage,
        Slider healthBarSlider, Image image, DamageVisualizer damageVisualizer,
        GameObject damageNumberPrefab, Sprite outlineSprite, AttackLimiter attackLimiter,
        OngoingEffectApplier effectApplier, float? currentHealth = null)
    {
        this.monsterType = monsterType;
        this.maxHealth = maxHealth;
        this.health = currentHealth ?? maxHealth;  // Use provided current health or default to max
        this.atkDamage = atkDamage;
        this.spriteImage = image;
        this.damageVisualizer = damageVisualizer;
        this.damageNumberPrefab = damageNumberPrefab;
        this.outlineSprite = outlineSprite;
        this.attackLimiter = attackLimiter;
        this.ongoingEffectApplier = effectApplier;

        remainingAttacks = allowedAttacks;
        Debug.Log($"[{name}] Initialized with {allowedAttacks} allowed attacks");

        InitializeHealthBar(healthBarSlider);
        gameObject.SetActive(false);
    }

    private void InitializeHealthBar(Slider healthBarSlider)
    {
        healthBar = healthBarSlider;
        if (healthBar != null)
        {
            healthBar.maxValue = 1;
            healthBar.value = health / maxHealth;
            healthBar.gameObject.SetActive(true);
        }
        else Debug.LogError("Health bar Slider component not found!");
    }
    #endregion

    #region Keyword Display
    private void UpdateKeywordDisplay()
    {
        if (_cardData?.Keywords == null || keywordIconsContainer == null) return;

        // Clear existing display
        ClearKeywordDisplay();

        // Create new display
        foreach (var keyword in _cardData.Keywords)
        {
            if (keywordIconMap.TryGetValue(keyword, out var iconPrefab) && iconPrefab != null)
            {
                // Instantiate a copy of the icon prefab
                Image newIcon = Instantiate(iconPrefab, keywordIconsContainer);
                newIcon.gameObject.SetActive(true);
                activeKeywordIcons.Add(newIcon);
            }
        }
    }

    private void ClearKeywordDisplay()
    {
        foreach (var icon in activeKeywordIcons)
        {
            if (icon != null) Destroy(icon.gameObject);
        }
        activeKeywordIcons.Clear();
    }
    #endregion

    #region Placement Control
    public void SetPlaced(bool isPlaced)
    {
        placed = isPlaced;
        gameObject.SetActive(isPlaced);

        if (isPlaced)
        {
            dead = false;
            health = maxHealth;
            UpdateHealthUI();
            GetUIStats();
            toggleUIStatStates(true);
            UpdateKeywordDisplay(); // Show relevant keywords
        }
        else
        {
            ClearKeywordDisplay(); // Hide all keywords
        }
    }
    #endregion

    #region Effect Management
    public void ApplyOngoingEffect()
    {
        if (dead) return;
        ongoingEffectApplier?.ApplyEffects(this);
    }

    public void ApplyOngoingEffect(IOngoingEffect effect, int duration)
    {
        if (dead || effect == null) return;
        ongoingEffectApplier?.AddEffect(effect, duration);
    }

    public void RemoveAllOngoingEffects()
    {
        ongoingEffectApplier?.RemoveEffectsForEntity(this);
    }
    #endregion

    #region IDamageable Implementation
    public virtual void TakeDamage(float damageAmount)
    {
        if (dead || !placed) return;

        // Apply Tough damage reduction to ALL incoming damage sources
        if (HasKeyword(Keywords.MonsterKeyword.Tough))
        {
            damageAmount = Mathf.Floor(damageAmount / 2f);
            Debug.Log($"[EntityManager] {name} is tough and reduces all incoming damage by half! Taking {damageAmount} damage.");
        }

        lastDamageTaken = damageAmount;
        health = Mathf.Clamp(health - damageAmount, 0, maxHealth);
        UpdateHealthUI();
        UpdateUIStats();
        ShowDamageNumber(damageAmount);

        if (health <= 0) Die();
    }

    public void Heal(float healAmount)
    {
        if (dead || !placed) return;

        health = Mathf.Min(health + healAmount, maxHealth);
        UpdateHealthUI();
        UpdateUIStats();
        ShowHealingNumber(healAmount);
    }

    public float GetHealth() => health;

    public float GetMaxHealth() => maxHealth;

    public float GetAttack() => atkDamage * atkDamageMulti;

    public void ModifyAttack(float modifier)
    {
        if (dead || !placed) return;
        atkDamage += modifier;
        Debug.Log($"{name} attack modified by {modifier}. New damage: {atkDamage}");
    }
    #endregion

    #region Damage Visualization
    public void ShowDamageNumber(float damageAmount)
    {
        if (damageVisualizer != null &&
            damageNumberPrefab != null &&
            gameObject.activeInHierarchy &&
            !dead)
        {
            damageVisualizer.CreateDamageNumber(
                this,
                damageAmount,
                transform.position,
                damageNumberPrefab
            );
        }
    }

    private void ShowHealingNumber(float healAmount)
    {
        if (damageVisualizer != null && damageNumberPrefab != null)
        {
            damageVisualizer.CreateHealingNumber(
                this,
                healAmount,
                transform.position,
                damageNumberPrefab
            );
        }
    }
    #endregion

    #region IAttacker Implementation
    public virtual void AttackBuff(float buffAmount) => ModifyAttack(buffAmount);
    public virtual void AttackDebuff(float debuffAmount) => ModifyAttack(-debuffAmount);

    public virtual void Attack(int damage)
    {
        if (dead || !placed || IsFadingOut)
        {
            Debug.Log($"[{name}] Cannot attack: entity is dead, not placed, or fading out");
            return;
        }
        Debug.Log($"Attacking with {damage} damage.");
    }

    public virtual float GetAttackDamage() => atkDamage * atkDamageMulti;

    public virtual void SetDoubleAttack(int duration = 1)
    {
        if (dead || !placed) return;
        StartCoroutine(DoubleAttackRoutine(duration));
    }
    #endregion

    #region Combat Utilities
    private IEnumerator DoubleAttackRoutine(int duration)
    {
        int originalAttacks = allowedAttacks;
        allowedAttacks *= 2;
        attackLimiter.ModifyAllowedAttacks(this, allowedAttacks);

        yield return new WaitForSeconds(turnDuration * duration);

        allowedAttacks = originalAttacks;
        attackLimiter.ModifyAllowedAttacks(this, allowedAttacks);
    }

    private void UpdateHealthUI()
    {
        if (healthBar != null) healthBar.value = health / maxHealth;
    }
    #endregion

    #region Lifecycle Management
    protected virtual void Die()
    {
        dead = true;
        placed = false;
        IsFadingOut = true;

        ClearKeywordDisplay(); // Clear keywords on death

        // Only record death in graveyard if it's from combat (killedBy is set)
        // Spell deaths are handled by SpellEffectApplier
        if (GraveYard.Instance != null && killedBy != null)
        {
            GraveYard.Instance.AddToGraveyard(this, killedBy, lastDamageTaken);
        }

        // First remove effects via our local effect applier
        RemoveAllOngoingEffects();

        // As a fallback, also ask StackManager to remove effects directly
        // This handles cases where the effect applier is not working correctly
        if (StackManager.Instance != null)
        {
            StackManager.Instance.RemoveEffectsForEntity(this);
            Debug.Log($"[{name}] Directly removed effects from StackManager on death");
        }

        DisableAllButtons();
        StartCoroutine(PlayDeathAnimation());
    }

    private IEnumerator PlayDeathAnimation()
    {
        FadeOutEffect fadeOutEffect = gameObject.AddComponent<FadeOutEffect>();
        yield return StartCoroutine(fadeOutEffect.FadeOutAndDeactivate(gameObject, 6.5f, outlineSprite, () =>
        {
            IsFadingOut = false;
            EnableAllButtons();
        }));
    }

    private void DisableAllButtons() => SetButtonsInteractable(false);
    private void EnableAllButtons() => SetButtonsInteractable(true);

    private void SetButtonsInteractable(bool interactable)
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
            button.interactable = interactable;
        foreach (Button button in GetComponentsInParent<Button>(true))
            button.interactable = interactable;
    }
    #endregion

    #region Utility Methods
    public MonsterType GetMonsterType() => monsterType;

    public void SetKilledBy(EntityManager killer)
    {
        killedBy = killer;
    }

    public void SetCardData(CardData cardData)
    {
        _cardData = cardData;
        UpdateKeywordsDisplay();
    }

    private void UpdateKeywordsDisplay()
    {
        if (_cardData != null && _cardData.Keywords != null)
        {
            _keywordsDisplay = string.Join(", ", _cardData.Keywords);
        }
        else
        {
            _keywordsDisplay = "None";
        }
    }

    public CardData GetCardData()
    {
        return _cardData;
    }

    public bool HasKeyword(Keywords.MonsterKeyword keyword)
    {
        return _cardData != null && _cardData.Keywords != null && _cardData.Keywords.Contains(keyword);
    }

    private void GetUIStats()
    {
        // Early return if this is a HealthIconManager (which doesn't have UI stat components)
        if (this is HealthIconManager)
            return;

        if (attackStatImage != null)
        {
            attackStatText = attackStatImage.GetComponentInChildren<TMP_Text>();

            // Flip text for enemy monsters by inverting X scale only
            if (monsterType == MonsterType.Enemy && attackStatText != null)
            {
                // Preserve the original Y and Z scale values
                Vector3 currentScale = attackStatText.transform.localScale;
                attackStatText.transform.localScale = new Vector3(-1 * Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
            }
        }
        else
        {
            Debug.LogError("Attack stat image not found!");
        }

        if (defenceStatImage != null)
        {
            defenceStatText = defenceStatImage.GetComponentInChildren<TMP_Text>();

            // Flip text for enemy monsters by inverting X scale only
            if (monsterType == MonsterType.Enemy && defenceStatText != null)
            {
                // Preserve the original Y and Z scale values
                Vector3 currentScale = defenceStatText.transform.localScale;
                defenceStatText.transform.localScale = new Vector3(-1 * Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
            }
        }
        else
        {
            Debug.LogError("Defence stat image not found!");
        }

        UpdateUIStats();
    }

    private void UpdateUIStats()
    {
        // Early return if this is a HealthIconManager (which doesn't have UI stat components)
        if (this is HealthIconManager)
            return;

        if (attackStatText != null)
        {
            attackStatText.text = atkDamage.ToString();
        }

        if (defenceStatText != null)
        {
            defenceStatText.text = health.ToString();
        }
        else
        {
            Debug.LogError("Defence stat text not found!");
        }
    }

    public void toggleUIStatStates(bool state)
    {
        // Early return if this is a HealthIconManager (which doesn't have UI stat components)
        if (this is HealthIconManager)
            return;

        if (attackStatImage != null)
        {
            attackStatImage.gameObject.SetActive(state);
        }
        else
        {
            Debug.LogError("Attack stat image not found!");
        }

        if (defenceStatImage != null)
        {
            defenceStatImage.gameObject.SetActive(state);
        }
        else
        {
            Debug.LogError("Defence stat image not found!");
        }
    }
    #endregion
}
