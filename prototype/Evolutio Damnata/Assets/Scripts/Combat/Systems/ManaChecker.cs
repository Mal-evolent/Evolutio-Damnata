using UnityEngine;

public class ManaChecker : IManaChecker
{
    private readonly IManaProvider _manaProvider;
    private readonly ICardOutlineManager _cardOutlineManager;
    private readonly ICardManager _cardManager;

    public ManaChecker(
        IManaProvider manaProvider,
        ICardOutlineManager cardOutlineManager,
        ICardManager cardManager)
    {
        _manaProvider = manaProvider ?? throw new System.ArgumentNullException(nameof(manaProvider));
        _cardOutlineManager = cardOutlineManager ?? throw new System.ArgumentNullException(nameof(cardOutlineManager));
        _cardManager = cardManager ?? throw new System.ArgumentNullException(nameof(cardManager));
    }

    public bool HasEnoughPlayerMana(CardData cardData)
    {
        if (cardData == null)
        {
            Debug.LogError("CardData is null!");
            return false;
        }

        bool hasEnoughMana = _manaProvider.PlayerMana >= cardData.ManaCost;

        if (!hasEnoughMana)
        {
            HandleInsufficientPlayerMana(cardData);
        }

        return hasEnoughMana;
    }

    public bool HasEnoughEnemyMana(CardData cardData)
    {
        if (cardData == null)
        {
            Debug.LogError("CardData is null!");
            return false;
        }

        bool hasEnoughMana = _manaProvider.EnemyMana >= cardData.ManaCost;

        if (!hasEnoughMana)
        {
            Debug.Log($"Not enough enemy mana. Required: {cardData.ManaCost}, Available: {_manaProvider.EnemyMana}");
        }

        return hasEnoughMana;
    }

    public void DeductPlayerMana(CardData cardData)
    {
        if (cardData == null) return;

        _manaProvider.PlayerMana -= cardData.ManaCost;
        _manaProvider.UpdatePlayerManaUI();
    }

    public void DeductEnemyMana(CardData cardData)
    {
        if (cardData == null) return;

        _manaProvider.EnemyMana -= cardData.ManaCost;
    }

    private void HandleInsufficientPlayerMana(CardData cardData)
    {
        Debug.Log($"Not enough player mana. Required: {cardData.ManaCost}, Available: {_manaProvider.PlayerMana}");
        _cardOutlineManager.RemoveHighlight();
        _cardManager.CurrentSelectedCard = null;
    }
}