using UnityEngine;

public class CardSpawnerFactory : ICardSpawnerFactory
{
    private readonly ISpritePositioning _spritePositioning;
    private readonly CardLibrary _cardLibrary;
    private readonly IManaProvider _manaProvider;
    private readonly DamageVisualizer _damageVisualizer;
    private readonly GameObject _damageNumberPrefab;
    private readonly Sprite _wizardOutlineSprite;
    private readonly ICombatStage _combatStage;
    private readonly AttackLimiter _attackLimiter;

    public CardSpawnerFactory(
        ISpritePositioning spritePositioning,
        CardLibrary cardLibrary,
        IManaProvider manaProvider,
        DamageVisualizer damageVisualizer,
        GameObject damageNumberPrefab,
        Sprite wizardOutlineSprite,
        ICombatStage combatStage, 
        AttackLimiter attackLimiter)
    {
        _spritePositioning = spritePositioning ?? throw new System.ArgumentNullException(nameof(spritePositioning));
        _cardLibrary = cardLibrary ?? throw new System.ArgumentNullException(nameof(cardLibrary));
        _manaProvider = manaProvider ?? throw new System.ArgumentNullException(nameof(manaProvider));
        _damageVisualizer = damageVisualizer ?? throw new System.ArgumentNullException(nameof(damageVisualizer));
        _damageNumberPrefab = damageNumberPrefab ?? throw new System.ArgumentNullException(nameof(damageNumberPrefab));
        _wizardOutlineSprite = wizardOutlineSprite ?? throw new System.ArgumentNullException(nameof(wizardOutlineSprite));
        _combatStage = combatStage ?? throw new System.ArgumentNullException(nameof(combatStage));
        _attackLimiter = attackLimiter ?? throw new System.ArgumentNullException(nameof(attackLimiter));
    }

    public ICardSpawner CreatePlayerSpawner()
    {
        return new GeneralEntities(
            _spritePositioning,
            _cardLibrary,
            _manaProvider,
            _damageVisualizer,
            _damageNumberPrefab,
            _wizardOutlineSprite,
            _combatStage, 
            _attackLimiter,
            EntityManager.MonsterType.Friendly);
    }

    public ICardSpawner CreateEnemySpawner()
    {
        return new GeneralEntities(
            _spritePositioning,
            _cardLibrary,
            _manaProvider,
            _damageVisualizer,
            _damageNumberPrefab,
            _wizardOutlineSprite,
            _combatStage, 
            _attackLimiter,
            EntityManager.MonsterType.Enemy);
    }

    public ICardSpawner CreateSpawner(EntityManager.MonsterType monsterType)
    {
        return monsterType switch
        {
            EntityManager.MonsterType.Friendly => CreatePlayerSpawner(),
            EntityManager.MonsterType.Enemy => CreateEnemySpawner(),
            _ => throw new System.ArgumentException($"Unknown monster type: {monsterType}")
        };
    }
}