using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnemyInteraction.Models;
using EnemyInteraction.Evaluation;
using EnemyInteraction.Managers.Evaluation;
using EnemyInteraction.Managers.Targeting;
using EnemyInteraction.Managers.Execution;
using EnemyInteraction.Extensions;
using EnemyInteraction.Interfaces;
using EnemyInteraction.Services;
using EnemyInteraction.Utilities;
using UnityEngine.SceneManagement;

namespace EnemyInteraction.Managers
{
    public class CardPlayManager : MonoBehaviour, ICardPlayManager
    {
        [SerializeField] private CardPlaySettings _settings;
        private IDependencyProvider _dependencyProvider;
        private ICardPlayInitializer _initializer;
        private ICardPlayStrategist _strategist;

        public static CardPlayManager Instance { get; private set; }

        private void Awake()
        {
            // Implement singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[CardPlayManager] Another instance already exists, destroying this one");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // Setup dependencies
            _dependencyProvider = new DependencyProvider(this);
            _initializer = new CardPlayInitializer(_dependencyProvider, _settings);
            _strategist = new CardPlayStrategist(_dependencyProvider, _settings);

            StartCoroutine(_initializer.Initialize());
        }

        private void OnEnable()
        {
            // Register for scene load events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            // Unregister to prevent memory leaks
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[CardPlayManager] Scene loaded: {scene.name}");
            StartCoroutine(_initializer.ReacquireSceneReferences());
        }

        public IEnumerator PlayCards()
        {
            Debug.Log("[CardPlayManager] Starting card play sequence...");
            yield return _strategist.ExecuteCardPlayStrategy();
        }
    }
}
