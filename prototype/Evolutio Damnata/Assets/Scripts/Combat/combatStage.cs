using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using GeneralInteraction;

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
    private ISpellEffectApplier _spellEffectApplier;

    public CardLibrary CardLibrary => _cardLibrary;
    public ISpritePositioning SpritePositioning => _spritePositioning;
    public ICardSpawner EnemyCardSpawner => _enemyCardSpawner;
    public ISpellEffectApplier SpellEffectApplier => _spellEffectApplier;

    public Image PrepPhaseImage => _prepPhaseImage;
    public Image CombatPhaseImage => _combatPhaseImage;
    public Image CleanupPhaseImage => _cleanupPhaseImage;

    private bool _buttonsInitialized = false;

    [Header("Combat Systems")]
    private AttackLimiter attackLimiter;

    private void Awake()
    {
        Debug.Log("[CombatStage] Starting initialization...");
        
        // Initialize core components first
        if (_spritePositioningComponent == null)
        {
            Debug.LogError("[CombatStage] SpritePositioning component is not assigned in inspector!");
            enabled = false;
            return;
        }
        _spritePositioning = _spritePositioningComponent;
        
        // Set all component references first
        _cardManager = _cardManagerComponent;
        _combatManager = _combatManagerComponent;
        _cardOutlineManager = _cardOutlineManagerComponent;

        // Initialize combat systems
        attackLimiter = new AttackLimiter();
        
        // Ensure _cardManager is set before creating OngoingEffectApplier
        _ongoingEffectApplier = new OngoingEffectApplier(_cardManager);
        
        // Initialize SpellEffectApplier with required dependencies
        if (_damageVisualizer == null || _damageNumberPrefab == null)
        {
            Debug.LogError("[CombatStage] Required components for SpellEffectApplier are not assigned!");
            enabled = false;
            return;
        }
        
        // Now _cardManager should be properly set before creating SpellEffectApplier
        _spellEffectApplier = new SpellEffectApplier(
            _cardManager,
            _ongoingEffectApplier,
            _damageVisualizer,
            _damageNumberPrefab,
            _cardLibrary
        );
        
        // Initialize other services
        InitializeServices();
    }

    private void InitializeServices()
    {
        Debug.Log("[CombatStage] Initializing services...");
        
        if (_cardManagerComponent == null || _cardLibrary == null || _combatManagerComponent == null)
        {
            Debug.LogError("[CombatStage] Required components are not assigned in inspector!");
            enabled = false;
            return;
        }

        // Ensure references are set before proceeding
        if (_cardManager == null || _combatManager == null || _cardOutlineManager == null)
        {
            Debug.LogError("[CombatStage] Required interface references are not set!");
            enabled = false;
            return;
        }

        var combatRulesEngine = new CombatRulesEngine();

        var spawnerFactory = new CardSpawnerFactory(
            _spritePositioning,
            _cardLibrary,
            _combatManagerComponent,
            _damageVisualizer,
            _damageNumberPrefab,
            _wizardOutlineSprite,
            this,
            attackLimiter,
            _ongoingEffectApplier
        );

        _playerCardSpawner = spawnerFactory.CreatePlayerSpawner();
        _enemyCardSpawner = spawnerFactory.CreateEnemySpawner();
        _attackHandler = new AttackHandler(attackLimiter, combatRulesEngine);

        var manaChecker = new ManaChecker(
            _combatManagerComponent,
            _cardOutlineManager,
            _cardManager
        );

        // Create CardSelectionHandler with null checks
        if (_spritePositioning != null && _spellEffectApplier != null)
        {
            _cardSelectionHandler = gameObject.AddComponent<CardSelectionHandler>();
            _cardSelectionHandler.Initialize(
                _cardManager,
                _combatManager,
                _cardOutlineManager,
                _spritePositioning,
                this,
                _playerCardSpawner,
                manaChecker,
                _spellEffectApplier
            );

            // Only create ButtonCreator if CardSelectionHandler was created successfully
            if (_battleField != null && _cardSelectionHandler != null)
            {
                var buttonCreatorComponent = gameObject.AddComponent<ButtonCreator>();
                buttonCreatorComponent.Initialize(
                    _battleField,
                    _spritePositioning,
                    _cardSelectionHandler
                );
                _buttonCreator = buttonCreatorComponent;
            }
            else
            {
                Debug.LogError("[CombatStage] Cannot initialize ButtonCreator - missing dependencies!");
            }
        }
        else
        {
            Debug.LogError("[CombatStage] Cannot initialize CardSelectionHandler - missing dependencies!");
        }

        InitializeSelectionEffectHandlers();
        
        Debug.Log("[CombatStage] Services initialized successfully");
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
        if (_spritePositioning == null)
        {
            Debug.LogError("[CombatStage] Cannot initialize selection effect handlers - SpritePositioning is null!");
            return;
        }

        // Only create player selection handler if cardManager is available
        if (_cardManager != null)
        {
            _playerSelectionEffectHandler = new PlayerSelectionEffectHandler(
                _spritePositioning,
                _cardManager,
                new Color(0.5f, 1f, 0.5f, 1f));
        }
        else
        {
            Debug.LogWarning("[CombatStage] Cannot create PlayerSelectionEffectHandler - CardManager is null!");
        }

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
        _buttonCreator.AddButtonsToHealthIcons();
        _buttonsInitialized = true;
    }

    public void HandleMonsterAttack(EntityManager attacker, EntityManager target)
    {
        if (_attackHandler == null)
        {
            Debug.LogError("AttackHandler is not initialized!");
            return;
        }

        _attackHandler.HandleAttack(attacker, target);
    }

    // Overload for attacking health icons
    public void HandleMonsterAttack(EntityManager attacker, HealthIconManager healthIcon)
    {
        if (_attackHandler == null)
        {
            Debug.LogError("AttackHandler is not initialized!");
            return;
        }

        // Health icons are EntityManagers, so we can pass them directly
        _attackHandler.HandleAttack(attacker, healthIcon);
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

        // Check if the selected card is a spell card
        bool isSpellCardSelected = false;
        if (hasSelectedCard && !isMonsterSelected)
        {
            var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
            if (cardUI != null && cardUI.Card != null && cardUI.Card.CardType != null)
            {
                isSpellCardSelected = cardUI.Card.CardType.IsSpellCard;
            }
        }

        // Apply enemy selection effects if a spell card is selected OR if a player's monster is selected
        if (isSpellCardSelected || isMonsterSelected)
        {
            _enemySelectionEffectHandler.ApplyEffect(true);
        }
        else
        {
            // Reset enemy tints if nothing is selected
            foreach (var entity in _spritePositioning.EnemyEntities)
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

        // Apply player selection effects if we have a spell card selected OR if a player monster is selected
        if (isSpellCardSelected || isMonsterSelected)
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

        // Check if the selected card is a spell card
        bool isSpellCard = false;
        bool isDrawBloodpriceOnlySpell = false;

        if (_cardManager.CurrentSelectedCard != null)
        {
            var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
            if (cardUI != null && cardUI.Card != null && cardUI.Card.CardType != null)
            {
                CardData cardData = cardUI.Card.CardType;
                isSpellCard = cardData.IsSpellCard;

                // Check if the spell card only has Draw and/or Bloodprice effects
                if (isSpellCard && cardData.EffectTypes != null && cardData.EffectTypes.Count > 0)
                {
                    bool hasDrawOrBloodprice = false;
                    bool hasOtherEffects = false;

                    foreach (var effect in cardData.EffectTypes)
                    {
                        if (effect == SpellEffect.Draw || effect == SpellEffect.Bloodprice)
                        {
                            hasDrawOrBloodprice = true;
                        }
                        else
                        {
                            hasOtherEffects = true;
                            break;
                        }
                    }

                    // Only set true if it has at least one Draw/Bloodprice effect and NO other effects
                    isDrawBloodpriceOnlySpell = hasDrawOrBloodprice && !hasOtherEffects;
                }
            }
        }

        foreach (var placeholder in _spritePositioning.PlayerEntities)
        {
            var entityManager = placeholder.GetComponent<EntityManager>();
            if (entityManager != null)
            {
                // Skip if entity is fading out
                if (entityManager.IsFadingOut) continue;

                if (isSpellCard)
                {
                    if (isDrawBloodpriceOnlySpell)
                    {
                        // If a spell card with only Draw and/or Bloodprice effects is selected, show all placeholders
                        placeholder.SetActive(true);
                    }
                    else
                    {
                        // For other spell cards, only show entities that are placed
                        placeholder.SetActive(entityManager.placed);
                    }
                }
                else
                {
                    // If not a spell card or no card selected, follow original logic
                    placeholder.SetActive(entityManager.placed || shouldShowPlaceholders);
                }
            }
        }
    }

    private bool IsPlacedCardSelected()
    {
        return _cardManager.CurrentSelectedCard?.GetComponent<EntityManager>()?.placed ?? false;
    }

    public AttackLimiter GetAttackLimiter()
    {
        return attackLimiter;
    }

    public OngoingEffectApplier GetOngoingEffectApplier()
    {
        return _ongoingEffectApplier;
    }
}
