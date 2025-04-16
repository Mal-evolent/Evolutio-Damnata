using EnemyInteraction.Models;
using System.Collections;
using System.Collections.Generic;

namespace EnemyInteraction.Managers.Execution
{
    public interface ICardPlayExecutor
    {
        IEnumerator PlayCardsInOrder(List<Card> cardsToPlay, Deck enemyDeck, BoardState boardState);
        bool PlayMonsterCard(Card card, BoardState boardState);
        bool CanPlaySpellCard(Card card);
        IEnumerator PlaySpellCardWithDelay(Card card);
        bool ContainsOnlyDrawAndBloodpriceEffects(CardData cardData);
    }
}
