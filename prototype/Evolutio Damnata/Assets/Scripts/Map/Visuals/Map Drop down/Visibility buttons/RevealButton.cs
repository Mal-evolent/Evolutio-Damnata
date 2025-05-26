using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the reveal button functionality for the map panel
/// </summary>
public class RevealButton : MonoBehaviour
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
            button.onClick.AddListener(ShowMap);

            // Ensure proper initial state
            gameObject.SetActive(!mapDropdown.isVisible);
        }
        else
        {
            Debug.LogError("Missing Button component or MapDropdownToggle reference");
        }
    }

    /// <summary>
    /// Shows the map when the button is clicked
    /// </summary>
    public void ShowMap()
    {
        if (mapDropdown != null && !mapDropdown.isVisible)
        {
            mapDropdown.isVisible = true;

            // Update button states - deactivate reveal button
            gameObject.SetActive(false);

            // Activate hide button
            if (mapDropdown.hideButton != null)
            {
                mapDropdown.hideButton.interactable = true;
                mapDropdown.hideButton.gameObject.SetActive(true);
            }

            // Start map sliding animation
            mapDropdown.StopAllCoroutines();
            mapDropdown.StartCoroutine(mapDropdown.SlideMap(mapDropdown.shownPos));
        }
    }
}
