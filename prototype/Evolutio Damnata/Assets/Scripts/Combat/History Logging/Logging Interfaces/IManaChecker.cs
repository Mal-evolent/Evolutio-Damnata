public interface IManaChecker
{
    bool HasEnoughPlayerMana(CardData cardData);
    bool HasEnoughEnemyMana(CardData cardData);
    void DeductPlayerMana(CardData cardData);
    void DeductEnemyMana(CardData cardData);
}