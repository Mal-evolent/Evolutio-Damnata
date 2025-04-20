using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeOutEffect : MonoBehaviour
{
    public IEnumerator FadeOutAndDeactivate(GameObject target, float duration, Sprite outlineSprite = null, System.Action onComplete = null)
    {
        SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
        Image uiImage = target.GetComponent<Image>();
        Slider healthBar = target.GetComponentInChildren<Slider>();
        Image healthBarImage = healthBar != null ? healthBar.GetComponentInChildren<Image>() : null;

        // Get the EntityManager component
        EntityManager entityManager = target.GetComponent<EntityManager>();

        Debug.Log($"Starting FadeOutAndDeactivate for {target.name}");

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

        if (entityManager != null)
        {
            entityManager.toggleUIStatStates(false);
        }

        target.SetActive(false);
        Debug.Log($"FadeOutAndDeactivate completed for {target.name}");

        onComplete?.Invoke(); 
    }
}
