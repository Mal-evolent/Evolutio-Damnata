using UnityEngine;
using TMPro;
using System.Collections;

public class CombatStage : MonoBehaviour, ICombatStage
{
    [Header("UI References")]
    [SerializeField] private Sprite _wizardOutlineSprite;
    [SerializeField] private GameObject _manaBar;
    [SerializeField] private GameObject _manaText;
    [SerializeField] private Canvas _battleField;

    [Header("Dependencies")]
    [SerializeField] private CardManager _cardManager;
    [SerializeField] private CardOutlineManager _cardOutlineManager;
    [SerializeField] private CardLibrary _cardLibrary;
    [SerializeField] private CombatManager _combatManager;
    [SerializeField] private SpritePositioning _spritePositioningComponent;
    [SerializeField] private DamageVisualizer _damageVisualizer;
    [SerializeField] private GameObject _damageNumberPrefab;

    // Interface references
    private ISpritePositioning _spritePositioning;
    private IAttackHandler _attackHandler;
    private ICardSpawner _playerCardSpawner;
    private ICardSpawner _enemyCardSpawner;
    private ISelectionEffectHandler _enemySelectionEffectHandler;
    private ISelectionEffectHandler _playerSelectionEffectHandler;
    private IButtonCreator _buttonCreator;
    private ICardSelectionHandler _cardSelectionHandler;
    private ICardManager _cardManagerInterface;

    // State
    public int CurrentMana { get; private set; }
    public int MaxMana { get; private set; }
    private bool _buttonsInitialized = false;

    private void Awake()
    {
        // Convert components to interfaces
        _spritePositioning = _spritePositioningComponent;
        _cardManagerInterface = _cardManager;

        InitializeServices();
    }

    private void InitializeServices()
    {
        var attackLimiter = new AttackLimiter();
        var spawnerFactory = new CardSpawnerFactory(
            _spritePositioning,
            _cardLibrary,
            _combatManager,
            _damageVisualizer,
            _damageNumberPrefab,
            _wizardOutlineSprite,
            this,
            attackLimiter);

        _playerCardSpawner = spawnerFactory.CreatePlayerSpawner();
        _enemyCardSpawner = spawnerFactory.CreateEnemySpawner();
        _attackHandler = new AttackHandler(attackLimiter);

        _cardSelectionHandler = new CardSelectionHandler(
            _cardManagerInterface,
            _combatManager,
            _cardOutlineManager,
            _spritePositioning,
            this,
            _playerCardSpawner);

        _buttonCreator = new ButtonCreator(
            _battleField,
            _spritePositioning,
            _cardSelectionHandler);

        InitializeSelectionEffectHandlers();
    }

    private void InitializeSelectionEffectHandlers()
    {
        _playerSelectionEffectHandler = new PlayerSelectionEffectHandler(
            _spritePositioning,
            _cardManagerInterface,
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

    private void Update()
    {
        UpdateSelectionEffects();
        UpdatePlaceholderVisibility();
    }

    private void UpdateSelectionEffects()
    {
        bool hasSelectedCard = _cardManagerInterface.CurrentSelectedCard != null;
        _enemySelectionEffectHandler.ApplyEffect(hasSelectedCard);

        if (hasSelectedCard && IsPlacedCardSelected())
        {
            _playerSelectionEffectHandler.ApplyEffect();
        }
    }

    private void UpdatePlaceholderVisibility()
    {
        bool shouldShowPlaceholders = _cardManagerInterface.CurrentSelectedCard == null ||
                                    !IsPlacedCardSelected();

        SetPlaceholderActiveState(shouldShowPlaceholders);
    }

    private bool IsPlacedCardSelected()
    {
        return _cardManagerInterface.CurrentSelectedCard?.GetComponent<EntityManager>()?.placed ?? false;
    }

    public void SetPlaceholderActiveState(bool active)
    {
        StartCoroutine(_spritePositioning.SetPlaceholderActiveState(active));
    }
}