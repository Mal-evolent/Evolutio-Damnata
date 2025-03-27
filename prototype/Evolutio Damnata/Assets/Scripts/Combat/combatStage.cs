using UnityEngine;
using TMPro;
using System.Collections;

public class CombatStage : MonoBehaviour, ICombatStage, IManaProvider
{
    [Header("UI References")]
    [SerializeField] private Sprite _wizardOutlineSprite;
    [SerializeField] private GameObject _manaBar;
    [SerializeField] private GameObject _manaText;
    [SerializeField] private Canvas _battleField;

    [Header("Dependencies")]
    [SerializeField] private CardManager _cardManagerComponent;
    [SerializeField] private CardOutlineManager _cardOutlineManagerComponent;
    [SerializeField] private CardLibrary _cardLibrary;
    [SerializeField] private CombatManager _combatManagerComponent;
    [SerializeField] private SpritePositioning _spritePositioningComponent;
    [SerializeField] private DamageVisualizer _damageVisualizer;
    [SerializeField] private GameObject _damageNumberPrefab;

    [Header("Mana Settings")]
    [SerializeField] private int _playerMana;
    [SerializeField] private int _enemyMana;

    // Interface references
    private ISpritePositioning _spritePositioning;
    private IAttackHandler _attackHandler;
    private ICardSpawner _playerCardSpawner;
    private ICardSpawner _enemyCardSpawner;
    private ISelectionEffectHandler _enemySelectionEffectHandler;
    private ISelectionEffectHandler _playerSelectionEffectHandler;
    private IButtonCreator _buttonCreator;
    private ICardSelectionHandler _cardSelectionHandler;
    private ICardManager _cardManager;
    private ICombatManager _combatManager;
    private ICardOutlineManager _cardOutlineManager;
    private OngoingEffectApplier _ongoingEffectApplier;

    // State
    public int PlayerMana { get => _playerMana; set => _playerMana = value; }
    public int EnemyMana { get; set; }
    private bool _buttonsInitialized = false;

    private void Awake()
    {
        // Initialize interface references
        _spritePositioning = _spritePositioningComponent;
        _cardManager = _cardManagerComponent;
        _combatManager = _combatManagerComponent;
        _cardOutlineManager = _cardOutlineManagerComponent;

        InitializeServices();
    }

    private void InitializeServices()
    {
        // 1. Initialize core combat systems
        var attackLimiter = new AttackLimiter();
        var spawnerFactory = new CardSpawnerFactory(
            _spritePositioning,
            _cardLibrary,
            this,
            _damageVisualizer,
            _damageNumberPrefab,
            _wizardOutlineSprite,
            this,
            attackLimiter
        );

        // 2. Create spawners
        _playerCardSpawner = spawnerFactory.CreatePlayerSpawner();  // ICardSpawner
        _enemyCardSpawner = spawnerFactory.CreateEnemySpawner();    // ICardSpawner
        _attackHandler = new AttackHandler(attackLimiter);          // IAttackHandler

        // 3. Initialize effect systems
        _ongoingEffectApplier = new OngoingEffectApplier();         // IEffectApplier
        var spellEffectApplier = new SpellEffectApplier(
            _cardManager,           // ICardManager
            _ongoingEffectApplier,  // IEffectApplier
            _damageVisualizer,      // Consider interface if needed
            _damageNumberPrefab     // GameObject
        );

        // 4. Create mana system
        var manaChecker = new ManaChecker(
            this,                   // IManaProvider (via CombatStage)
            _cardOutlineManager,    // ICardOutlineManager
            _cardManager            // ICardManager
        );

        // 5. Initialize card selection
        _cardSelectionHandler = new CardSelectionHandler();
        _cardSelectionHandler.Initialize(
            _cardManager,           // ICardManager
            _combatManager,         // ICombatManager
            _cardOutlineManager,    // ICardOutlineManager
            _spritePositioning,     // ISpritePositioning
            this,                   // ICombatStage
            _playerCardSpawner,     // ICardSpawner
            manaChecker,            // IManaChecker
            spellEffectApplier      // ISpellEffectApplier
        );

        // 6. Set up UI
        _buttonCreator = gameObject.AddComponent<ButtonCreator>();
        (_buttonCreator as ButtonCreator).Initialize(
            _battleField,           // Canvas
            _spritePositioning,     // ISpritePositioning
            _cardSelectionHandler   // ICardSelectionHandler
        );

        // 7. Initialize visual effects
        InitializeSelectionEffectHandlers();
    }

