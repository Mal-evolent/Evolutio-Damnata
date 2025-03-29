using System.Resources;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;


public class EntityManager : MonoBehaviour, IDamageable, IAttacker
{
    // Public Variables
    public Sprite outlineSprite;
    public bool dead = false;
    public bool placed = false;
    public bool IsFadingOut { get; private set; } = false;

    // Serialized Fields
    [Header("Resource Management")]
    [SerializeField]
    ResourceManager resourceManager;

    [Header("Monster Attributes")]
    [SerializeField]
    float health;
    [SerializeField]
    float maxHealth;
    [SerializeField]
    float atkDamage;
    [SerializeField]
    float atkDamageMulti = 1.0f;

    [Header("UI Elements")]
    [SerializeField]
    Image spriteImage;
    [SerializeField]
    Slider healthBar;
    [SerializeField]
    DamageVisualizer damageVisualizer;
    [SerializeField]
    GameObject damageNumberPrefab;

    [Header("Attack Settings")]
    [SerializeField]
    int allowedAttacks = 1;

    // Private Variables
    private MonsterType monsterType;
    private List<OngoingEffectManager> ongoingEffects = new List<OngoingEffectManager>();
    private OngoingEffectApplier ongoingEffectApplier;
    private AttackLimiter attackLimiter;
    private float turnDuration = 1.0f;

    public enum MonsterType
    {
        Friendly,
        Enemy,
    }

    public void InitializeMonster(MonsterType monsterType, float maxHealth, float atkDamage, Slider healthBarSlider, Image image, DamageVisualizer damageVisualizer, GameObject damageNumberPrefab, Sprite outlineSprite, AttackLimiter attackLimiter)
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

        attackLimiter.RegisterEntity(this, allowedAttacks);
        Debug.Log($"Entity {name} initialized with {allowedAttacks} allowed attacks.");

