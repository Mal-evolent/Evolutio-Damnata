using EnemyInteraction.Models;
using System.Collections.Generic;
using UnityEngine;

namespace EnemyInteraction.MachineLearning
{
    public interface IPlayerBehaviorPredictor
    {
        /// <summary>
        /// Predicts the likelihood of the player taking a specific action
        /// </summary>
        float PredictPlayerAction(BoardState boardState, int actionType);

        /// <summary>
        /// Updates the model with new observed player behavior
        /// </summary>
        void UpdateModel(BoardState boardState, int actualAction);

        /// <summary>
        /// Handle phase changes for adaptive learning
        /// </summary>
        void PhaseSwitched(CombatPhase newPhase);
    }
}
