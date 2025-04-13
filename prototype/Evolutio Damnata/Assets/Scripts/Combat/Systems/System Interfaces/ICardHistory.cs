public interface ICardHistory
{
    void RecordCardPlay(Card card, EntityManager player, int turnNumber, int manaUsed);
    void RecordCardPlay(CardDataWrapper cardWrapper, EntityManager player, int turnNumber, int manaUsed);
    void ClearHistory();
    int GetTotalCardsPlayed();
    int GetPlayerCardsPlayed();
    int GetEnemyCardsPlayed();
    int GetCardsPlayedInTurn(int turnNumber);
} 