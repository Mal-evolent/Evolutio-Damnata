using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System;

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

    [Header("Game State")]
    [SerializeField] private int _turnCount = 0;
    [SerializeField] private int _playerHealth = 30;
    [SerializeField] private Slider _playerHealthSlider;
    [SerializeField] private int _enemyHealth = 30;
    [SerializeField] private Slider _enemyHealthSlider;
    [SerializeField] private bool _playerGoesFirst = true;
    [SerializeField] private bool _playerTurn;
    [SerializeField] private CombatPhase _currentPhase = CombatPhase.None;

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

    private bool _isInitialized = false;

    // Properties
    public int TurnCount { get => _turnCount; set => _turnCount = value; }
    public int PlayerHealth { get => _playerHealth; set => _playerHealth = value; }
    public int EnemyHealth { get => _enemyHealth; set => _enemyHealth = value; }
    public bool PlayerTurn { get => _playerTurn; set => _playerTurn = value; }
    public bool PlayerGoesFirst { get => _playerGoesFirst; set => _playerGoesFirst = value; }
    public CombatPhase CurrentPhase { get => _currentPhase; set => _currentPhase = value; }
    public CombatStage CombatStage => _combatStage;
    public Deck PlayerDeck => _playerDeck;
    public Deck EnemyDeck => _enemyDeck;
    public Button EndPhaseButton => _endPhaseButton;
    public Image EndPhaseButtonShadow => _endPhaseButtonShadow;
    public Button EndTurnButton => _endTurnButton;
    public Image EndTurnButtonShadow => _endPhaseButtonShadow;
    public TMP_Text TurnUI => _turnUI;
    public TMP_Text TurnUIShadow => _turnUIShadow;

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

    private void Awake()
    {
        try
        {
            Debug.Log("[CombatManager] Initializing dependencies...");

            // Initialize core dependencies
            _uiManager = new UIManager(this);
            _playerActions = new PlayerActions(this);

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

    private IEnumerator InitializeManagers()
    {
        // Wait for CombatStage to be fully initialized
        while (_combatStage.SpritePositioning == null)
        {
            yield return null;
        }

        // Wait for SpritePositioning to be ready
        while (!_combatStage.SpritePositioning.RoomReady)
        {
            yield return null;
        }

        _enemyActions = new EnemyActions(
            this,
            _combatStage.SpritePositioning as SpritePositioning,
            _enemyDeck,
            _combatStage.CardLibrary,
            _combatStage
        );

        var attackLimiter = new AttackLimiter();

        Debug.Log("[CombatManager] Creating RoundManager (PhaseManager will be set later)...");
        // Create RoundManager first with minimal dependencies
        var roundManagerImpl = new RoundManager(
            combatManager: this,
            enemyActions: _enemyActions,
            uiManager: _uiManager
        );
        _roundManager = roundManagerImpl;

        Debug.Log("[CombatManager] Creating PhaseManager with all dependencies...");
        //create PhaseManager with all dependencies
        _phaseManager = new PhaseManager(
            combatManager: this,
            attackLimiter: attackLimiter,
            uiManager: _uiManager,
            enemyActions: _enemyActions,
            playerActions: _playerActions,
            roundManager: _roundManager
        );

        Debug.Log("[CombatManager] Setting PhaseManager in RoundManager...");
        // Complete the dependency chain
        roundManagerImpl.SetPhaseManager(_phaseManager);

        Debug.Log("[CombatManager] All managers initialized successfully");
        _isInitialized = true;
    }

    private void Start()
    {
        StartCoroutine(WaitForInitialization());
    }

    private IEnumerator WaitForInitialization()
    {
        while (!_isInitialized)
        {
            yield return null;
        }

        InitializeManaUI();
        _roundManager.InitializeGame();
    }

    private void InitializeManaUI()
    {
        if (_manaSlider != null)
        {
            _manaSlider.minValue = 0;
            _manaSlider.maxValue = MaxMana;
            _manaSlider.value = PlayerMana;
        }
        UpdateAllManaUI();
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
        _currentPhase = CombatPhase.None;
    }

    // Phase checks
    bool ICombatManager.IsPlayerPrepPhase() => _currentPhase == CombatPhase.PlayerPrep;
    bool ICombatManager.IsPlayerCombatPhase() => _currentPhase == CombatPhase.PlayerCombat;
    bool ICombatManager.IsEnemyPrepPhase() => _currentPhase == CombatPhase.EnemyPrep;
    bool ICombatManager.IsEnemyCombatPhase() => _currentPhase == CombatPhase.EnemyCombat;
    bool ICombatManager.IsCleanUpPhase() => _currentPhase == CombatPhase.CleanUp;
}