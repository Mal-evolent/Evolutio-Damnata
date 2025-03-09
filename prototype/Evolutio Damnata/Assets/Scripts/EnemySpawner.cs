using UnityEngine;
using UnityEngine.UI;

/* 
 * EnemySpawner class is used to spawn enemies in the game.
 * It contains methods to spawn enemies in the game world.
 */

public class EnemySpawner
{
    private SpritePositioning spritePositioning;
    private CardLibrary cardLibrary;
    private DamageVisualizer damageVisualizer;
    private GameObject damageNumberPrefab;
    private Sprite wizardOutlineSprite;

    public EnemySpawner(SpritePositioning spritePositioning, CardLibrary cardLibrary, DamageVisualizer damageVisualizer, GameObject damageNumberPrefab, Sprite wizardOutlineSprite)
    {
        this.spritePositioning = spritePositioning;
        this.cardLibrary = cardLibrary;
        this.damageVisualizer = damageVisualizer;
        this.damageNumberPrefab = damageNumberPrefab;
        this.wizardOutlineSprite = wizardOutlineSprite;
    }

    public void SpawnEnemy(string cardName, int whichOutline)
    {
        if (whichOutline < 0 || whichOutline >= spritePositioning.enemyEntities.Count)
        {
            Debug.LogError($"Invalid outline index: {whichOutline}");
            return;
        }

        // Check if the placeholder is already populated
        GameObject enemyPlaceholder = spritePositioning.enemyEntities[whichOutline];
        if (enemyPlaceholder == null)
        {
            Debug.LogError($"Placeholder at index {whichOutline} is null!");
            return;
        }

        EntityManager existingEntityManager = enemyPlaceholder.GetComponent<EntityManager>();
        if (existingEntityManager != null && existingEntityManager.placed)
        {
            Debug.LogError("Cannot place a card in an already populated placeholder.");
            return;
        }

        CardData selectedCardData = null;
        foreach (CardData cardData in cardLibrary.cardDataList)
        {
            if (cardName == cardData.CardName)
            {
                selectedCardData = cardData;
                break;
            }
        }

        if (selectedCardData == null)
        {
            Debug.LogError($"Card data not found for card name: {cardName}");
            return;
        }

        // Set monster attributes
        Image placeholderImage = enemyPlaceholder.GetComponent<Image>();
        if (placeholderImage != null)
        {
            placeholderImage.sprite = cardLibrary.cardImageGetter(cardName);
        }

        // Add the EntityManager component to the placeholder
        EntityManager entityManager = enemyPlaceholder.GetComponent<EntityManager>();
        if (entityManager == null)
        {
            entityManager = enemyPlaceholder.AddComponent<EntityManager>();
        }

        // Find the health bar Slider component using transform.Find
        Transform healthBarTransform = enemyPlaceholder.transform.Find("healthBar");
        Slider healthBarSlider = healthBarTransform != null ? healthBarTransform.GetComponent<Slider>() : null;

        entityManager.placed = true;

        // Initialize the monster with the appropriate type, attributes, and outline image
        entityManager.InitializeMonster(EntityManager._monsterType.Enemy, selectedCardData.Health, selectedCardData.AttackPower, healthBarSlider, placeholderImage, damageVisualizer, damageNumberPrefab, wizardOutlineSprite);

        // Rename the placeholder to the card name
        enemyPlaceholder.name = cardName;

        // Display the health bar
        DisplayHealthBar(enemyPlaceholder, true);

        // Activate the placeholder game object
        enemyPlaceholder.SetActive(true);
    }

    private void DisplayHealthBar(GameObject entity, bool active)
    {
        Transform healthBarTransform = entity.transform.Find("healthBar");
        if (healthBarTransform != null)
        {
            healthBarTransform.gameObject.SetActive(active);
        }
    }
}
