using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System;
using EnemyInteraction;
using Combat.UI;
using GeneralInteraction;
using GameManagement;

public class CombatManager : MonoBehaviour, ICombatManager, IManaProvider
{
    [Header("References")]
    [SerializeField] private CombatStage _combatStage;
    [SerializeField] private TMP_Text _turnUI;
    [SerializeField] private TMP_Text _turnUIShadow;
    [SerializeField] private Button _endPhaseButton;
    [SerializeField] private Image _endPhaseButtonShadow;
    [SerializeField] private Button _endTurnButton;
    [SerializeField] private Image _endTurnButtonShadow;
    [SerializeField] private Deck _playerDeck;
    [SerializeField] private Deck _enemyDeck;

    [Header("Mana UI References")]
    [SerializeField] private Slider _manaSlider;
    [SerializeField] private TMP_Text _manaText;
    [SerializeField] private GameObject _uIContainerObject;

    [Header("Game State")]
    [SerializeField] private int _turnCount = 0;
    [SerializeField] private int _playerHealth;
    [SerializeField] private Slider _playerHealthSlider;
    [SerializeField] private int _enemyHealth;
    [SerializeField] private Slider _enemyHealthSlider;
    [SerializeField] private bool _playerGoesFirst = true;
    [SerializeField] private bool _playerTurn;
    [SerializeField] private CombatPhase _currentPhase = CombatPhase.None;
    [SerializeField] private int _maxPlayerHealth;
    [SerializeField] private int _maxEnemyHealth;

    // Mana fields - now the single source of truth
    [Header("Mana Settings")]
    [SerializeField] private int _playerMana = 0;
    [SerializeField] private int _enemyMana = 0;
    [SerializeField] private int _maxMana = 0;

    private IRoundManager _roundManager;
    private IPhaseManager _phaseManager;
    private IPlayerActions _playerActions;
    private IEnemyActions _enemyActions;
    private IUIManager _uiManager;
    private CombatUIVisibilityManager _uiVisibilityManager;

    private bool _isInitialized = false;

    // Event for phase changes
    public event Action<CombatPhase> OnPhaseChanged;

    // Event for combat start
    public event Action OnCombatStart;

    // New event for enemy defeat
    public event Action OnEnemyDefeated;

    // Properties
    public int TurnCount
    {
        get => _turnCount;
        set => _turnCount = value;
    }

    public int PlayerHealth
    {
        get => _playerHealth;
        set
        {
            _playerHealth = value;
            if (_playerHealthSlider != null)
            {
                // Normalize the health value based on max health
                _playerHealthSlider.value = (float)value / _maxPlayerHealth;
            }
        }
    }

    public int EnemyHealth
    {
        get => _enemyHealth;
        set
        {
            int previousHealth = _enemyHealth;
            _enemyHealth = value;
            if (_enemyHealthSlider != null)
            {
                // Normalize the health value based on max health
                _enemyHealthSlider.value = (float)value / _maxEnemyHealth;
            }

            // Check if enemy was just defeated
            if (previousHealth > 0 && value <= 0)
            {
                Debug.Log("[CombatManager] Enemy defeated!");
                OnEnemyDefeated?.Invoke();
            }
        }
    }

    public bool PlayerTurn { get => _playerTurn; set => _playerTurn = value; }
    public bool PlayerGoesFirst { get => _playerGoesFirst; set => _playerGoesFirst = value; }

    // Modified CurrentPhase property that invokes the event
    public CombatPhase CurrentPhase
    {
        get => _currentPhase;
        set
        {
            if (_currentPhase != value)
            {
                Debug.Log($"[CombatManager] Phase changing: {_currentPhase} -> {value}");
                _currentPhase = value;
                OnPhaseChanged?.Invoke(_currentPhase);
            }
        }
    }

    public CombatStage CombatStage => _combatStage;
    public Deck PlayerDeck => _playerDeck;
    public Deck EnemyDeck => _enemyDeck;
    public Button EndPhaseButton => _endPhaseButton;
    public Image EndPhaseButtonShadow => _endPhaseButtonShadow;
    public Button EndTurnButton => _endTurnButton;
    public Image EndTurnButtonShadow => _endPhaseButtonShadow;
    public TMP_Text TurnUI => _turnUI;
    public TMP_Text TurnUIShadow => _turnUIShadow;
    public Slider PlayerHealthSlider => _playerHealthSlider;
    public Slider EnemyHealthSlider => _enemyHealthSlider;
    public int PlayerMaxHealth => _maxPlayerHealth;
    public int EnemyMaxHealth => _maxEnemyHealth;
    public int PlayerHandSize => _playerDeck != null ? _playerDeck.HandSize : 0;
    public int EnemyHandSize => _enemyDeck != null ? _enemyDeck.HandSize : 0;

