public interface ICardHistory
{
    void RecordCardPlay(Card card, EntityManager player, int turnNumber, int manaUsed);
    void ClearHistory();
    int GetTotalCardsPlayed();
    int GetCardsPlayedInTurn(int turnNumber);
} 