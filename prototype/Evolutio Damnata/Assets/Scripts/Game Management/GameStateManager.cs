using UnityEngine;

namespace GameManagement
{
    /// <summary>
    /// Static class that manages persistent game state between scenes
    /// </summary>
    public static class GameStateManager
    {
        private static int _playerHealth = 0;
        private static bool _hasPlayerHealthData = false;
        private static int _defaultPlayerHealth = 30;

        /// <summary>
        /// Save the player health for the next game session
        /// </summary>
        /// <param name="health">Current player health to save</param>
        public static void SavePlayerHealth(int health)
        {
            _playerHealth = health;
            _hasPlayerHealthData = true;
            Debug.Log($"[GameStateManager] Saved player health: {_playerHealth}");
        }

        /// <summary>
        /// Get the saved player health or default value if none available
        /// </summary>
        /// <returns>The saved player health or default value</returns>
        public static int GetPlayerHealth()
        {
            if (!_hasPlayerHealthData)
            {
                Debug.Log($"[GameStateManager] No saved health data, returning default: {_defaultPlayerHealth}");
                return _defaultPlayerHealth;
            }

            Debug.Log($"[GameStateManager] Returning saved player health: {_playerHealth}");
            return _playerHealth;
        }

        /// <summary>
        /// Reset the stored player health data
        /// </summary>
        public static void ResetPlayerHealth()
        {
            _playerHealth = _defaultPlayerHealth;
            _hasPlayerHealthData = false;
            Debug.Log("[GameStateManager] Player health data reset");
        }
    }
}
