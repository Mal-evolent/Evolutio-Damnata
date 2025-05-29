using TMPro;
using UnityEngine;
using CardSystem.History;

public class CardPlayEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text ownerTag;
    [SerializeField] private TMP_Text effectTargetsText;

    public void Setup(CardPlayRecord record)
    {
        if (record == null) return;

        cardNameText.text = $"Card Name: {record.CardName}";
        turnText.text = $"turn: {record.TurnNumber}";
        ownerTag.text = $"Owner: {(record.IsEnemyCard ? "Enemy" : "Player")}";
        effectTargetsText.text = record.EffectTargets.Count > 0
            ? $"Effects:\n{string.Join("\n", record.EffectTargets)}"
            : "Effects: None";
    }
}
