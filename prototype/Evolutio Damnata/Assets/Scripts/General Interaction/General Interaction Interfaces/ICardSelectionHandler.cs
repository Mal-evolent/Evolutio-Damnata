public interface ICardSelectionHandler
{
    void OnPlayerButtonClick(int index);
    void OnEnemyButtonClick(int index);
    void Initialize(
        ICardManager cardManager,
        ICombatManager combatManager,
        ICardOutlineManager cardOutlineManager,
        ISpritePositioning spritePositioning,
        ICombatStage combatStage,
        ICardSpawner playerCardSpawner,
        IManaChecker manaChecker,
        ISpellEffectApplier spellEffectApplier);
        public void OnPlayerHealthIconClick();

}