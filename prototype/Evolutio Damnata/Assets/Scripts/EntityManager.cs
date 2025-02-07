using System.Resources;
using UnityEngine;
using UnityEngine.UI;

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

    public bool dead = false;
    public bool placed = false;

    // Method to set monster type and initialize health bar
    public void InitializeMonster(_monsterType monsterType, float maxHealth, float atkDamage, Slider healthBarSlider, Image image)
    {
        this.monsterType = monsterType;
        this.maxHealth = maxHealth;
        this.health = maxHealth;
        this.atkDamage = atkDamage;
        this.spriteImage = image;

        // Use the passed Slider component reference
        healthBar = healthBarSlider;
        if (healthBar != null)
        {
            healthBar.maxValue = 1; // Set max value to 1 for percentage
            healthBar.value = health / maxHealth; // Normalize health to a percentage
            healthBar.gameObject.SetActive(true);
            Debug.Log($"Health bar initialized with value: {healthBar.value}");
        }
        else
        {
            Debug.LogError("Health bar Slider component not found!");
        }
    }

    // Toggle switch
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
        selected = false;
        outlineImg.SetActive(false);
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

    //-------------------- IDamageable Implementation --------------------//

    public void takeDamage(float damageAmount)
    {
        health -= damageAmount;
        if (healthBar != null)
        {
            healthBar.value = health / maxHealth; // Normalize health to a percentage
        }
        Debug.Log($"Health is now {health}");
        if (health <= 0)
        {
            Debug.Log("Monster is dead.");
            gameObject.SetActive(false);
            dead = true;
        }
    }

    // Heals the monster by amount
    public void heal(float healAmount)
    {
        health += healAmount;
        if (healthBar != null)
        {
            healthBar.value = health / maxHealth; // Normalize health to a percentage
        }
    }

    // Returns monster's current health
    public float getHealth()
    {
        return health;
    }

    //-------------------- IAttacker Implementation --------------------//
    // Returns monster's total attack
    public float getAttackDamage()
    {
        return atkDamage * atkDamageMulti; // Total attack damage calculation
    }

    // Buffs monster attack by amount (additive not replacement)
    public void attackBuff(float buffAmount)
    {
        atkDamage += buffAmount;
    }

    // Same as attackBuff but removes instead of adds
    public void attackDebuff(float buffAmount)
    {
        atkDamage -= buffAmount;
    }

    // Send attack event to room to apply damage
    public void attack(int targetID)
    {
        Debug.LogError("Attack functionality is not implemented.");
    }

    // Other methods, Start, and Update logic can remain unchanged
    void Start()
    {
        // Initialization logic if needed
    }

    void Update()
    {
        // Game logic per frame
    }
}
