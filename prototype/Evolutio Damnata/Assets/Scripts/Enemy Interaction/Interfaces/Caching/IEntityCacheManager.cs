using System.Collections.Generic;
using UnityEngine;

namespace EnemyInteraction.Interfaces
{
    public interface IEntityCacheManager
    {
        void BuildEntityManagerCache();
        void RefreshEntityCaches();
        List<EntityManager> CachedPlayerEntities { get; }
        List<EntityManager> CachedEnemyEntities { get; }
        Dictionary<GameObject, EntityManager> EntityManagerCache { get; }
        List<EntityManager> GetValidEntities(IEnumerable<GameObject> source, bool checkAttackLimiter);
    }
}