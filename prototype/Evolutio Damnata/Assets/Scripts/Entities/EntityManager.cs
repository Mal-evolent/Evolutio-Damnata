using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Resources;

public class EntityManager : MonoBehaviour, IDamageable, IAttacker
{
    // Public Variables
    public Sprite outlineSprite;
    public bool dead = false;
    public bool placed = false;
    public bool IsFadingOut { get; private set; } = false;

    // Serialized Fields
    [Header("Resource Management")]
    [SerializeField] private ResourceManager resourceManager;

    [Header("Monster Attributes")]
    [SerializeField] private float health;
    [SerializeField] private float maxHealth;
    [SerializeField] private float atkDamage;
    [SerializeField] private float atkDamageMulti = 1.0f;

    [Header("UI Elements")]
    [SerializeField] private Image spriteImage;
    [SerializeField] private Slider healthBar;
    [SerializeField] private DamageVisualizer damageVisualizer;
    [SerializeField] private GameObject damageNumberPrefab;

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

    public enum MonsterType { Friendly, Enemy }

    #region Attack Management
    public int GetRemainingAttacks() => remainingAttacks;
    
    public void UseAttack()
    {
        if (remainingAttacks > 0)
        {
            remainingAttacks--;
            Debug.Log($"[{name}] Used attack. Remaining attacks: {remainingAttacks}/{allowedAttacks}");
        }
    }

    public void ResetAttacks()
    {
        remainingAttacks = allowedAttacks;
        Debug.Log($"[{name}] Reset attacks. Remaining attacks: {remainingAttacks}/{allowedAttacks}");
    }

    public void SetAllowedAttacks(int newAllowedAttacks)
    {
        allowedAttacks = newAllowedAttacks;
        remainingAttacks = allowedAttacks;
        Debug.Log($"[{name}] Set allowed attacks to {allowedAttacks}. Remaining attacks: {remainingAttacks}/{allowedAttacks}");
    }
    #endregion

    #region Initialization
    public void InitializeMonster(MonsterType monsterType, float maxHealth, float atkDamage,
        Slider healthBarSlider, Image image, DamageVisualizer damageVisualizer,
        GameObject damageNumberPrefab, Sprite outlineSprite, AttackLimiter attackLimiter,
        OngoingEffectApplier effectApplier)
    {
        this.monsterType = monsterType;
        this.maxHealth = maxHealth;
        this.health = maxHealth;
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
    public void TakeDamage(float damageAmount)
    {
        if (dead || !placed) return;

        lastDamageTaken = damageAmount;
        health = Mathf.Clamp(health - damageAmount, 0, maxHealth);
        UpdateHealthUI();

        ShowDamageNumber(damageAmount);

        if (health <= 0) Die();
    }

    public void Heal(float healAmount)
    {
        if (dead || !placed) return;

        health = Mathf.Min(health + healAmount, maxHealth);
        UpdateHealthUI();
        ShowHealingNumber(healAmount);
    }

    public float GetHealth() => health;

    public float GetMaxHealth() => maxHealth;

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
    public void AttackBuff(float buffAmount) => ModifyAttack(buffAmount);
    public void AttackDebuff(float debuffAmount) => ModifyAttack(-debuffAmount);

    public void Attack(int damage)
    {
        if (dead || !placed) return;
        Debug.Log($"Attacking with {damage} damage.");
    }

    public float GetAttackDamage() => atkDamage * atkDamageMulti;

    public void SetDoubleAttack(int duration = 1)
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
    private void Die()
    {
        dead = true;
        placed = false;
        IsFadingOut = true;

        // Only record death in graveyard if it's from combat (killedBy is set)
        // Spell deaths are handled by SpellEffectApplier
        if (GraveYard.Instance != null && killedBy != null)
        {
            GraveYard.Instance.AddToGraveyard(this, killedBy, lastDamageTaken);
        }

        RemoveAllOngoingEffects();
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
    #endregion
}