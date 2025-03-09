using System.Resources;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

//---------------interfaces for different attributes--------------------------------//

public class EntityManager : MonoBehaviour, IDamageable, IAttacker
{
    [SerializeField]
    GameObject outlineImg;

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

    private List<OngoingEffect> ongoingEffects = new List<OngoingEffect>();

    public void InitializeMonster(_monsterType monsterType, float maxHealth, float atkDamage, Slider healthBarSlider, Image image, DamageVisualizer damageVisualizer, GameObject damageNumberPrefab)
    {
        this.monsterType = monsterType;
        this.maxHealth = maxHealth;
        this.health = maxHealth;
        this.atkDamage = atkDamage;
        this.spriteImage = image;
        this.damageVisualizer = damageVisualizer;
        this.damageNumberPrefab = damageNumberPrefab;

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

    public void ShowOutline()
    {
        selected = true;
        outlineImg.SetActive(true);
    }

    public void HideOutline()
    {
        if (outlineImg != null)
        {
            selected = false;
            outlineImg.SetActive(false);
        }
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
                // Instantiate the damage number prefab as a separate GameObject
                GameObject damageNumberInstance = Instantiate(damageNumberPrefab, position, Quaternion.identity);
                damageVisualizer.createDamageNumber(this, damageAmount, position, damageNumberInstance);
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

        // Hide the outline before starting the fade-out process
        HideOutline();

        // Start the coroutine to fade out and deactivate the game object
        StartCoroutine(FadeOutAndDeactivate(10.0f)); // Adjust the duration as needed
    }

    private IEnumerator FadeOutAndDeactivate(float duration)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Image uiImage = GetComponent<Image>();

        if (spriteRenderer == null && uiImage == null)
        {
            Debug.LogError("No SpriteRenderer or Image component found! Cannot fade out.");
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, alpha);
            }
            if (uiImage != null)
            {
                uiImage.color = new Color(uiImage.color.r, uiImage.color.g, uiImage.color.b, alpha);
            }

            yield return null;
        }

        // Ensure the object is fully transparent before deactivating
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0f);

        if (uiImage != null)
            uiImage.color = new Color(uiImage.color.r, uiImage.color.g, uiImage.color.b, 0f);

        gameObject.SetActive(false);
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

    public void AddOngoingEffect(OngoingEffect effect)
    {
        if (dead) return;

        ongoingEffects.Add(effect);
    }

    public void ApplyOngoingEffects()
    {
        if (dead) return;

        for (int i = ongoingEffects.Count - 1; i >= 0; i--)
        {
            OngoingEffect effect = ongoingEffects[i];
            effect.ApplyEffect(this);
            effect.DecreaseDuration();
            if (effect.IsExpired())
            {
                ongoingEffects.RemoveAt(i);
            }
        }
    }

    public void AddNewOngoingEffect(OngoingEffect effect)
    {
        if (dead) return;

        ongoingEffects.Add(effect);
    }

    void Start()
    {
        // Initialization logic if needed
    }

    void Update()
    {
        if (dead)
        {
            // Handle death logic
        }
    }
}
