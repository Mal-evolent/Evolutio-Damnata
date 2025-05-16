using EnemyInteraction.Models;
using System.Collections;
using System.Collections.Generic;

namespace EnemyInteraction.Managers.Execution
{
    public interface ICardPlayExecutor
    {
        /// <summary>
        /// Plays a sequence of cards in their optimal order, handling mana costs and state management
        /// </summary>
        /// <param name="cardsToPlay">The list of cards to play</param>
        /// <param name="enemyDeck">The enemy's deck containing the cards</param>
        /// <param name="boardState">Current state of the game board</param>
        IEnumerator PlayCardsInOrder(List<Card> cardsToPlay, Deck enemyDeck, BoardState boardState);

        /// <summary>
        /// Plays a monster card, waiting for any entities that are fading out at the target position
        /// </summary>
        /// <param name="card">The monster card to play</param>
        /// <param name="enemyDeck">The enemy's deck containing the card</param>
        /// <param name="boardState">Current state of the game board</param>
        IEnumerator PlayMonsterCardWithFadeCheck(Card card, Deck enemyDeck, BoardState boardState);

        /// <summary>
        /// Determines if a spell card can be played based on available targets and game state
        /// </summary>
        /// <param name="card">The spell card to evaluate</param>
        bool CanPlaySpellCard(Card card);

        /// <summary>
        /// Plays a spell card with a delay for visual effects
        /// </summary>
        /// <param name="card">The spell card to play</param>
        IEnumerator PlaySpellCardWithDelay(Card card);

        /// <summary>
        /// Checks if a card data contains only Draw and/or Bloodprice effects
        /// </summary>
        /// <param name="cardData">The card data to check</param>
        bool ContainsOnlyDrawAndBloodpriceEffects(CardData cardData);
    }
}
