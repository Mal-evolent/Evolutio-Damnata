using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCharacterHealth : MonoBehaviour, ICharacterHealth
{
    [Header("References")]
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private DamageVisualizer damageVisualizer;

    [Header("Damage Display Settings")]
    [SerializeField] private Vector3 damageNumberOffset = new Vector3(50f, 50f, 0);
    [SerializeField] private Vector3 healingNumberOffset = new Vector3(50f, 50f, 0);

    private ICombatManager _combatManager;

    // Properties from interface
    public float CurrentHealth => _combatManager != null ? _combatManager.PlayerHealth : 0;
    public float MaxHealth => 30f;
    public float HealthRatio => CurrentHealth / MaxHealth;

    private void Awake()
    {
        // Find the CombatManager in the scene
        _combatManager = FindObjectOfType<CombatManager>();
        if (_combatManager == null)
        {
            Debug.LogError("No CombatManager found in the scene!");
        }
    }

    private void Start()
    {
        // Update UI on start
        UpdateHealthBar();

        // Verify damage visualizer is assigned
        if (damageVisualizer == null)
        {
            Debug.LogWarning("DamageVisualizer not assigned in inspector. Some visual effects will not display.");
        }
    }

    public float TakeDamage(float amount)
    {
        if (_combatManager == null) return 0;

        int currentHealth = _combatManager.PlayerHealth;
        int newHealth = Mathf.Max(0, currentHealth - Mathf.RoundToInt(amount));
        float actualDamage = currentHealth - newHealth;

        // Update health in combat manager
        _combatManager.PlayerHealth = newHealth;

        // Check for death
        if (newHealth <= 0)
        {
            HandlePlayerDeath();
        }

        // Show damage number using visualizer
        ShowDamageNumber(actualDamage);

        // Update UI
        UpdateHealthBar();

        return actualDamage;
    }

    public float Heal(float amount)
    {
        if (_combatManager == null) return 0;

        int currentHealth = _combatManager.PlayerHealth;
        int newHealth = Mathf.Min(Mathf.RoundToInt(MaxHealth), currentHealth + Mathf.RoundToInt(amount));
        float actualHeal = newHealth - currentHealth;

        // Update health in combat manager
        _combatManager.PlayerHealth = newHealth;

        // Show healing number
        ShowHealingNumber(actualHeal);

        // Update UI
        UpdateHealthBar();

        return actualHeal;
    }

    public bool IsDead()
    {
        return CurrentHealth <= 0;
    }

    public void UpdateHealthBar()
    {
        if (_combatManager != null && _combatManager.PlayerHealthSlider != null)
        {
            _combatManager.PlayerHealthSlider.value = HealthRatio;
        }
    }

    // Visualize damage using IDamageVisualizer
    private void ShowDamageNumber(float damageAmount)
    {
        if (damageVisualizer != null && damageNumberPrefab != null && gameObject.activeInHierarchy)
        {
            // Get the position based on whether this is a UI element or world object
            RectTransform rectTransform = GetComponent<RectTransform>();
            Vector3 position;

            if (rectTransform != null)
            {
                // For UI elements, use the screen position as-is
                Canvas parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    // Get the rect position on screen
                    Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, transform.position);

                    // Apply the custom offset from inspector
                    screenPos += damageNumberOffset;

                    // Convert back to world position based on camera
                    position = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
                }
                else
                {
                    // Fallback if no canvas found
                    position = transform.position + damageNumberOffset;
                }
            }
            else
            {
                // For world objects, offset the position directly
                position = transform.position + damageNumberOffset;
            }

            damageVisualizer.CreateDamageNumber(
                null, // EntityManager is null for player character
                damageAmount,
                position,
                damageNumberPrefab
            );
        }
    }

    // Visualize healing using IDamageVisualizer
    private void ShowHealingNumber(float healAmount)
    {
        if (damageVisualizer != null && damageNumberPrefab != null && gameObject.activeInHierarchy)
        {
            // Get the position based on whether this is a UI element or world object
            RectTransform rectTransform = GetComponent<RectTransform>();
            Vector3 position;

            if (rectTransform != null)
            {
                // For UI elements, use the screen position as-is
                Canvas parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    // Get the rect position on screen
                    Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, transform.position);

                    // Apply the custom offset from inspector
                    screenPos += healingNumberOffset;

                    // Convert back to world position based on camera
                    position = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
                }
                else
                {
                    // Fallback if no canvas found
                    position = transform.position + healingNumberOffset;
                }
            }
            else
            {
                // For world objects, offset the position directly
                position = transform.position + healingNumberOffset;
            }

            damageVisualizer.CreateHealingNumber(
                null, // EntityManager is null for player character
                healAmount,
                position,
                damageNumberPrefab
            );
        }
    }

    // Conventional method for handling death
    private void HandlePlayerDeath()
    {
        Debug.Log("Player has died!");
    }

    // Public method that can be attached to button clicks
    public void DamageButtonClicked(float amount)
    {
        TakeDamage(amount);
    }
}