        healthBar = healthBarSlider;
        if (healthBar != null)
        {
            healthBar.maxValue = 1;
            healthBar.value = health / maxHealth;
            healthBar.gameObject.SetActive(true);
            Debug.Log($"Health bar initialized with value: {healthBar.value}");
        }
        else
        {
            Debug.LogError("Health bar Slider component not found!");
        }
    }

    public MonsterType GetMonsterType()
    {
        return monsterType;
    }

    public void SetPlaced(bool isPlaced)
    {
        placed = isPlaced;
        SetGameObjectActive(placed);
    }

    private void SetGameObjectActive(bool isActive)
    {
        gameObject.SetActive(isActive);
    }

    public void takeDamage(float damageAmount)
    {
        if (dead) return;

        health -= damageAmount;
        health = Mathf.Clamp(health, 0, maxHealth);
        if (healthBar != null)
        {
            healthBar.value = health / maxHealth;
        }
        Debug.Log($"Health is now {health}");
        if (health <= 0)
        {
            Die();
        }

        if (damageVisualizer != null && damageNumberPrefab != null)
        {
            if (gameObject.activeInHierarchy)
            {
                Vector3 position = transform.position;
                damageVisualizer.CreateDamageNumber(this, damageAmount, position, damageNumberPrefab);
            }
            else
            {
                Debug.LogWarning("Cannot start coroutine on inactive game object.");
            }
        }
        else
        {
            if (damageVisualizer == null)
            {
                Debug.LogError("DamageVisualizer is not set.");
            }
            if (damageNumberPrefab == null)
            {
                Debug.LogError("damageNumberPrefab is not set.");
            }
        }
    }

    private void Die()
    {
        dead = true;
        placed = false;
        IsFadingOut = true;
        RemoveAllOngoingEffects();
        Debug.Log("Monster is dead.");

        ongoingEffectApplier?.RemoveEffectsForEntity(this);

        // Disable all buttons in the hierarchy, including parent objects
        Button[] buttonsInChildren = GetComponentsInChildren<Button>(true);
        Button[] buttonsInParents = GetComponentsInParent<Button>(true);

        foreach (Button button in buttonsInChildren)
        {
            button.interactable = false;
        }

        foreach (Button button in buttonsInParents)
        {
            button.interactable = false;
        }

        FadeOutEffect fadeOutEffect = gameObject.AddComponent<FadeOutEffect>();
        StartCoroutine(fadeOutEffect.FadeOutAndDeactivate(gameObject, 6.5f, outlineSprite, () =>
        {
            IsFadingOut = false;
            foreach (Button button in buttonsInChildren)
            {
                button.interactable = true;
            }

            foreach (Button button in buttonsInParents)
            {
                button.interactable = true;
            }
        }));
    }

    private void RemoveAllOngoingEffects()
    {
        ongoingEffects.Clear();
    }

    public void healAmount(float healAmount)
    {
        if (dead) return;

        health += healAmount;
        health = Mathf.Clamp(health, 0, maxHealth);
        if (healthBar != null)
        {
            healthBar.value = health / maxHealth;
        }
    }

    public float getHealth()
    {
        return health;
    }

    public void attackBuff(float buffAmount)
    {
        if (dead) return;

        atkDamage += buffAmount;
    }

    public void attackDebuff(float buffAmount)
    {
        if (dead) return;

        atkDamage -= buffAmount;
    }

    public void attack(int damage)
    {
        if (dead) return;

        Debug.Log($"Attacking with {damage} damage.");
    }

    public float getAttackDamage()
    {
        return atkDamage * atkDamageMulti;
    }

    public void AddOngoingEffect(OngoingEffectManager effect)
    {
        if (dead) return;

        ongoingEffects.Add(effect);
    }

    public void ApplyOngoingEffects()
    {
        if (dead) return;

        ongoingEffectApplier.ApplyEffects(this);
    }

    public void AddNewOngoingEffect(OngoingEffectManager effect)
    {
        if (dead) return;

        ongoingEffects.Add(effect);
    }

    public void ModifyAllowedAttacks(int newAllowedAttacks)
    {
        attackLimiter.ModifyAllowedAttacks(this, newAllowedAttacks);
    }

    void Start()
    {
        ongoingEffectApplier = new OngoingEffectApplier();
    }

    void Update()
    {

    }

    public void Heal(float healAmount)
    {
        if (dead) return;

        health = Mathf.Min(health + healAmount, maxHealth);
        if (healthBar != null)
        {
            healthBar.value = health / maxHealth;
        }

        // Optional: Add healing visual effect
        if (damageVisualizer != null && damageNumberPrefab != null)
        {
            Vector3 position = transform.position;
            damageVisualizer.CreateHealingNumber(this, healAmount, position, damageNumberPrefab);
        }
    }

    public void ModifyAttack(float modifier)
    {
        if (dead) return;
        atkDamage += modifier;
        Debug.Log($"{name} attack modified by {modifier}. New damage: {atkDamage}");
    }

    public void SetDoubleAttack(int duration = 1)
    {
        if (dead) return;

        StartCoroutine(DoubleAttackRoutine(duration));
    }

    private IEnumerator DoubleAttackRoutine(int duration)
    {
        int originalAttacks = allowedAttacks;
        allowedAttacks *= 2;
        attackLimiter.ModifyAllowedAttacks(this, allowedAttacks);

        Debug.Log($"{name} gained double attack for {duration} turn(s)");

        // Wait for one turn duration
        yield return new WaitForSeconds(turnDuration);

        allowedAttacks = originalAttacks;
        attackLimiter.ModifyAllowedAttacks(this, allowedAttacks);
        Debug.Log($"{name}'s double attack expired");
    }

    public void TakeDamage(float damageAmount) => takeDamage(damageAmount);
    public float GetHealth() => getHealth();
    public void AttackBuff(float buffAmount) => attackBuff(buffAmount);
    public void AttackDebuff(float debuffAmount) => attackDebuff(debuffAmount);
    public void Attack(int damage) => attack(damage);
    public float GetAttackDamage() => getAttackDamage();
}
