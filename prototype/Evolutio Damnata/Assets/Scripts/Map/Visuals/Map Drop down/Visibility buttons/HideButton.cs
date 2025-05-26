using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the hide button functionality for the map panel
/// </summary>
public class HideButton : MonoBehaviour
{
    [Tooltip("Reference to the MapDropdownToggle component")]
    public MapDropdownToggle mapDropdown;

    private Button button;

    /// <summary>
    /// Initialize component references and set up button listener
    /// </summary>
    void Start()
    {
        button = GetComponent<Button>();
        if (button != null && mapDropdown != null)
        {
            button.onClick.AddListener(HideMap);

            // Ensure proper initial state
            gameObject.SetActive(mapDropdown.isVisible);
        }
        else
        {
            Debug.LogError("Missing Button component or MapDropdownToggle reference");
        }
    }

    /// <summary>
    /// Updates button visibility based on map state
    /// </summary>
    void Update()
    {
        // Keep button visibility synced with map state (for keyboard toggles)
        if (mapDropdown != null && gameObject.activeSelf != mapDropdown.isVisible)
        {
            gameObject.SetActive(mapDropdown.isVisible);
        }
    }

    /// <summary>
    /// Hides the map when the button is clicked
    /// </summary>
    public void HideMap()
    {
        if (mapDropdown != null && mapDropdown.isVisible)
        {
            mapDropdown.isVisible = false;

            // Update show button state
            if (mapDropdown.showButton != null)
            {
                mapDropdown.showButton.interactable = true;
                mapDropdown.showButton.gameObject.SetActive(true);
            }

            // Deactivate hide button
            gameObject.SetActive(false);

            // Start map sliding animation
            mapDropdown.StopAllCoroutines();
            mapDropdown.StartCoroutine(mapDropdown.SlideMap(mapDropdown.hiddenPos));
        }
    }
}