    // Changed to full property syntax to ensure proper accessibility
    public Slider ManaSlider { get => _manaSlider; }
    public TMP_Text ManaText { get => _manaText; }
    public GameObject UIContainerObject { get => _uIContainerObject; }

    public int PlayerMana
    {
        get => _playerMana;
        set
        {
            _playerMana = Mathf.Clamp(value, 0, MaxMana);
            UpdateAllManaUI();
        }
    }

    public int EnemyMana
    {
        get => _enemyMana;
        set
        {
            _enemyMana = Mathf.Clamp(value, 0, MaxMana);
            UpdateAllManaUI();
        }
    }

    public int MaxMana
    {
        get => _maxMana;
        set
        {
            _maxMana = Mathf.Max(1, value);
            PlayerMana = Mathf.Min(PlayerMana, _maxMana);
            EnemyMana = Mathf.Min(EnemyMana, _maxMana);
            UpdateAllManaUI();
        }
    }

    public void SubscribeToPhaseChanges(Action<CombatPhase> callback)
    {
        OnPhaseChanged += callback;
    }

    public void UnsubscribeFromPhaseChanges(Action<CombatPhase> callback)
    {
        OnPhaseChanged -= callback;
    }

    private IEnumerator InitializeManagers()
    {
        Debug.Log("[CombatManager] Starting manager initialization...");

        // Wait for CombatStage to be fully initialized
        while (_combatStage == null || _combatStage.SpritePositioning == null || _combatStage.SpellEffectApplier == null)
        {
            Debug.Log("[CombatManager] Waiting for CombatStage to be fully initialized...");
            yield return null;
        }

        // Create EnemyActions only after CombatStage is ready
        var enemyActionsObj = GameObject.Find("EnemyActions");
        if (enemyActionsObj == null)
        {
            enemyActionsObj = new GameObject("EnemyActions");
            _enemyActions = enemyActionsObj.AddComponent<EnemyActions>();
        }
        else
        {
            _enemyActions = enemyActionsObj.GetComponent<EnemyActions>();
            if (_enemyActions == null)
            {
                _enemyActions = enemyActionsObj.AddComponent<EnemyActions>();
            }
        }

        // Wait for EnemyActions to be fully initialized
        while (_enemyActions == null || !((_enemyActions as MonoBehaviour)?.enabled ?? false))
        {
            Debug.Log("[CombatManager] Waiting for EnemyActions to be fully initialized...");
            yield return null;
        }

        var attackLimiter = new AttackLimiter();

        Debug.Log("[CombatManager] Creating RoundManager...");
        // Create RoundManager first with minimal dependencies
        var roundManagerImpl = new RoundManager(
            combatManager: this,
            enemyActions: _enemyActions,
            uiManager: _uiManager
        );
        _roundManager = roundManagerImpl;

        Debug.Log("[CombatManager] Creating PhaseManager...");
        // Create PhaseManager with all dependencies
        _phaseManager = new PhaseManager(
            combatManager: this,
            attackLimiter: attackLimiter,
            uiManager: _uiManager,
            enemyActions: _enemyActions,
            playerActions: _playerActions,
            roundManager: _roundManager,
            prepPhaseImage: _combatStage.PrepPhaseImage,
            combatPhaseImage: _combatStage.CombatPhaseImage,
            cleanupPhaseImage: _combatStage.CleanupPhaseImage
        );

        Debug.Log("[CombatManager] Setting PhaseManager in RoundManager...");
        // Complete the dependency chain
        roundManagerImpl.SetPhaseManager(_phaseManager);

        Debug.Log("[CombatManager] All managers initialized successfully");
        _isInitialized = true;
    }

