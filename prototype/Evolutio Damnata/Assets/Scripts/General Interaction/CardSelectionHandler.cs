using UnityEngine;

public class CardSelectionHandler : MonoBehaviour, ICardSelectionHandler
{
    [Header("Dependencies")]
    [SerializeField] private CardManager _cardManagerComponent;
    [SerializeField] private CombatManager _combatManagerComponent;
    [SerializeField] private CardOutlineManager _cardOutlineManagerComponent;
    [SerializeField] private SpritePositioning _spritePositioningComponent;
    [SerializeField] private CombatStage _combatStageComponent;
    [SerializeField] private GeneralEntities _playerCardSpawnerComponent;

    private ICardManager _cardManager;
    private ICombatManager _combatManager;
    private ICardOutlineManager _cardOutlineManager;
    private ISpritePositioning _spritePositioning;
    private ICombatStage _combatStage;
    private ICardSpawner _playerCardSpawner;

    private PlayerCardSelectionHandler _playerCardSelectionHandler;
    private EnemyCardSelectionHandler _enemyCardSelectionHandler;

    public void Initialize(ICardManager cardManager,
                         ICombatManager combatManager,
                         ICardOutlineManager cardOutlineManager,
                         ISpritePositioning spritePositioning,
                         ICombatStage combatStage,
                         ICardSpawner playerCardSpawner)
    {
        _cardManager = cardManager;
        _combatManager = combatManager;
        _cardOutlineManager = cardOutlineManager;
        _spritePositioning = spritePositioning;
        _combatStage = combatStage;
        _playerCardSpawner = playerCardSpawner;

        InitializeHandlers();
    }

    private void Awake()
    {
        // Fallback to component references if not initialized via interface
        if (_cardManager == null) _cardManager = _cardManagerComponent;
        if (_combatManager == null) _combatManager = _combatManagerComponent;
        if (_cardOutlineManager == null) _cardOutlineManager = _cardOutlineManagerComponent;
        if (_spritePositioning == null) _spritePositioning = _spritePositioningComponent;
        if (_combatStage == null) _combatStage = _combatStageComponent;
        if (_playerCardSpawner == null) _playerCardSpawner = _playerCardSpawnerComponent;

        if (_playerCardSelectionHandler == null || _enemyCardSelectionHandler == null)
        {
            InitializeHandlers();
        }
    }

    private void InitializeHandlers()
    {
        _playerCardSelectionHandler = new PlayerCardSelectionHandler(
            _cardManager,
            _combatManager,
            _cardOutlineManager,
            _spritePositioning,
            _combatStage,
            _playerCardSpawner);

        _enemyCardSelectionHandler = new EnemyCardSelectionHandler(
            _cardManager,
            _combatManager,
            _cardOutlineManager,
            _spritePositioning,
            _combatStage);
    }

    public void OnPlayerButtonClick(int index)
    {
        if (!ValidateSelection(index, _spritePositioning.PlayerEntities, out EntityManager entityManager))
            return;

        if (_cardManager.CurrentSelectedCard != null && _combatManager.PlayerTurn)
        {
            _playerCardSelectionHandler.HandlePlayerCardSelection(index, entityManager);
        }
        else if (_cardManager.CurrentSelectedCard == null)
        {
            if (entityManager != null && entityManager.placed)
            {
                _cardManager.CurrentSelectedCard = _spritePositioning.PlayerEntities[index];
            }
        }
        else
        {
            Debug.Log("No card selected or not the players turn!");
            _cardOutlineManager.RemoveHighlight();
        }
    }

    public void OnEnemyButtonClick(int index)
    {
        if (!ValidateSelection(index, _spritePositioning.EnemyEntities, out EntityManager entityManager))
            return;

        if (_cardManager.CurrentSelectedCard != null && _combatManager.PlayerTurn)
        {
            _enemyCardSelectionHandler.HandleEnemyCardSelection(index, entityManager);
        }
        else
        {
            Debug.Log("No card selected or not the players turn!");
            _cardOutlineManager.RemoveHighlight();
            return;
        }

        HandlePossibleAttack(entityManager);
    }

    private bool ValidateSelection(int index, System.Collections.Generic.List<GameObject> entities, out EntityManager entityManager)
    {
        entityManager = null;

        if (index < 0 || index >= entities.Count)
        {
            Debug.LogError($"Invalid entity index: {index}");
            return false;
        }

        entityManager = entities[index]?.GetComponent<EntityManager>();
        if (entityManager == null)
        {
            Debug.LogError($"No EntityManager found at index {index}");
            return false;
        }

        return true;
    }

    private void HandlePossibleAttack(EntityManager enemyEntity)
    {
        if (_cardManager.CurrentSelectedCard == null) return;

        EntityManager playerEntity = _cardManager.CurrentSelectedCard.GetComponent<EntityManager>();
        if (playerEntity == null || !playerEntity.placed) return;

        if (_combatManager.IsPlayerCombatPhase)
        {
            _combatStage.HandleMonsterAttack(playerEntity, enemyEntity);
            _cardManager.CurrentSelectedCard = null;
        }
        else
        {
            Debug.Log("Attacks are not allowed at this stage!");
        }
    }
}