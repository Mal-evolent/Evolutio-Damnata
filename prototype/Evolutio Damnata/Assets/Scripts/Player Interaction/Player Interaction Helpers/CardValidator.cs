
public class CardValidator : ICardValidator
{
    public bool ValidateCardPlay(CardData cardData, CombatPhase currentPhase, bool isPlaced)
    {
        if (cardData == null && !isPlaced)
            return false;

        if (cardData.IsMonsterCard && currentPhase != CombatPhase.PlayerPrep)
            return false;

        if (cardData.IsSpellCard && currentPhase == CombatPhase.CleanUp)
            return false;

        return true;
    }
}
