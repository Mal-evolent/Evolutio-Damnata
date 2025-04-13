public interface ISpellEffectApplier
{
    void ApplySpellEffects(EntityManager target, CardData spellData, int positionIndex);
    void ApplySpellEffectsAI(EntityManager target, CardData spellData, int positionIndex);
}