    private void Awake()
    {
        try
        {
            Debug.Log("[CombatManager] Initializing dependencies...");

            // Initialize core dependencies
            _uiManager = new UIManager(this);
            _playerActions = new PlayerActions(this);

            // Find or create CombatUIVisibilityManager
            _uiVisibilityManager = GetComponent<CombatUIVisibilityManager>();
            if (_uiVisibilityManager == null)
            {
                _uiVisibilityManager = gameObject.AddComponent<CombatUIVisibilityManager>();
            }
            _uiVisibilityManager.Initialize(this);

            // Validate combat stage before creating EnemyActions
            if (_combatStage == null)
            {
                throw new NullReferenceException("CombatStage reference is not set in inspector");
            }

            // Start initialization coroutine
            StartCoroutine(InitializeManagers());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CombatManager] Initialization failed: {ex.Message}");
            Debug.LogException(ex);
            enabled = false; // Disable the component to prevent further errors
        }
    }

    private void Start()
    {
        // Remove automatic initialization - it will be triggered by CombatTrigger instead
        Debug.Log("[CombatManager] Ready for combat initialization");
    }

    public IEnumerator WaitForInitialization()
    {
        Debug.Log("[CombatManager] Starting combat initialization...");

        // Reset initialization state
        _isInitialized = false;

        // Start the manager initialization chain
        StartCoroutine(InitializeManagers());

        // Wait for initialization to complete
        while (!_isInitialized)
        {
            yield return null;
        }

        InitializeManaUI();

        // Set UI visibility here AFTER initialization is complete
        combatUIVisibility(true);

        _roundManager.InitializeGame();

        Debug.Log("[CombatManager] Combat initialization complete");
    }

    public void InitializeManaUI()
    {
        if (_manaSlider != null)
        {
            _manaSlider.minValue = 0;
            _manaSlider.maxValue = MaxMana;
            _manaSlider.value = PlayerMana;
        }
        UpdateAllManaUI();
    }

    public void combatUIVisibility(bool isVisible)
    {
        _uiVisibilityManager.SetCombatUIVisibility(isVisible);
    }

    // Consolidated mana UI updates
    public void UpdateManaUI()
    {
        if (_manaSlider != null)
        {
            _manaSlider.value = PlayerMana;
        }

        if (_manaText != null)
        {
            _manaText.text = $"{PlayerMana}";
        }
    }

    private void UpdateAllManaUI()
    {
        UpdateManaUI();
        _combatStage.UpdateManaUI();
    }

    private void DeselectEverything()
    {
        if (_combatStage == null) return;

        var cardManager = _combatStage.GetComponent<CardManager>();
        var cardOutlineManager = _combatStage.GetComponent<CardOutlineManager>();

        if (cardManager != null)
        {
            cardManager.CurrentSelectedCard = null;
        }
        if (cardOutlineManager != null)
        {
            cardOutlineManager.RemoveHighlight();
        }
    }

    public void EndPhase()
    {
        _uiManager.SetButtonState(_endPhaseButton, false);
        DeselectEverything();
        _phaseManager.EndPhase();
    }

    public void EndTurn()
    {
        _uiManager.SetButtonState(_endTurnButton, false);
        DeselectEverything();
        _playerActions.EndTurn();
    }

    public void ResetPhaseState()
    {
        // Use the property to ensure event firing
        CurrentPhase = CombatPhase.None;
    }

    public void TriggerCombatStart()
    {
        OnCombatStart?.Invoke();
    }

    public void StartNextRound()
    {
        StartCoroutine(_roundManager.RoundStart());
    }

    // Phase checks
    bool ICombatManager.IsPlayerPrepPhase() => _currentPhase == CombatPhase.PlayerPrep;
    bool ICombatManager.IsPlayerCombatPhase() => _currentPhase == CombatPhase.PlayerCombat;
    bool ICombatManager.IsEnemyPrepPhase() => _currentPhase == CombatPhase.EnemyPrep;
    bool ICombatManager.IsEnemyCombatPhase() => _currentPhase == CombatPhase.EnemyCombat;
    bool ICombatManager.IsCleanUpPhase() => _currentPhase == CombatPhase.CleanUp;

    // Debug method to show card history
    public void ShowCardHistory()
    {
        if (CardHistory.Instance != null)
        {
            CardHistory.Instance.LogAllCardHistory();
        }
        else
        {
            Debug.LogError("[CombatManager] CardHistory.Instance is null!");
        }
    }
}
