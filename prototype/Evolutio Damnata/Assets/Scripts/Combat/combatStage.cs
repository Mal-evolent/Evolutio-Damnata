using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class CombatStage : MonoBehaviour, ICombatStage
{
    [Header("UI References")]
    [SerializeField] private Sprite _wizardOutlineSprite;
    [SerializeField] private GameObject _manaBar;
    [SerializeField] private GameObject _manaText;
    [SerializeField] private GameObject _manaTextShadow;
    [SerializeField] private Canvas _battleField;
    [SerializeField] private Image _prepPhaseImage;
    [SerializeField] private Image _combatPhaseImage;
    [SerializeField] private Image _cleanupPhaseImage;

    [Header("Dependencies")]
    [SerializeField] private CardManager _cardManagerComponent;
    [SerializeField] private CardOutlineManager _cardOutlineManagerComponent;
    [SerializeField] private CardLibrary _cardLibrary;
    [SerializeField] private CombatManager _combatManagerComponent;
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
    private ICardManager _cardManager;
    private ICombatManager _combatManager;
    private ICardOutlineManager _cardOutlineManager;
    private OngoingEffectApplier _ongoingEffectApplier;
    public CardLibrary CardLibrary => _cardLibrary;
    public ISpritePositioning SpritePositioning => _spritePositioning;
    public ICardSpawner EnemyCardSpawner => _enemyCardSpawner;

    public Image PrepPhaseImage => _prepPhaseImage;
    public Image CombatPhaseImage => _combatPhaseImage;
    public Image CleanupPhaseImage => _cleanupPhaseImage;

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
        var ongoingEffectApplier = new OngoingEffectApplier(_cardManager);

        var spawnerFactory = new CardSpawnerFactory(
            _spritePositioning,
            _cardLibrary,
            _combatManagerComponent,
            _damageVisualizer,
            _damageNumberPrefab,
            _wizardOutlineSprite,
            this,
            attackLimiter,
            ongoingEffectApplier
        );

        _playerCardSpawner = spawnerFactory.CreatePlayerSpawner();
        _enemyCardSpawner = spawnerFactory.CreateEnemySpawner();
        _attackHandler = new AttackHandler(attackLimiter);

        var spellEffectApplier = new SpellEffectApplier(
            _cardManager,
            ongoingEffectApplier,
            _damageVisualizer,
            _damageNumberPrefab
        );

        var manaChecker = new ManaChecker(
            _combatManagerComponent,
            _cardOutlineManager,
            _cardManager
        );

        _cardSelectionHandler = gameObject.AddComponent<CardSelectionHandler>();
        _cardSelectionHandler.Initialize(
            _cardManager,
            _combatManager,
            _cardOutlineManager,
            _spritePositioning,
            this,
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

    public void UpdateManaUI()
    {
        if (_manaBar == null || _manaText == null) return;

        var manaSlider = _manaBar.GetComponent<Slider>();
        manaSlider.maxValue = _combatManagerComponent.MaxMana;
        manaSlider.value = _combatManagerComponent.PlayerMana;
        _manaText.GetComponent<TMP_Text>().text = _combatManagerComponent.PlayerMana.ToString();
        _manaTextShadow.GetComponent<TMP_Text>().text = _combatManagerComponent.PlayerMana.ToString();
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

    private void Update()
    {
        UpdateSelectionEffects();
        UpdatePlaceholderVisibility();
    }

    private void UpdateSelectionEffects()
    {
        bool hasSelectedCard = _cardManager.CurrentSelectedCard != null;
        bool isMonsterSelected = hasSelectedCard && IsPlacedCardSelected();

        // Apply enemy selection effects only if we have a card or monster selected
        _enemySelectionEffectHandler.ApplyEffect(hasSelectedCard);

        // Apply player selection effects if we have a monster selected
        if (isMonsterSelected)
        {
            _playerSelectionEffectHandler.ApplyEffect();
        }
        else
        {
            // Reset player monster tints if nothing is selected
            foreach (var entity in _spritePositioning.PlayerEntities)
            {
                if (entity != null)
                {
                    var image = entity.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = Color.white;
                    }
                }
            }
        }
    }

    private void UpdatePlaceholderVisibility()
    {
        bool shouldShowPlaceholders = _cardManager.CurrentSelectedCard != null &&
                                    !IsPlacedCardSelected();

        foreach (var placeholder in _spritePositioning.PlayerEntities)
        {
            var entityManager = placeholder.GetComponent<EntityManager>();
            if (entityManager != null)
            {
                // Skip if entity is fading out
                if (entityManager.IsFadingOut) continue;

                // Original logic
                placeholder.SetActive(entityManager.placed || shouldShowPlaceholders);
            }
        }
    }

    private bool IsPlacedCardSelected()
    {
        return _cardManager.CurrentSelectedCard?.GetComponent<EntityManager>()?.placed ?? false;
    }
}
