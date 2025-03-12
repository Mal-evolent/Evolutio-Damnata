using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/*
 * This class, FadeOutEffect, provides a coroutine method FadeOutAndDeactivate that gradually fades out the visual components 
 * (SpriteRenderer, UI Image, and HealthBar) of a target GameObject over a specified duration and then deactivates the GameObject. 
 * Optionally, it can replace the sprite with an outline sprite before deactivation.
 */

public class FadeOutEffect : MonoBehaviour
{
    public IEnumerator FadeOutAndDeactivate(GameObject target, float duration, Sprite outlineSprite = null)
    {
        SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
        Image uiImage = target.GetComponent<Image>();
        Slider healthBar = target.GetComponentInChildren<Slider>();
        Image healthBarImage = healthBar != null ? healthBar.GetComponentInChildren<Image>() : null;

        if (spriteRenderer == null && uiImage == null && healthBarImage == null)
        {
            Debug.LogError("No SpriteRenderer, Image, or HealthBar component found! Cannot fade out.");
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, alpha);
            }
            if (uiImage != null)
            {
                uiImage.color = new Color(uiImage.color.r, uiImage.color.g, uiImage.color.b, alpha);
            }
            if (healthBarImage != null)
            {
                healthBarImage.color = new Color(healthBarImage.color.r, healthBarImage.color.g, healthBarImage.color.b, alpha);
            }

            yield return null;
        }

        if (spriteRenderer != null && outlineSprite != null)
        {
            spriteRenderer.sprite = outlineSprite;
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 1f);
        }
        if (uiImage != null && outlineSprite != null)
        {
            uiImage.sprite = outlineSprite;
            uiImage.color = new Color(uiImage.color.r, uiImage.color.g, uiImage.color.b, 1f);
        }
        if (healthBarImage != null)
        {
            healthBarImage.color = new Color(healthBarImage.color.r, healthBarImage.color.g, healthBarImage.color.b, 1f);
        }

        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }

        target.SetActive(false);
    }
}
