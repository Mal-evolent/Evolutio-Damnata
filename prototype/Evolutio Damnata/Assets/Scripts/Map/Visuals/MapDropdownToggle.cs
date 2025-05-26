using System.Collections;
using UnityEngine;

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

    private Vector2 hiddenPos;
    private Vector2 shownPos;
    private bool isVisible = false;

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
    /// </summary>
    public void ToggleMap()
    {
        isVisible = !isVisible;
        StopAllCoroutines();
        StartCoroutine(SlideMap(isVisible ? shownPos : hiddenPos));
    }

    /// <summary>
    /// Animates the map panel smoothly to the target position.
    /// </summary>
    /// <param name="targetPos">The position to slide the map to</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    System.Collections.IEnumerator SlideMap(Vector2 targetPos)
    {
        while (Vector2.Distance(mapPanel.anchoredPosition, targetPos) > 0.1f)
        {
            mapPanel.anchoredPosition = Vector2.Lerp(mapPanel.anchoredPosition, targetPos, Time.deltaTime * 10);
            yield return null;
        }
        mapPanel.anchoredPosition = targetPos;
    }
}
