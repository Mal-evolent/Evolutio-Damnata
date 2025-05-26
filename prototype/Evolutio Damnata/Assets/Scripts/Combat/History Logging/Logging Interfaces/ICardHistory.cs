public interface ICardHistory
{
    void RecordCardPlay(Card card, EntityManager entity, int turnNumber, int manaUsed);
    void RecordCardPlay(CardDataWrapper cardWrapper, EntityManager entity, int turnNumber, int manaUsed);
    void RecordAttack(EntityManager attacker, EntityManager target, int turnNumber, float damageDealt, float counterDamage, bool isRangedAttack);
    void RecordOngoingEffect(IOngoingEffect effect, int duration, string sourceCardName, int turnNumber = -1);
    void RecordEffectApplication(SpellEffect effectType, EntityManager target, int damage, int turnNumber = -1);
    void RecordAdditionalEffectTarget(string effectName, string targetName);
    int GetTotalCardsPlayed();
    int GetPlayerCardsPlayed();
    int GetEnemyCardsPlayed();
    int GetCardsPlayedInTurn(int turnNumber);
}
