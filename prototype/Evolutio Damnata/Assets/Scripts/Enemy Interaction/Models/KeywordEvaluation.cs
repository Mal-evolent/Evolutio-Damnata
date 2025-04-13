namespace EnemyInteraction.Models
{
    public class KeywordEvaluation
    {
        public float BaseScore { get; set; }
        public bool IsPositive { get; set; }
        public bool IsDefensive { get; set; }
        public bool IsOffensive { get; set; }
        public bool RequiresTarget { get; set; }
    }
} 