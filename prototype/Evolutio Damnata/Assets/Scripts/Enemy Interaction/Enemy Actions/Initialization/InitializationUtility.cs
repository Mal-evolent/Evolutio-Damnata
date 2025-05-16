using System.Collections;
using UnityEngine;
using EnemyInteraction.Services;

namespace EnemyInteraction.Utilities
{
    public static class InitializationUtility
    {
        public static IEnumerator WaitForInitialization(bool isInitialized, float timeout = 3f)
        {
            if (!isInitialized)
            {
                Debug.Log("[InitializationUtility] Waiting for initialization to complete...");
                float waitTime = 0f;
                while (!isInitialized && waitTime < timeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    waitTime += 0.1f;
                }
            }
        }
        
        public static IEnumerator WaitForAIServicesInitialization(float timeout = 3f)
        {
            if (!AIServices.IsInitialized)
            {
                Debug.Log("[InitializationUtility] Waiting for AIServices to initialize...");
                float waitTime = 0f;
                while (!AIServices.IsInitialized && waitTime < timeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    waitTime += 0.1f;
                }
            }
        }
    }
}
