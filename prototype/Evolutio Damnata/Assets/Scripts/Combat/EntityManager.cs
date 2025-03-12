using System.Resources;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/**
 * This class is responsible for managing the entity's health, attack damage, and ongoing effects.
 * It implements the IDamageable and IAttacker interfaces to handle damage and attack logic.
 */

//---------------interfaces for different attributes--------------------------------//

public class EntityManager : MonoBehaviour, IDamageable, IAttacker
{
    public Sprite outlineSprite;

    [SerializeField]
    ResourceManager resourceManager;

    bool selected = false;

    public enum _monsterType
    {
        player,
        Friendly,
        Enemy,
        Boss
    }
    _monsterType monsterType;

    [SerializeField]
    Image spriteImage;
    [SerializeField]
    float health;
    [SerializeField]
    float maxHealth;
    [SerializeField]
    float atkDamage;
    [SerializeField]
    float atkDamageMulti = 1.0f;

    [SerializeField]
    Slider healthBar;

    [SerializeField]
    DamageVisualizer damageVisualizer;

    [SerializeField]
    GameObject damageNumberPrefab;

    public bool dead = false;
    public bool placed = false;

    private List<OngoingEffectManager> ongoingEffects = new List<OngoingEffectManager>();
    private OngoingEffectApplier ongoingEffectApplier;

    public void InitializeMonster(_monsterType monsterType, float maxHealth, float atkDamage, Slider healthBarSlider, Image image, DamageVisualizer damageVisualizer, GameObject damageNumberPrefab, Sprite outlineSprite)
    {
        this.monsterType = monsterType;
        this.maxHealth = maxHealth;
        this.health = maxHealth;
        this.atkDamage = atkDamage;
        this.spriteImage = image;
        this.damageVisualizer = damageVisualizer;
        this.damageNumberPrefab = damageNumberPrefab;
        this.outlineSprite = outlineSprite;

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

    public bool OutlineSelect()
    {
        selected = !selected;
        return selected;
    }

    public void loadMonster()
    {
        gameObject.SetActive(true);
    }

    public void unloadMonster()
    {
        gameObject.SetActive(false);
    }

    public _monsterType getMonsterType()
    {
        return monsterType;
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
                GameObject damageNumberInstance = Instantiate(damageNumberPrefab, position, Quaternion.identity);

                damageVisualizer.CreateDamageNumber(this, damageAmount, position, damageNumberInstance);


                Destroy(damageNumberInstance, 1.5f);
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
        RemoveAllOngoingEffects();
        Debug.Log("Monster is dead.");

        FadeOutEffect fadeOutEffect = gameObject.AddComponent<FadeOutEffect>();
        StartCoroutine(fadeOutEffect.FadeOutAndDeactivate(gameObject, 6.5f, outlineSprite));
    }

    private void RemoveAllOngoingEffects()
    {
        ongoingEffects.Clear();
    }

    public void heal(float healAmount)
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

    void Start()
    {
        ongoingEffectApplier = new OngoingEffectApplier(ongoingEffects);
        // Other initialization logic if needed
    }

    void Update()
    {

    }
}
