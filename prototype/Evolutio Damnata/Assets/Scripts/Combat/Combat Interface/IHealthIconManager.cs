using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Defines the contract for managing health icons in the combat system.
/// Provides functionality for health tracking and state management of health icons.
/// </summary>
public interface IHealthIconManager
{
    /// <summary>
    /// Gets whether this health icon represents the player.
    /// </summary>
    bool IsPlayerIcon { get; }

    /// <summary>
    /// Gets the current health value of the icon.
    /// </summary>
    float CurrentHealth { get; }

    /// <summary>
    /// Gets the maximum health value of the icon.
    /// </summary>
    float MaxHealth { get; }

    /// <summary>
    /// Sets the health value of the icon.
    /// </summary>
    /// <param name="newHealth">The new health value to set</param>
    void SetHealth(float newHealth);
} 