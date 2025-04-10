using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICharacterHealth
{
    /// <summary>
    /// Gets the current health amount.
    /// </summary>
    float CurrentHealth { get; }

    /// <summary>
    /// Gets the maximum health amount.
    /// </summary>
    float MaxHealth { get; }

    /// <summary>
    /// Gets the health ratio (CurrentHealth / MaxHealth).
    /// </summary>
    float HealthRatio { get; }

    /// <summary>
    /// Damages the character by subtracting health.
    /// </summary>
    /// <param name="amount">The amount of health to subtract.</param>
    /// <returns>The actual damage taken.</returns>
    float TakeDamage(float amount);

    /// <summary>
    /// Heals the character by adding health.
    /// </summary>
    /// <param name="amount">The amount of health to add.</param>
    /// <returns>The actual amount healed.</returns>
    float Heal(float amount);

    /// <summary>
    /// Checks if the character is dead (health <= 0).
    /// </summary>
    /// <returns>True if the character is dead, false otherwise.</returns>
    bool IsDead();

    /// <summary>
    /// Updates the health bar visual to reflect the current health.
    /// </summary>
    void UpdateHealthBar();

}
