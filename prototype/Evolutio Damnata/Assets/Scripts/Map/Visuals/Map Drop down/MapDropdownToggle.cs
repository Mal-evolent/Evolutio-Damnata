using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the sliding animation of a map panel UI element.
/// Provides functionality to toggle the visibility of the map
/// by moving it on and off screen.
/// </summary>
public class MapDropdownToggle : MonoBehaviour
{
    [Tooltip("Reference to the map panel that will be shown/hidden")]
    public RectTransform mapPanel;

    [Tooltip("Speed at which the map panel slides in and out")]
    public float slideSpeed = 500f;

    [HideInInspector]
    public Button showButton;

    [HideInInspector]
    public Button hideButton;

    [HideInInspector]
    public Vector2 hiddenPos;
    [HideInInspector]
    public Vector2 shownPos;
    [HideInInspector]
    public bool isVisible = false;

    /// <summary>
    /// Initializes positions and starts with the map hidden.
    /// </summary>
    void Start()
    {
        // Get initial positions
        shownPos = mapPanel.anchoredPosition;
        hiddenPos = shownPos + new Vector2(0, mapPanel.rect.height + 50); // Offset up
        mapPanel.anchoredPosition = hiddenPos; // Start hidden
    }

    /// <summary>
    /// Checks for input to toggle the map visibility.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) // Change to your input method
        {
            ToggleMap();
        }
    }

    /// <summary>
    /// Toggles the visibility state of the map and initiates sliding animation.
    /// Also updates button visibility states.
    /// </summary>
    public void ToggleMap()
    {
        isVisible = !isVisible;
        StopAllCoroutines();
        StartCoroutine(SlideMap(isVisible ? shownPos : hiddenPos));

        // Update button visibility states
        UpdateButtonStates();
    }

    /// <summary>
    /// Updates the visibility of show/hide buttons based on the current map state
    /// </summary>
    private void UpdateButtonStates()
    {
        if (showButton != null)
        {
            showButton.gameObject.SetActive(!isVisible);
            showButton.interactable = !isVisible;
        }

        if (hideButton != null)
        {
            hideButton.gameObject.SetActive(isVisible);
            hideButton.interactable = isVisible;
        }
    }

    /// <summary>
    /// Animates the map panel smoothly to the target position.
    /// </summary>
    /// <param name="targetPos">The position to slide the map to</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    public IEnumerator SlideMap(Vector2 targetPos)
    {
        while (Vector2.Distance(mapPanel.anchoredPosition, targetPos) > 0.1f)
        {
            mapPanel.anchoredPosition = Vector2.Lerp(mapPanel.anchoredPosition, targetPos, Time.deltaTime * 10);
            yield return null;
        }
        mapPanel.anchoredPosition = targetPos;
    }
}
