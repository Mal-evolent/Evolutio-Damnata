public interface ICombatStage
{
    void UpdateManaUI();
    void HandleMonsterAttack(EntityManager attacker, EntityManager target);
    void SpawnEnemyCard(string cardName, int position);
}