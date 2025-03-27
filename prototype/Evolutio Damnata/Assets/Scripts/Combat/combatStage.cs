using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

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
    [SerializeField] private int _maxMana = 0;

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

    // IManaProvider implementation
    public int PlayerMana
    {
        get => _playerMana;
        set
        {
            _playerMana = Mathf.Clamp(value, 0, _maxMana);
            UpdateManaUI();
        }
    }

    public int EnemyMana
    {
        get => _enemyMana;
        set => _enemyMana = Mathf.Clamp(value, 0, _maxMana);
    }

    public int MaxMana
    {
        get => _maxMana;
        set
        {
            _maxMana = Mathf.Max(1, value);
            PlayerMana = Mathf.Min(PlayerMana, _maxMana);
            EnemyMana = Mathf.Min(EnemyMana, _maxMana);
            UpdateManaUI();
        }
    }

    // Expose dependencies through properties
    public CardLibrary CardLibrary => _cardLibrary;
    public ISpritePositioning SpritePositioning => _spritePositioning;
    public ICardSpawner EnemyCardSpawner => _enemyCardSpawner;

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
        var attackLimiter = new AttackLimiter();
        var spawnerFactory = new CardSpawnerFactory(
            _spritePositioning,
            _cardLibrary,
            this, // IManaProvider
            _damageVisualizer,
            _damageNumberPrefab,
            _wizardOutlineSprite,
            this, // ICombatStage
            attackLimiter
        );

        _playerCardSpawner = spawnerFactory.CreatePlayerSpawner();
        _enemyCardSpawner = spawnerFactory.CreateEnemySpawner();
        _attackHandler = new AttackHandler(attackLimiter);

        _ongoingEffectApplier = new OngoingEffectApplier();
        var spellEffectApplier = new SpellEffectApplier(
            _cardManager,
            _ongoingEffectApplier,
            _damageVisualizer,
            _damageNumberPrefab
        );

        var manaChecker = new ManaChecker(
            this, // IManaProvider
            _cardOutlineManager,
            _cardManager
        );

        _cardSelectionHandler = gameObject.AddComponent<CardSelectionHandler>();
        _cardSelectionHandler.Initialize(
            _cardManager,
            _combatManager,
            _cardOutlineManager,
            _spritePositioning,
            this, // ICombatStage
            _playerCardSpawner,
            manaChecker,
            spellEffectApplier
        );

        _buttonCreator = gameObject.AddComponent<ButtonCreator>();
        (_buttonCreator as ButtonCreator).Initialize(
            _battleField,
            _spritePositioning,
            _cardSelectionHandler
        );

        InitializeSelectionEffectHandlers();
    }

    // IManaProvider implementation
    public void UpdateManaUI()
    {
        if (_manaBar == null || _manaText == null) return;

        var manaSlider = _manaBar.GetComponent<Slider>();
        manaSlider.maxValue = MaxMana;
        manaSlider.value = PlayerMana;
        _manaText.GetComponent<TMP_Text>().text = PlayerMana.ToString();
    }

    public void UpdatePlayerManaUI() => UpdateManaUI();

    // Rest of the CombatStage implementation remains the same...
    // [Previous methods like InitializeSelectionEffectHandlers, HandleMonsterAttack, etc.]

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
