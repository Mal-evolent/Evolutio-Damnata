using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Combat.Reset;

/// <summary>
/// Handles resetting the health icons to their initial state when combat resets
/// </summary>
public class HealthIconResetter : MonoBehaviour, IResettable
{
    [SerializeField] private int _resetPriority = 2;
    
    // Implement IResettable interface
    public int ResetPriority => _resetPriority;

    private void Awake()
    {
        // Register with the reset manager when the component is initialized
        var resetManager = FindObjectOfType<CombatResetManager>();
        if (resetManager != null)
        {
            resetManager.RegisterResettable(this);
            Debug.Log("[HealthIconResetter] Registered with CombatResetManager");
        }
        else
        {
            Debug.LogWarning("[HealthIconResetter] No CombatResetManager found in scene");
        }
    }

    private void OnDestroy()
    {
        // Unregister when destroyed to prevent memory leaks
        var resetManager = FindObjectOfType<CombatResetManager>();
        if (resetManager != null)
        {
            resetManager.UnregisterResettable(this);
        }
    }

    public async Task ResetAsync()
    {
        Debug.Log("[HealthIconResetter] Resetting health icons");
        ResetHealthIcons();
        await Task.CompletedTask; // For async compatibility
    }

    private void ResetHealthIcons()
    {
        // Find all health icons in the scene, including inactive ones
        HealthIconManager[] healthIcons = FindObjectsOfType<HealthIconManager>(includeInactive: true);
        
        Debug.Log($"[HealthIconResetter] Found {healthIcons.Length} health icons to reset");
        
        foreach (var icon in healthIcons)
        {
            GameObject iconObject = icon.gameObject;
            
            // Reactivate the icon if it's inactive
            if (!iconObject.activeSelf)
            {
                iconObject.SetActive(true);
                Debug.Log($"[HealthIconResetter] Reactivated health icon: {iconObject.name}");
            }
            
            // Reset alpha values on components
            ResetAlphaValues(iconObject);
            
            // Toggle UI stats on
            icon.toggleUIStatStates(true);
        }
    }
    
    private void ResetAlphaValues(GameObject iconObject)
    {
        // Reset alpha for SpriteRenderer
        SpriteRenderer spriteRenderer = iconObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.color.a < 1f)
        {
            Color color = spriteRenderer.color;
            spriteRenderer.color = new Color(color.r, color.g, color.b, 1f);
            Debug.Log($"[HealthIconResetter] Reset SpriteRenderer alpha for {iconObject.name}");
        }
        
        // Reset alpha for Image
        Image uiImage = iconObject.GetComponent<Image>();
        if (uiImage != null && uiImage.color.a < 1f)
        {
            Color color = uiImage.color;
            uiImage.color = new Color(color.r, color.g, color.b, 1f);
            Debug.Log($"[HealthIconResetter] Reset Image alpha for {iconObject.name}");
        }
        
        // Reset health bars
        Slider healthBar = iconObject.GetComponentInChildren<Slider>(includeInactive: true);
        if (healthBar != null)
        {
            if (!healthBar.gameObject.activeSelf)
            {
                healthBar.gameObject.SetActive(true);
                Debug.Log($"[HealthIconResetter] Reactivated health bar for {iconObject.name}");
            }
            
            Image healthBarImage = healthBar.GetComponentInChildren<Image>();
            if (healthBarImage != null && healthBarImage.color.a < 1f)
            {
                Color color = healthBarImage.color;
                healthBarImage.color = new Color(color.r, color.g, color.b, 1f);
                Debug.Log($"[HealthIconResetter] Reset health bar Image alpha for {iconObject.name}");
            }
        }
    }
}