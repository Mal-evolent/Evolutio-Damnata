using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellEffectApplier
{
    private CardManager cardManager;

    public SpellEffectApplier(CardManager cardManager)
    {
        this.cardManager = cardManager;
    }

    public void ApplySpellEffect(EntityManager entityManager, CardData cardData, int index)
    {
        if (entityManager != null && entityManager.placed)
        {
            // Apply spell effect to the placed monster
            Debug.Log($"Applying spell {cardManager.currentSelectedCard.name} to monster {index}");
            SpellCard spellCard = cardManager.currentSelectedCard.GetComponent<SpellCard>();
            if (spellCard == null)
            {
                Debug.LogWarning("SpellCard component not found on current selected card! Adding SpellCard component.");
                spellCard = cardManager.currentSelectedCard.AddComponent<SpellCard>();

                spellCard.CardType = cardData;
                spellCard.CardName = cardData.CardName;
                spellCard.CardImage = cardData.CardImage;
                spellCard.Description = cardData.Description;
                spellCard.ManaCost = cardData.ManaCost;
                spellCard.EffectTypes = cardData.EffectTypes;
                spellCard.EffectValue = cardData.EffectValue;
                spellCard.Duration = cardData.Duration;
            }
            spellCard.targetEntity = entityManager;
            spellCard.Play();
        }
    }
}