    private void InitializeSelectionEffectHandlers()
    {
        _playerSelectionEffectHandler = new PlayerSelectionEffectHandler(
            _spritePositioning,
            _cardManager,
            new Color(0.5f, 1f, 0.5f, 1f));

        _enemySelectionEffectHandler = new EnemySelectionEffectHandler(
            _spritePositioning,
            new Color(1f, 0.5f, 0.5f, 1f));
    }

    private void Start()
    {
        StartCoroutine(InitializeGameElements());
    }

    private IEnumerator InitializeGameElements()
    {
        yield return StartCoroutine(_spritePositioning.WaitForRoomSelection());
        yield return StartCoroutine(_spritePositioning.SetAllPlaceHoldersInactive());
        yield return StartCoroutine(InitializeInteractableHighlights());
    }

    private IEnumerator InitializeInteractableHighlights()
    {
        while (_spritePositioning.PlayerEntities.Count == 0)
            yield return null;

        InitializeButtons();
    }

    public void InitializeButtons()
    {
        if (_buttonsInitialized) return;

        _buttonCreator.AddButtonsToPlayerEntities();
        _buttonCreator.AddButtonsToEnemyEntities();
        _buttonsInitialized = true;
    }

    public void HandleMonsterAttack(EntityManager attacker, EntityManager target)
    {
        _attackHandler.HandleAttack(attacker, target);
    }

    public void SpawnEnemyCard(string cardName, int position)
    {
        _enemyCardSpawner.SpawnCard(cardName, position);
    }

    public void UpdateManaUI()
    {
        if (_manaBar == null || _manaText == null) return;

        var manaSlider = _manaBar.GetComponent<Slider>();
        manaSlider.maxValue = MaxMana;
        manaSlider.value = CurrentMana;
        _manaText.GetComponent<TMP_Text>().text = CurrentMana.ToString();
    }

    public void UpdatePlayerManaUI()
    {
        // Your existing mana UI update logic
        if (_manaBar == null || _manaText == null) return;

        var manaSlider = _manaBar.GetComponent<Slider>();
        manaSlider.maxValue = MaxMana;
        manaSlider.value = PlayerMana;
        _manaText.GetComponent<TMP_Text>().text = PlayerMana.ToString();
    }

    private void Update()
    {
        UpdateSelectionEffects();
        UpdatePlaceholderVisibility();
    }

    private void UpdateSelectionEffects()
    {
        bool hasSelectedCard = _cardManager.CurrentSelectedCard != null;
        _enemySelectionEffectHandler.ApplyEffect(hasSelectedCard);

        if (hasSelectedCard && IsPlacedCardSelected())
        {
            _playerSelectionEffectHandler.ApplyEffect();
        }
    }

    private void UpdatePlaceholderVisibility()
    {
        bool shouldShowPlaceholders = _cardManager.CurrentSelectedCard == null ||
                                    !IsPlacedCardSelected();

        SetPlaceholderActiveState(shouldShowPlaceholders);
    }

    private bool IsPlacedCardSelected()
    {
        return _cardManager.CurrentSelectedCard?.GetComponent<EntityManager>()?.placed ?? false;
    }

    public void SetPlaceholderActiveState(bool active)
    {
        StartCoroutine(_spritePositioning.SetPlaceholderActiveState(active));
    }
}
