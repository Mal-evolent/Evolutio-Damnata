using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

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
        private CombatManager _combatManager;

        [Header("Button Settings")]
        private readonly Vector2 _buttonSize = new Vector2(114.2145f, 104.5f);
        private readonly Vector2 _enemyButtonSize = new Vector2(114.2145f, 104.5f);
        private readonly Vector2 _healthIconButtonSize = new Vector2(200f, 200f);

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;
        private bool _healthButtonsCreated = false;

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

            // Find the CombatManager reference
            _combatManager = FindObjectOfType<CombatManager>();

            // Subscribe to phase changes to know when combat is active
            if (_combatManager != null)
            {
                _combatManager.SubscribeToPhaseChanges(OnCombatPhaseChanged);
            }
            else
            {
                Debug.LogWarning("[ButtonCreator] Could not find CombatManager");
            }
        }

        private void Start()
        {
            // Delay button creation to ensure UI hierarchy is fully loaded
            StartCoroutine(DelayedInitialization());
        }

        private IEnumerator DelayedInitialization()
        {
            // Wait for two frames to ensure UI is initialized
            yield return null;
            yield return null;

            // Force check if we already have a combat phase
            if (_combatManager != null && _combatManager.CurrentPhase != CombatPhase.None)
            {
                AddButtonsToHealthIcons();
            }
        }

        private void OnDestroy()
        {
            // Clean up subscription when this object is destroyed
            if (_combatManager != null)
            {
                _combatManager.UnsubscribeFromPhaseChanges(OnCombatPhaseChanged);
            }
        }

        private void OnCombatPhaseChanged(CombatPhase phase)
        {
            // When combat begins (any phase except None), add buttons to health icons
            if (phase != CombatPhase.None)
            {
                // Add a slight delay to ensure UI container is active
                StartCoroutine(DelayedAddButtonsToHealthIcons());
            }
        }

        private IEnumerator DelayedAddButtonsToHealthIcons()
        {
            // Small delay to ensure UI has been activated
            yield return new WaitForSeconds(0.2f);
            AddButtonsToHealthIcons();

            // Make a second attempt if first one failed (containers might not be found)
            if (!_healthButtonsCreated)
            {
                yield return new WaitForSeconds(0.5f);
                TryFindAlternativeContainerPaths();
            }
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

        // Updated method with more logging and fallback paths
        public void AddButtonsToHealthIcons()
        {
            Debug.Log("[ButtonCreator] Adding buttons to health icons");
            _healthButtonsCreated = false;

            if (_battleField == null)
            {
                Debug.LogError("[ButtonCreator] Battlefield canvas is null, trying to find it");
                _battleField = FindObjectOfType<Canvas>();
                if (_battleField == null)
                {
                    Debug.LogError("[ButtonCreator] Could not find any Canvas!");
                    return;
                }
            }

            // Check if the UI container object exists and is active
            GameObject uiContainer = GameObject.Find("Combat UI Container");
            if (uiContainer == null)
            {
                if (_debugMode)
                {
                    Debug.LogWarning("[ButtonCreator] Could not find 'Combat UI Container' by name");

                    // List all root game objects for debugging
                    Debug.LogWarning("[ButtonCreator] Listing scene root objects:");
                    foreach (var rootObj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        Debug.LogWarning($"Root object: {rootObj.name}");
                    }

                    // List all Canvas children for debugging
                    Debug.LogWarning("[ButtonCreator] Listing Canvas children:");
                    foreach (Transform child in _battleField.transform)
                    {
                        Debug.LogWarning($"Canvas child: {child.name}");
                    }
                }

                // Try different paths for Combat UI Container
                Transform combatUIContainer = _battleField.transform.Find("Combat UI Container");
                if (combatUIContainer == null)
                {
                    combatUIContainer = _battleField.transform.Find("CombatUI Container");
                }

                if (combatUIContainer == null)
                {
                    Debug.LogError("[ButtonCreator] Combat UI Container not found under Canvas! Trying to find health icons by tag");
                    TryCreateButtonsByTag();
                    return;
                }
                else
                {
                    uiContainer = combatUIContainer.gameObject;
                }
            }

            // Find health icon containers in the UI hierarchy
            Transform playerIconContainer = uiContainer.transform.Find("Player Icon");
            Transform enemyIconContainer = uiContainer.transform.Find("Enemy Icon");

            int buttonsCreated = 0;

            // Process player icon container
            if (playerIconContainer != null)
            {
                buttonsCreated += ProcessIconContainer(playerIconContainer, "Player_Health_Button", false);
            }
            else
            {
                Debug.LogError("[ButtonCreator] Player Icon container not found!");
            }

            // Process enemy icon container
            if (enemyIconContainer != null)
            {
                buttonsCreated += ProcessIconContainer(enemyIconContainer, "Enemy_Health_Button", true);
            }
            else
            {
                Debug.LogError("[ButtonCreator] Enemy Icon container not found!");
            }

            _healthButtonsCreated = buttonsCreated > 0;

            if (_healthButtonsCreated)
            {
                Debug.Log($"[ButtonCreator] Successfully created {buttonsCreated} health icon buttons");
            }
            else
            {
                Debug.LogError("[ButtonCreator] No health icon buttons were created!");
            }
        }

        private int ProcessIconContainer(Transform container, string buttonName, bool isEnemyIcon)
        {
            int buttonsCreated = 0;
            bool foundHealthIcon = false;

            // First try to find a direct HealthIconManager component
            HealthIconManager healthIconManager = container.GetComponent<HealthIconManager>();
            if (healthIconManager != null)
            {
                CreateHealthIconButton(container.gameObject, buttonName, container.position, isEnemyIcon);
                buttonsCreated++;
                foundHealthIcon = true;
            }
            else
            {
                // Then check all children
                foreach (Transform child in container)
                {
                    healthIconManager = child.GetComponent<HealthIconManager>();
                    if (healthIconManager != null)
                    {
                        CreateHealthIconButton(child.gameObject, buttonName, child.position, isEnemyIcon);
                        buttonsCreated++;
                        foundHealthIcon = true;
                        break;
                    }

                    // If we don't find it directly, search recursively
                    if (!foundHealthIcon)
                    {
                        foreach (Transform grandchild in child)
                        {
                            healthIconManager = grandchild.GetComponent<HealthIconManager>();
                            if (healthIconManager != null)
                            {
                                CreateHealthIconButton(grandchild.gameObject, buttonName, grandchild.position, isEnemyIcon);
                                buttonsCreated++;
                                foundHealthIcon = true;
                                break;
                            }
                        }
                    }
                }
            }

            return buttonsCreated;
        }

        private void TryFindAlternativeContainerPaths()
        {
            Debug.Log("[ButtonCreator] Attempting to find health icons through alternative paths");

            // List of potential container paths to check
            string[] playerPaths = new string[] {
                    "CombatUIContainer/Player Icon",
                    "CombatUIContainer/PlayerIcon",
                    "CombatUI Container/PlayerIcon",
                    "UI Container/Player Icon",
                    "UIContainer/PlayerIcon"
                };

            string[] enemyPaths = new string[] {
                    "CombatUIContainer/Enemy Icon",
                    "CombatUIContainer/EnemyIcon",
                    "CombatUI Container/EnemyIcon",
                    "UI Container/Enemy Icon",
                    "UIContainer/EnemyIcon"
                };

            bool foundAny = false;

            // Try all player paths
            foreach (string path in playerPaths)
            {
                Transform container = _battleField.transform.Find(path);
                if (container != null)
                {
                    Debug.Log($"[ButtonCreator] Found alternative player path: {path}");
                    ProcessIconContainer(container, "Player_Health_Button", false);
                    foundAny = true;
                    break;
                }
            }

            // Try all enemy paths
            foreach (string path in enemyPaths)
            {
                Transform container = _battleField.transform.Find(path);
                if (container != null)
                {
                    Debug.Log($"[ButtonCreator] Found alternative enemy path: {path}");
                    ProcessIconContainer(container, "Enemy_Health_Button", true);
                    foundAny = true;
                    break;
                }
            }

            if (!foundAny)
            {
                Debug.LogError("[ButtonCreator] No alternative paths found, falling back to tag-based search");
                TryCreateButtonsByTag();
            }
        }

        private void TryCreateButtonsByTag()
        {
            // Last resort - find by tags
            Debug.Log("[ButtonCreator] Attempting to find health icons by tag");

            GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
            GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag("Enemy");

            int buttonsCreated = 0;

            foreach (GameObject player in playerObjects)
            {
                HealthIconManager healthIcon = player.GetComponent<HealthIconManager>();
                if (healthIcon != null)
                {
                    CreateHealthIconButton(player, "Player_Health_Button", player.transform.position, false);
                    buttonsCreated++;
                    Debug.Log("[ButtonCreator] Created player button by tag");
                }
            }

            foreach (GameObject enemy in enemyObjects)
            {
                HealthIconManager healthIcon = enemy.GetComponent<HealthIconManager>();
                if (healthIcon != null)
                {
                    CreateHealthIconButton(enemy, "Enemy_Health_Button", enemy.transform.position, true);
                    buttonsCreated++;
                    Debug.Log("[ButtonCreator] Created enemy button by tag");
                }
            }

            _healthButtonsCreated = buttonsCreated > 0;
        }

        private void CreateHealthIconButton(GameObject icon, string buttonName, Vector3 position, bool isEnemyIcon)
        {
            // Check if button already exists
            Transform existingButton = icon.transform.Find(buttonName);
            if (existingButton != null)
            {
                Debug.Log($"[ButtonCreator] {buttonName} already exists, skipping creation");
                return;
            }

            // Create button as child of the health icon
            GameObject buttonObject = new GameObject(buttonName)
            {
                transform =
                        {
                            parent = icon.transform,
                            localPosition = Vector3.zero
                        }
            };

            // Set up button components
            var rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = _healthIconButtonSize;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            var button = buttonObject.AddComponent<Button>();

            // Use different handlers for enemy and player health icons
            if (isEnemyIcon)
                button.onClick.AddListener(() => _cardSelectionHandler.OnEnemyButtonClick(-1)); // Use -1 to indicate enemy health icon click
            else
                button.onClick.AddListener(() => _cardSelectionHandler.OnPlayerHealthIconClick());

            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(1, 1, 1, 0); // Transparent
            buttonImage.raycastTarget = true;

            Debug.Log($"[ButtonCreator] Created {buttonName} for {(isEnemyIcon ? "enemy" : "player")} health icon");
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