using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class CardSelectionHandler : MonoBehaviour, ICardSelectionHandler
{
    private ICardManager _cardManager;
    private ICombatManager _combatManager;
    private ICardOutlineManager _cardOutlineManager;
    private ISpritePositioning _spritePositioning;
    private ICombatStage _combatStage;
    private ICardSpawner _playerCardSpawner;
    private IManaChecker _manaChecker;
    private ISpellEffectApplier _spellEffectApplier;

    private PlayerCardSelectionHandler _playerCardSelectionHandler;
    private EnemyCardSelectionHandler _enemyCardSelectionHandler;

    public void Initialize(
        ICardManager cardManager,
        ICombatManager combatManager,
        ICardOutlineManager cardOutlineManager,
        ISpritePositioning spritePositioning,
        ICombatStage combatStage,
        ICardSpawner playerCardSpawner,
        IManaChecker manaChecker,
        ISpellEffectApplier spellEffectApplier)
    {
        _cardManager = cardManager ?? throw new System.ArgumentNullException(nameof(cardManager));
        _combatManager = combatManager ?? throw new System.ArgumentNullException(nameof(combatManager));
        _cardOutlineManager = cardOutlineManager ?? throw new System.ArgumentNullException(nameof(cardOutlineManager));
        _spritePositioning = spritePositioning ?? throw new System.ArgumentNullException(nameof(spritePositioning));
        _combatStage = combatStage ?? throw new System.ArgumentNullException(nameof(combatStage));
        _playerCardSpawner = playerCardSpawner ?? throw new System.ArgumentNullException(nameof(playerCardSpawner));
        _manaChecker = manaChecker ?? throw new System.ArgumentNullException(nameof(manaChecker));
        _spellEffectApplier = spellEffectApplier ?? throw new System.ArgumentNullException(nameof(spellEffectApplier));

        InitializeHandlers();
    }

    private void InitializeHandlers()
    {
        var cardValidator = new CardValidator();
        var cardRemover = new CardRemover(_cardManager);

        _playerCardSelectionHandler = new PlayerCardSelectionHandler(
            _cardManager,
            _combatManager,
            cardValidator,
            cardRemover,
            _cardOutlineManager,
            _playerCardSpawner,
            _spellEffectApplier
        );

        _enemyCardSelectionHandler = new EnemyCardSelectionHandler(
            _cardManager,
            _combatManager,
            _cardOutlineManager,
            _spritePositioning,
            _combatStage,
            _manaChecker,
            _spellEffectApplier
        );
    }

    public void ResetAllMonsterTints()
    {
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

    public void OnPlayerButtonClick(int index)
    {
        if (!ValidateSelection(index, _spritePositioning.PlayerEntities, out EntityManager entityManager))
            return;

        // If we have a card selected and it's our turn, try to play it
        if (_cardManager.HandCardObjects.Contains(_cardManager.CurrentSelectedCard) && _combatManager.IsPlayerPrepPhase())
        {
            var cardUI = _cardManager.CurrentSelectedCard.GetComponent<CardUI>();
            var cardData = cardUI?.Card?.CardType;

            // Only check for occupied space if it's a monster card
            if (cardData != null && cardData.IsMonsterCard && entityManager != null && entityManager.placed)
            {
                Debug.Log("Cannot place a monster on an already occupied space!");
                return;
            }

            _playerCardSelectionHandler.HandlePlayerCardSelection(index, entityManager);
            return;
        }

        // If it's a placed monster we're selecting
        if (entityManager != null && entityManager.placed)
        {
            // If clicking the same monster, toggle its selection off
            if (_cardManager.CurrentSelectedCard == _spritePositioning.PlayerEntities[index])
            {
                _cardManager.CurrentSelectedCard = null;
                ResetAllMonsterTints();
                return;
            }

            // If we have any card selected, deselect it first
            if (_cardManager.CurrentSelectedCard != null)
            {
                _cardOutlineManager.RemoveHighlight();
            }

            // Update monster selection
            _cardManager.CurrentSelectedCard = _spritePositioning.PlayerEntities[index];
            return;
        }

        Debug.Log("No card selected or not the players turn!");
        _cardManager.CurrentSelectedCard = null;
        ResetAllMonsterTints();
    }

    public void OnEnemyButtonClick(int index)
    {
        if (!ValidateSelection(index, _spritePositioning.EnemyEntities, out EntityManager entityManager))
            return;

        if (_cardManager.CurrentSelectedCard != null && _combatManager.PlayerTurn)
        {
            // Try spell handling first
            _enemyCardSelectionHandler.HandleEnemyCardSelection(index, entityManager);

            if (_cardManager.CurrentSelectedCard != null)
            {
                var attacker = _cardManager.CurrentSelectedCard.GetComponent<EntityManager>();
                if (attacker != null && attacker.placed)
                {
                    HandlePossibleAttack(entityManager);
                }
                else
                {
                    Debug.Log("Selected monster is not valid for attacking!");
                    _cardManager.CurrentSelectedCard = null;
                    ResetAllMonsterTints();
                }
            }
        }
        else
        {
            Debug.Log("No card selected or not the players turn!");
            _cardOutlineManager.RemoveHighlight();
            ResetAllMonsterTints();
        }
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

        if (_combatManager.IsPlayerCombatPhase())
        {
            _combatStage.HandleMonsterAttack(playerEntity, enemyEntity);
            // Deselect everything after attack
            _cardManager.CurrentSelectedCard = null;
            _cardOutlineManager.RemoveHighlight();
            ResetAllMonsterTints();
        }
        else
        {
            Debug.Log("Attacks are not allowed at this stage!");
        }
    }
}
