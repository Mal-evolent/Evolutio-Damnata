using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICardValidator
{
    bool ValidateCardPlay(CardData cardData, CombatPhase currentPhase, bool isPlaced);
}