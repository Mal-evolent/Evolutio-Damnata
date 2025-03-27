using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class CombatManager : MonoBehaviour, ICombatManager, IManaProvider
{
    [Header("References")]
    [SerializeField] private CombatStage _combatStage;
    [SerializeField] private TMP_Text _turnUI;
    [SerializeField] private Button _endPhaseButton;
    [SerializeField] private Button _endTurnButton;
    [SerializeField] private Deck _playerDeck;
    [SerializeField] private Deck _enemyDeck;

    [Header("Mana UI References")]
    [SerializeField] private Slider _manaSlider;
    [SerializeField] private TMP_Text _manaText;
    [SerializeField] private TMP_Text _playerManaText;

    [Header("Game State")]
    [SerializeField] private int _turnCount = 0;
    [SerializeField] private int _playerMana = 0;
    [SerializeField] private int _enemyMana = 0;
    [SerializeField] private int _maxMana = 10;
    [SerializeField] private int _playerHealth = 30;
    [SerializeField] private int _enemyHealth = 30;
    [SerializeField] private bool _playerGoesFirst = true;
    [SerializeField] private bool _playerTurn;
    [SerializeField] private CombatPhase _currentPhase = CombatPhase.None;

    private IRoundManager _roundManager;
    private IPhaseManager _phaseManager;
    private IPlayerActions _playerActions;
    private IEnemyActions _enemyActions;
    private IUIManager _uiManager;

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
    public Button EndTurnButton => _endTurnButton;
    public TMP_Text TurnUI => _turnUI;

    // IManaProvider implementation
    public int PlayerMana
    {
        get => _playerMana;
        set
        {
            _playerMana = Mathf.Clamp(value, 0, MaxMana);
            UpdateAllManaUI();
        }
    }

    public int EnemyMana { get => _enemyMana; set => _enemyMana = value; }
    public int MaxMana => _maxMana;

    private void Awake()
    {
        // Initialize dependencies
        _uiManager = new UIManager(this);
        _playerActions = new PlayerActions(this);

        _enemyActions = new EnemyActions(
            this,
            _combatStage.SpritePositioning,
            _enemyDeck,
            _combatStage.CardLibrary,
            _combatStage
        );

        var attackLimiter = new AttackLimiter();

        // Create RoundManager first with null PhaseManager
        _roundManager = new RoundManager(this, null, _enemyActions, _uiManager);

        // Now create PhaseManager with all dependencies
        _phaseManager = new PhaseManager(
            combatManager: this,
            attackLimiter: attackLimiter,
            uiManager: _uiManager,
            enemyActions: _enemyActions,
            playerActions: _playerActions,
            roundManager: _roundManager
        );

        // Update RoundManager with actual PhaseManager
        _roundManager = new RoundManager(this, _phaseManager, _enemyActions, _uiManager);
    }

    private void Start()
    {
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

    // IManaProvider implementation
    public void UpdateManaUI()
    {
        if (_manaSlider != null)
        {
            _manaSlider.value = PlayerMana;
        }

        if (_manaText != null)
        {
            _manaText.text = $"{PlayerMana}/{MaxMana}";
        }
    }

    public void UpdatePlayerManaUI()
    {
        if (_playerManaText != null)
        {
            _playerManaText.text = $"Mana: {PlayerMana}";
        }
    }

    private void UpdateAllManaUI()
    {
        UpdateManaUI();
        UpdatePlayerManaUI();
    }

    // Gameplay methods
    public void EndPhase()
    {
        _uiManager.SetButtonState(_endPhaseButton, false);
        _phaseManager.EndPhase();
    }

    public void EndTurn()
    {
        _uiManager.SetButtonState(_endTurnButton, false);
        _playerActions.EndTurn();
    }

    public void ResetPhaseState()
    {
        _currentPhase = CombatPhase.None;
    }

    // Explicit interface implementations for phase checks
    bool ICombatManager.IsPlayerPrepPhase() => _currentPhase == CombatPhase.PlayerPrep;
    bool ICombatManager.IsPlayerCombatPhase() => _currentPhase == CombatPhase.PlayerCombat;
    bool ICombatManager.IsEnemyPrepPhase() => _currentPhase == CombatPhase.EnemyPrep;
    bool ICombatManager.IsEnemyCombatPhase() => _currentPhase == CombatPhase.EnemyCombat;
    bool ICombatManager.IsCleanUpPhase() => _currentPhase == CombatPhase.CleanUp;

    // Unity method implementations
    public Coroutine StartCoroutine(IEnumerator routine) => base.StartCoroutine(routine);
    public T GetComponent<T>() where T : Component => base.GetComponent<T>();
}