using UnityEngine;
using UnityEngine.UI;

public class ButtonCreator : MonoBehaviour
{
    private Canvas battleField;
    private SpritePositioning spritePositioning;
    private CardSelectionHandler cardSelectionHandler;

    private readonly Vector2 buttonSize = new Vector2(217.9854f, 322.7287f);
    private readonly Vector2 enemyButtonSize = new Vector2(114.2145f, 188.1686f);

    public void Initialize(Canvas battleField, SpritePositioning spritePositioning, CardSelectionHandler cardSelectionHandler)
    {
        this.battleField = battleField;
        this.spritePositioning = spritePositioning;
        this.cardSelectionHandler = cardSelectionHandler;
    }

    public void AddButtonsToPlayerEntities()
    {
        for (int i = 0; i < spritePositioning.playerEntities.Count; i++)
        {
            if (spritePositioning.playerEntities[i] == null)
            {
                Debug.LogError($"Player placeholder at index {i} is null!");
                continue;
            }

            CreatePlayerButton(i);
        }
    }

    private void CreatePlayerButton(int index)
    {
        GameObject playerEntity = spritePositioning.playerEntities[index];

        // Set RaycastTarget to false for the placeholder outline
        Image placeholderImage = playerEntity.GetComponent<Image>();
        if (placeholderImage != null)
        {
            placeholderImage.raycastTarget = false;
        }

        // Create a new GameObject for the Button
        GameObject buttonObject = new GameObject($"Button_Outline_{index}");
        buttonObject.transform.SetParent(playerEntity.transform, false); // Add as a child of the Placeholder
        buttonObject.transform.localPosition = Vector3.zero; // Center the Button inside the Placeholder

        // Add required components to make it a Button
        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = buttonSize; // Set the size of the Button to match the placeholder size

        Button buttonComponent = buttonObject.AddComponent<Button>();

        // Optional: Add an Image component to visualize the Button
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new UnityEngine.Color(1, 1, 1, 0); // Transparent background for the Button

        // Add onClick functionality
        buttonComponent.onClick.AddListener(() => cardSelectionHandler.OnPlayerButtonClick(index));
    }

    public void AddButtonsToEnemyEntities()
    {
        for (int i = 0; i < spritePositioning.enemyEntities.Count; i++)
        {
            if (spritePositioning.enemyEntities[i] == null)
            {
                Debug.LogError($"Enemy placeholder at index {i} is null!");
                continue;
            }

            CreateEnemyButton(i);
        }
    }

    private void CreateEnemyButton(int index)
    {
        GameObject enemyEntity = spritePositioning.enemyEntities[index];

        // Store the placeholder's world position before parenting
        Vector3 originalWorldPos = enemyEntity.transform.position;
        Vector3 originalScale = enemyEntity.transform.localScale;
        Quaternion originalRotation = enemyEntity.transform.rotation;

        // Create a new GameObject for the Button
        GameObject buttonObject = new GameObject($"Enemy_Button_Outline_{index}");

        // Set the button's parent to the top-level canvas (so it's clickable)
        buttonObject.transform.SetParent(battleField.transform, false);

        // Restore the button's world position
        buttonObject.transform.position = originalWorldPos;

        // Add required UI components
        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = enemyButtonSize; // Use the enemy button size

        Button buttonComponent = buttonObject.AddComponent<Button>();

        // Optional: Add an Image component for debugging (can be made transparent)
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new UnityEngine.Color(1, 1, 1, 0);
        buttonImage.raycastTarget = true;

        // Convert the placeholder's world position into the button's local space
        Vector3 localPos = buttonObject.transform.InverseTransformPoint(originalWorldPos);

        // Now make the placeholder a child of the button
        enemyEntity.transform.SetParent(buttonObject.transform, false);

        // Restore the correct local position, scale, and rotation
        enemyEntity.transform.localPosition = localPos;
        enemyEntity.transform.localScale = originalScale;
        enemyEntity.transform.rotation = originalRotation;

        // Add onClick functionality
        buttonComponent.onClick.AddListener(() => cardSelectionHandler.OnEnemyButtonClick(index));

        Debug.Log($"Button {index} created, parented correctly, and position fixed.");
    }
}
