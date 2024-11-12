using UnityEngine;
using System.Collections.Generic;

public class BackgroundResizer : MonoBehaviour
{
    private Camera mainCamera;

    public List<SpriteRenderer> backgroundSprites;

    // Custom scaling factor accessible in the Unity Inspector
    [Range(0.1f, 10f)] // Optionally limit the range for better control
    public float scaleMultiplier = 1f;

    void Start()
    {
        mainCamera = Camera.main;

        // Center the parent GameObject at the camera's position
        transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, 0);

        // Resize the sprites based on the camera's view
        ResizeSpritesToScreen();
    }

    //-----FOR TESTING!!!! REMOVE LATER!!!!-----//
    void Update()
    {
        // Check if the screen size has changed
        if (Screen.width != mainCamera.pixelWidth || Screen.height != mainCamera.pixelHeight)
        {
            // Resize the sprites based on the camera's view
            ResizeSpritesToScreen();
        }
    }

    void ResizeSpritesToScreen()
    {
        if (backgroundSprites.Count == 0)
        {
            Debug.LogError("No SpriteRenderers assigned!");
            return;
        }

        // Calculate the aspect ratio of the first sprite as a reference
        float originalWidth = backgroundSprites[0].sprite.bounds.size.x;
        float originalHeight = backgroundSprites[0].sprite.bounds.size.y;
        float desiredAspect = originalWidth / originalHeight;

        // Calculate the aspect ratio of the screen
        float screenAspect = (float)Screen.width / (float)Screen.height;

        // Determine the base scale factor based on the screen aspect ratio
        float baseScale;
        if (screenAspect >= desiredAspect)
        {
            // Screen is wider; match height
            baseScale = Camera.main.orthographicSize * 2 / originalHeight;
        }
        else
        {
            // Screen is taller; match width
            baseScale = (Camera.main.orthographicSize * 2 * screenAspect) / originalWidth;
        }

        // Resize each sprite in the array
        foreach (SpriteRenderer spriteRenderer in backgroundSprites)
        {
            if (spriteRenderer != null)
            {
                float finalScale = baseScale * scaleMultiplier;
                spriteRenderer.transform.localScale = new Vector3(finalScale, finalScale, 1f);

                // Center the sprite in the camera view
                spriteRenderer.transform.position = new Vector3(0, 0, 0);
            }
            else
            {
                Debug.LogWarning("A SpriteRenderer in the array is null!");
            }
        }
    }
}
