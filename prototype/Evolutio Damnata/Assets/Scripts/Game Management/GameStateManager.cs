using UnityEngine;

namespace GameManagement
{
    public static class GameStateManager
    {
        private static bool isCombatActive = false;

        public static bool IsCombatActive
        {
            get => isCombatActive;
            set
            {
                if (isCombatActive != value)
                {
                    isCombatActive = value;
                    Debug.Log($"[GameState] Combat active state changed to: {value}");
                }
            }
        }
    }
}
