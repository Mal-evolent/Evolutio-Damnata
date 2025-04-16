using EnemyInteraction.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnemyInteraction.MachineLearning
{
    public class PlayerBehaviorPredictor : IPlayerBehaviorPredictor
    {
        // Component references
        private FeatureExtractor featureExtractor;
        private NeuralNetwork neuralNetwork;
        private LogisticRegressionModel logisticModel;
        private HeuristicPredictor heuristicPredictor;

        // Training data storage
        private List<float[]> featuresHistory = new List<float[]>();
        private List<int> labelsHistory = new List<int>();

        // Configuration
        private const int MaxHistorySize = 100;
        private const float LearningRate = 0.01f;

        // Current influence of the model (0 = none, 1 = full)
        private float modelInfluence = 0f;
        private const float InfluenceGrowthRate = 0.05f;

        // Reference to card history system
        private CardHistory cardHistorySystem;

        public PlayerBehaviorPredictor()
        {
            // Find the CardHistory system
            cardHistorySystem = CardHistory.Instance;
            if (cardHistorySystem == null)
            {
                Debug.LogWarning("[PlayerBehaviorPredictor] CardHistory system not found. Running with limited features.");
            }

            // Initialize components
            featureExtractor = new FeatureExtractor();
            int inputSize = featureExtractor.GetInputSize();
            neuralNetwork = new NeuralNetwork(inputSize);
            logisticModel = new LogisticRegressionModel(inputSize);
            heuristicPredictor = new HeuristicPredictor();
        }

        // Main prediction method
        public float PredictPlayerAction(BoardState boardState, int actionType)
        {
            float[] features = featureExtractor.ExtractFeatures(boardState, actionType);

            float heuristicPrediction = heuristicPredictor.GetHeuristicPrediction(boardState, actionType);
            float logisticPrediction = logisticModel.Sigmoid(logisticModel.Predict(features));
            float nnPrediction = neuralNetwork.Predict(features);

            float cardHistoryDataAvailable = cardHistorySystem != null ?
                Mathf.Min(1f, cardHistorySystem.GetTotalCardsPlayed() / 20f) : 0f;

            float nnWeight = Mathf.Min(0.7f, modelInfluence * (0.6f + cardHistoryDataAvailable * 0.2f));
            float logisticWeight = Mathf.Min(0.4f, modelInfluence * 0.2f);
            float heuristicWeight = 1f - (nnWeight + logisticWeight);

            return (nnPrediction * nnWeight) +
                   (logisticPrediction * logisticWeight) +
                   (heuristicPrediction * heuristicWeight);
        }

        // Update model with new observed player behavior
        public void UpdateModel(BoardState boardState, int actualAction)
        {
            float[] features = featureExtractor.ExtractFeatures(boardState, actualAction);

            featuresHistory.Add(features);
            labelsHistory.Add(actualAction);
            if (featuresHistory.Count > MaxHistorySize)
            {
                featuresHistory.RemoveAt(0);
                labelsHistory.RemoveAt(0);
            }

            logisticModel.Train(features, actualAction, LearningRate);
            neuralNetwork.Train(features, actualAction, LearningRate * 0.5f);

            modelInfluence = Mathf.Min(1f, modelInfluence + InfluenceGrowthRate);
        }

        // Handle phase changes for adaptive learning
        public void PhaseSwitched(CombatPhase newPhase)
        {
            switch (newPhase)
            {
                case CombatPhase.EnemyPrep:
                    modelInfluence = Mathf.Max(0.2f, modelInfluence * 0.8f);
                    break;
                case CombatPhase.EnemyCombat:
                    modelInfluence = Mathf.Min(1f, modelInfluence * 1.1f);
                    break;
                case CombatPhase.CleanUp:
                    if (featuresHistory.Count > 5)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            BatchTraining();
                        }
                    }
                    break;
            }

            if (featuresHistory.Count > 20 && newPhase == CombatPhase.CleanUp)
            {
                LogFeatureImportance();
            }
        }

        // Batch training on historical data
        private void BatchTraining()
        {
            for (int i = 0; i < featuresHistory.Count; i++)
            {
                logisticModel.Train(featuresHistory[i], labelsHistory[i], LearningRate);
                neuralNetwork.Train(featuresHistory[i], labelsHistory[i], LearningRate * 0.5f);
            }
        }

        // Log feature importance based on weights (for debugging)
        private void LogFeatureImportance()
        {
            // Create a list to hold feature importance values
            List<KeyValuePair<string, float>> featureImportance = new List<KeyValuePair<string, float>>();

            // Updated feature names with enum-relevant terminology
            string[] featureNames = new string[]
            {
                "PlayerHealthRatio", "EnemyHealthRatio",
                "PlayerBoardControl", "EnemyBoardControl",
                "TurnCount", "CardAdvantage",
                "IsAttackAction", "IsSpellAction",
                "IsPlayerPrep", "IsPlayerCombat", "IsEnemyPrep", "IsEnemyCombat", "IsCleanUp",
                "IsPlayerTurn", "IsPlayerFirstNextTurn",
                // Card history features with better naming
                "SpellEffectUsageRatio", "CurrentTurnCardCount", "AggressiveKeywordTrend",
                "RecentTurnActivity", "CardPlayBalance", "CurrentPhaseActivity",
                "DefensiveKeywordPreference", "ResourceManipulationPreference"
            };

            float[] weights = logisticModel.GetWeights();

            // Add absolute weight values as importance scores
            for (int i = 0; i < Mathf.Min(weights.Length, featureNames.Length); i++)
            {
                featureImportance.Add(new KeyValuePair<string, float>(featureNames[i], Mathf.Abs(weights[i])));
            }

            // Sort by importance (descending)
            featureImportance.Sort((a, b) => b.Value.CompareTo(a.Value));

            // Log the top features
            Debug.Log("[PlayerBehaviorPredictor] Top feature importance:");
            for (int i = 0; i < Mathf.Min(5, featureImportance.Count); i++)
            {
                Debug.Log($"{i + 1}. {featureImportance[i].Key}: {featureImportance[i].Value}");
            }
        }
    }
}
