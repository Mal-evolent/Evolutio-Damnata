using EnemyInteraction.Models;

namespace EnemyInteraction.Managers
{
    public class BoardStateCache
    {
        private BoardState _cachedState;
        private float _lastUpdateTime;
        private readonly float _cacheTimeout;

        public BoardStateCache(float cacheTimeout)
        {
            _cacheTimeout = cacheTimeout;
            _lastUpdateTime = 0;
            _cachedState = null;
        }

        /// <summary>
        /// Checks if the cached board state is still valid
        /// </summary>
        public bool IsValid(float currentTime)
        {
            return _cachedState != null && (currentTime - _lastUpdateTime <= _cacheTimeout);
        }

        /// <summary>
        /// Updates the cached board state
        /// </summary>
        public void UpdateCache(BoardState newState, float currentTime)
        {
            _cachedState = newState;
            _lastUpdateTime = currentTime;
        }

        /// <summary>
        /// Gets the currently cached board state
        /// </summary>
        public BoardState GetCachedState()
        {
            return _cachedState;
        }

        /// <summary>
        /// Invalidates the cached board state
        /// </summary>
        public void Invalidate()
        {
            _cachedState = null;
        }
    }
}
