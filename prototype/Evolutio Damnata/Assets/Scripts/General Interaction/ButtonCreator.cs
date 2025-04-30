using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace GeneralInteraction
{
    public class ButtonCreator : MonoBehaviour, IButtonCreator
    {
        [Header("Dependencies")]
        [SerializeField] private Canvas _battleField;
        [SerializeField] private SpritePositioning _spritePositioningComponent;
        [SerializeField] private CardSelectionHandler _cardSelectionHandlerComponent;

        private ISpritePositioning _spritePositioning;
        private ICardSelectionHandler _cardSelectionHandler;

        [Header("Button Settings")]
        private readonly Vector2 _buttonSize = new Vector2(217.9854f, 322.7287f);
        private readonly Vector2 _enemyButtonSize = new Vector2(114.2145f, 188.1686f);

        private void Awake()
        {
            // Initialize interface references
            _spritePositioning = _spritePositioningComponent;
            _cardSelectionHandler = _cardSelectionHandlerComponent;

            // Fallback if not set in inspector
            if (_battleField == null) _battleField = FindObjectOfType<Canvas>();
            if (_cardSelectionHandler == null) _cardSelectionHandler = FindObjectOfType<CardSelectionHandler>();
            if (_spritePositioning == null && _spritePositioningComponent == null)
                _spritePositioning = FindObjectOfType<SpritePositioning>();
        }

        public void Initialize(Canvas battleField, ISpritePositioning spritePositioning, ICardSelectionHandler cardSelectionHandler)
        {
            _battleField = battleField;
            _spritePositioning = spritePositioning;
            _cardSelectionHandler = cardSelectionHandler;
        }

        public void AddButtonsToPlayerEntities()
        {
            if (_spritePositioning?.PlayerEntities == null)
            {
                Debug.LogError("Player entities not initialized!");
                return;
            }

            for (int i = 0; i < _spritePositioning.PlayerEntities.Count; i++)
            {
                if (_spritePositioning.PlayerEntities[i] == null)
                {
                    Debug.LogError($"Player placeholder at index {i} is null!");
                    continue;
                }
                CreatePlayerButton(i);
            }
        }

        public void AddButtonsToEnemyEntities()
        {
            if (_spritePositioning?.EnemyEntities == null)
            {
                Debug.LogError("Enemy entities not initialized!");
                return;
            }

            for (int i = 0; i < _spritePositioning.EnemyEntities.Count; i++)
            {
                if (_spritePositioning.EnemyEntities[i] == null)
                {
                    Debug.LogError($"Enemy placeholder at index {i} is null!");
                    continue;
                }
                CreateEnemyButton(i);
            }
        }

        // Modification to ButtonCreator.cs - Adding player health icon button support
        public void AddButtonsToHealthIcons()
        {
            // Create buttons for enemy health icons
            var enemyHealthIcons = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var icon in enemyHealthIcons)
            {
                if (icon == null) continue;
                CreateHealthIconButton(icon, "Enemy_Health_Button", icon.transform.position, true);
            }

            // Add buttons for player health icons
            var playerHealthIcons = GameObject.FindGameObjectsWithTag("Player");
            foreach (var icon in playerHealthIcons)
            {
                if (icon == null) continue;
                CreateHealthIconButton(icon, "Player_Health_Button", icon.transform.position, false);
            }
        }

        private void CreateHealthIconButton(GameObject icon, string buttonName, Vector3 position, bool isEnemyIcon)
        {
            // Create button as canvas child
            GameObject buttonObject = new GameObject(buttonName)
            {
                transform =
                {
                    parent = _battleField.transform,
                    position = position
                }
            };

            // Set up button components
            var rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200f, 200f); // Adjust size as needed

            var button = buttonObject.AddComponent<Button>();

            // Use different handlers for enemy and player health icons
            if (isEnemyIcon)
                button.onClick.AddListener(() => _cardSelectionHandler.OnEnemyButtonClick(-1)); // Use -1 to indicate enemy health icon click
            else
                button.onClick.AddListener(() => _cardSelectionHandler.OnPlayerHealthIconClick());

            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(1, 1, 1, 0);
            buttonImage.raycastTarget = true;
        }

        private void CreatePlayerButton(int index)
        {
            GameObject playerEntity = _spritePositioning.PlayerEntities[index];

            // Disable raycast on placeholder image
            if (playerEntity.TryGetComponent(out Image placeholderImage))
            {
                placeholderImage.raycastTarget = false;
            }

            // Create button child object
            GameObject buttonObject = new GameObject($"Button_Outline_{index}")
            {
                transform =
                {
                    parent = playerEntity.transform,
                    localPosition = Vector3.zero
                }
            };

            // Set up button components
            var rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = _buttonSize;

            var button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => _cardSelectionHandler.OnPlayerButtonClick(index));

            // Add transparent image (required for button functionality)
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(1, 1, 1, 0);
        }

        private void CreateEnemyButton(int index)
        {
            GameObject enemyEntity = _spritePositioning.EnemyEntities[index];

            // Disable raycast on placeholder image if it exists
            if (enemyEntity.TryGetComponent(out Image placeholderImage))
            {
                placeholderImage.raycastTarget = false;
            }

            // Create button as child of enemy entity
            GameObject buttonObject = new GameObject($"Enemy_Button_Outline_{index}")
            {
                transform =
                {
                    parent = enemyEntity.transform,
                    localPosition = Vector3.zero
                }
            };

            // Set up button components
            var rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = _enemyButtonSize;

            var button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => _cardSelectionHandler.OnEnemyButtonClick(index));

            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(1, 1, 1, 0);
            buttonImage.raycastTarget = true;
        }
    }
}