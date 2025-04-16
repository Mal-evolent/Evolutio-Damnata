using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDataWrapper
{
    public string CardName { get; set; }
    public string Description { get; set; }
    public List<SpellEffect> EffectTypes { get; set; }

    public CardDataWrapper(CardData cardData)
    {
        CardName = cardData.CardName;
        Description = cardData.Description;
        EffectTypes = cardData.EffectTypes;
    }
}
