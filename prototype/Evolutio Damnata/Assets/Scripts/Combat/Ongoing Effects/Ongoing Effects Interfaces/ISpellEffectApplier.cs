public interface ISpellEffectApplier
{
    void ApplySpellEffects(EntityManager target, CardData spellData, int positionIndex);
}