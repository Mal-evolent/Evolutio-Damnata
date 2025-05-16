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
            // Check if AIServices.Instance exists
            if (AIServices.Instance == null)
            {
                Debug.Log("[InitializationUtility] AIServices instance not found");
                yield break;
            }

            // Use the instance property instead of a static property
            if (!AIServices.Instance.IsInitialized)
            {
                Debug.Log("[InitializationUtility] Waiting for AIServices to initialize...");
                float waitTime = 0f;
                while (AIServices.Instance != null && !AIServices.Instance.IsInitialized && waitTime < timeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    waitTime += 0.1f;
                }
            }

            if (AIServices.Instance == null || !AIServices.Instance.IsInitialized)
            {
                Debug.LogWarning("[InitializationUtility] AIServices initialization timed out or instance was destroyed");
            }
            else
            {
                Debug.Log("[InitializationUtility] AIServices initialization completed");
            }
        }
    }
}
