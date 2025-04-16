using System.Collections.Generic;
using UnityEngine;

namespace EnemyInteraction.Models
{
    public class BoardState
    {
        public List<EntityManager> EnemyMonsters { get; set; }
        public List<EntityManager> PlayerMonsters { get; set; }
        public int EnemyHealth { get; set; }
        public int PlayerHealth { get; set; }
        public int TurnCount { get; set; }
        public int EnemyMana { get; set; }
        public float EnemyBoardControl { get; set; }
        public float PlayerBoardControl { get; set; }
        public float BoardControlDifference { get; set; }
        public int HealthAdvantage { get; set; }
        public float HealthRatio { get; set; }
        public int playerHandSize { get; set; }
        public int enemyHandSize { get; set; }
        public int CardAdvantage { get; set; }
    }
} 