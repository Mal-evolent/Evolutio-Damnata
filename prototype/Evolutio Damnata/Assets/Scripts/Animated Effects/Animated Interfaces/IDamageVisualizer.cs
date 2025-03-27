using UnityEngine;

public interface IDamageVisualizer
{
    GameObject CreateDamageNumber(EntityManager target, float damageNumber, Vector3 position, GameObject prefab);
    GameObject CreateHealingNumber(EntityManager target, float healNumber, Vector3 position, GameObject prefab);
}