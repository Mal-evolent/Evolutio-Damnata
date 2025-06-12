using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using GameManagement;

/// <summary>
/// <summary>
/// Manages the health icon representation for both player and enemy entities.
/// Inherits from EntityManager to leverage health and damage functionality while
/// providing specialized behavior for health icons.
/// </summary>
public class HealthIconManager : EntityManager, IHealthIconManager
{
    #region Fields
    private bool isPlayerIcon;
    private CombatStage combatStageRef;
    private CombatManager combatManagerRef;
    #endregion

    #region Properties
    /// <summary>
    /// Gets whether this health icon represents the player.
    /// </summary>
    public bool IsPlayerIcon => isPlayerIcon;

    /// <summary>
    /// Gets the current health value of the icon.
    /// </summary>
    public float CurrentHealth => GetHealth();

    /// <summary>
    /// Gets the maximum health value of the icon.
    /// </summary>
    public float MaxHealth => base.GetMaxHealth();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeIcon();
        InitializeReferences();
        InitializeFromCombatManager();
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the basic icon settings and state.
    /// </summary>
    private void InitializeIcon()
    {
        isPlayerIcon = gameObject.CompareTag("Player");
        placed = true;
        dead = false;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Initializes references to required components.
    /// </summary>
    private void InitializeReferences()
    {
        combatStageRef = FindObjectOfType<CombatStage>();
        if (combatStageRef == null)
        {
            Debug.LogError($"[{nameof(HealthIconManager)}] Could not find CombatStage in scene!");
            return;
        }

        combatManagerRef = FindObjectOfType<CombatManager>();
        if (combatManagerRef == null)
        {
            Debug.LogError($"[{nameof(HealthIconManager)}] Could not find CombatManager in scene!");
            return;
        }
    }

    /// <summary>
    /// Initializes health values from the combat manager.
    /// </summary>
    private void InitializeFromCombatManager()
    {
        // Get the current health and max health values
        float currentHealth = isPlayerIcon ? combatManagerRef.PlayerHealth : combatManagerRef.EnemyHealth;
        float maxHealthValue = isPlayerIcon ? combatManagerRef.PlayerMaxHealth : combatManagerRef.EnemyMaxHealth;
        Slider healthBarRef = isPlayerIcon ? combatManagerRef.PlayerHealthSlider : combatManagerRef.EnemyHealthSlider;
        
        // Initialize with current health instead of max health
        InitializeWithValues(currentHealth, healthBarRef);
    }

    /// <summary>
    /// Initializes the health icon with specific values.
    /// </summary>
    /// <param name="maxHealth">Maximum health value</param>
    /// <param name="healthBar">Reference to the health bar UI element</param>
    private void InitializeWithValues(float currentHealth, Slider healthBar)
    {
        this.maxHealth = isPlayerIcon ? combatManagerRef.PlayerMaxHealth : combatManagerRef.EnemyMaxHealth;
        this.health = currentHealth;

        InitializeMonster(
            isPlayerIcon ? MonsterType.Friendly : MonsterType.Enemy,
            this.maxHealth,
            0f,
            healthBar,
            GetComponent<Image>(),
            FindObjectOfType<DamageVisualizer>(),
            Resources.Load<GameObject>("Prefabs/numberVisual"),
            null,
            combatStageRef.GetAttackLimiter(),
            combatStageRef.GetOngoingEffectApplier(),
            currentHealth  // Pass the current health
        );

        gameObject.SetActive(true);
        SetAllowedAttacks(0);
    }
    #endregion

    #region Health Management
    /// <summary>
    /// Sets the health value of the icon and updates related UI elements.
    /// </summary>
    /// <param name="newHealth">The new health value to set</param>
    public void SetHealth(float newHealth)
    {
        if (newHealth > MaxHealth)
            health = MaxHealth;
        else if (newHealth <= 0)
            Die();
        else
            health = newHealth;

        UpdateCombatManagerHealth();
    }

    /// <summary>
    /// Updates the health value in the combat manager.
    /// </summary>
    private void UpdateCombatManagerHealth()
    {
        if (combatManagerRef == null) return;

        if (isPlayerIcon)
        {
            combatManagerRef.PlayerHealth = (int)health;
        }
        else
            combatManagerRef.EnemyHealth = (int)health;
    }

    /// <summary>
    /// Handles the death state of the health icon.
    /// </summary>
    protected override void Die()
    {
        base.Die();

        if (isPlayerIcon)
        {
            Debug.Log("Player Defeated - Game Over!");

            // Wait a moment before changing scenes
            StartCoroutine(LoadGameOverScene("DefeatedScene"));
        }
        else
        {
            Debug.Log("Enemy Defeated - Room Cleared!");
        }
    }

    /// <summary>
    /// Coroutine to load the game over scene after a short delay
    /// </summary>
    /// <param name="sceneName">The name of the scene to load</param>
    private IEnumerator LoadGameOverScene(string sceneName)
    {
        // Wait for death animation to play
        yield return new WaitForSeconds(2f);

        // Import SceneManager namespace at the top of your file:
        // using UnityEngine.SceneManagement;
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadVictoryScene(string sceneName)
    {
        // Wait for death animation to play
        yield return new WaitForSeconds(2f);

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Applies damage to the health icon and updates related systems.
    /// </summary>
    /// <param name="damageAmount">Amount of damage to apply</param>
    public override void TakeDamage(float damageAmount)
    {
        base.TakeDamage(damageAmount);
        UpdateCombatManagerHealth();

        if (GetHealth() / MaxHealth <= 0.25f)
            Debug.Log($"{(isPlayerIcon ? "Player" : "Enemy")} health critical!");
    }
    #endregion

    #region Attack Management
    public override void Attack(int damage) => Debug.LogWarning("Health icons cannot attack.");
    public override float GetAttackDamage() => 0f;
    public override void AttackBuff(float buffAmount) { }
    public override void AttackDebuff(float debuffAmount) { }

    /// <summary>
    /// Applies blood price damage which is self-inflicted and should not be
    /// attributed to an opponent in the combat history.
    /// </summary>
    /// <param name="damageAmount">Amount of blood price damage to apply</param>
    public void ApplyBloodPriceDamage(float damageAmount)
    {
        if (damageAmount <= 0 || dead) return;

        // Apply the damage directly to health
        float healthBefore = health;
        health = Mathf.Max(0, health - damageAmount);

        // Update the combat manager health
        UpdateCombatManagerHealth();

        // Show damage number
        ShowDamageNumber(damageAmount);

        // IMPORTANT: Record this as self-damage in the card history
        if (CardHistory.Instance != null && combatManagerRef != null)
        {
            // Use this entity as both attacker and target to record self-damage
            CardHistory.Instance.RecordAttack(
                this,                        // Self as attacker
                this,                        // Self as target
                combatManagerRef.TurnCount,  // Current turn
                damageAmount,                // Damage dealt
                0f,                          // No counter damage for self-inflicted damage
                false                        // Not a ranged attack
            );
            Debug.Log($"[CardHistory] Recorded Blood Price self-damage of {damageAmount} for {(isPlayerIcon ? "Player" : "Enemy")}");
        }

        // Log with clear indication this is self-inflicted blood price damage
        Debug.Log($"[Blood Price] {(isPlayerIcon ? "Player" : "Enemy")} used Blood Price and took {damageAmount} self-inflicted damage. Health: {health}/{MaxHealth}");

        // Check for critical health
        if (GetHealth() / MaxHealth <= 0.25f)
            Debug.Log($"{(isPlayerIcon ? "Player" : "Enemy")} health critical!");

        // Check for death
        if (health <= 0 && healthBefore > 0)
            Die();
    }
    #endregion
}
