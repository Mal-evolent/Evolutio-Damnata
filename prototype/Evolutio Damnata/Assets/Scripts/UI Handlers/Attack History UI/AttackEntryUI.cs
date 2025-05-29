using TMPro;
using UnityEngine;
using CardSystem.History;
using System.Linq;

public class AttackEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text attackerNameText;
    [SerializeField] private TMP_Text targetNameText;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text ownerTag;
    [SerializeField] private TMP_Text attackTypeText;
    [SerializeField] private TMP_Text keywordsText;
    [SerializeField] private TMP_Text counterDamageText;

    public void Setup(AttackRecord record)
    {
        if (record == null) return;

        attackerNameText.text = record.AttackerName;
        targetNameText.text = record.TargetName;
        turnText.text = $"Turn {record.TurnNumber}";
        damageText.text = $"{record.DamageDealt} DMG";
        ownerTag.text = record.IsEnemyAttack ? "Enemy" : "Player";

        // Show attack type (Ranged/Melee)
        attackTypeText.text = record.WasRangedAttack ? "Ranged" : "Melee";

        // Display keywords if any
        if (record.AttackerKeywords != null && record.AttackerKeywords.Count > 0)
        {
            var keywordStrings = record.AttackerKeywords.Select(k => k.ToString());
            keywordsText.text = string.Join(", ", keywordStrings);
        }
        else
        {
            keywordsText.text = "None";
        }

        counterDamageText.text = $"Counter: {(record.WasRangedAttack ? "0" : record.CounterDamage.ToString())} DMG";
    }
}
