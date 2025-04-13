using System.Linq;

namespace EnemyInteraction.Extensions
{
    public static class CardDataExtensions
    {
        public static bool HasKeyword(this CardData cardData, Keywords.MonsterKeyword keyword)
        {
            return cardData.Keywords != null && cardData.Keywords.Contains(keyword);
        }
    }
} 