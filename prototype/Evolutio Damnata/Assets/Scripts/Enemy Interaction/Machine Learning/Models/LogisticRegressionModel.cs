using UnityEngine;

namespace EnemyInteraction.MachineLearning
{
    public class LogisticRegressionModel
    {
        // Model parameters
        private float[] weights;
        private float bias;
        private readonly int inputSize;
        
        public LogisticRegressionModel(int inputSize)
        {
            this.inputSize = inputSize;
            
            // Initialize weights for logistic regression
            weights = new float[inputSize];
            for (int i = 0; i < inputSize; i++)
            {
                weights[i] = UnityEngine.Random.Range(-0.1f, 0.1f);
            }
            bias = UnityEngine.Random.Range(-0.1f, 0.1f);
        }
        
        // Simple logistic regression prediction
        public float Predict(float[] features)
        {
            float sum = bias;
            for (int i = 0; i < Mathf.Min(weights.Length, features.Length); i++)
            {
                sum += weights[i] * features[i];
            }
            return sum;
        }
        
        public void Train(float[] features, int label, float learningRate)
        {
            float prediction = Sigmoid(Predict(features));
            float error = label - prediction;

            bias += learningRate * error;
            for (int i = 0; i < Mathf.Min(weights.Length, features.Length); i++)
            {
                weights[i] += learningRate * error * features[i];
            }
        }
        
        public float Sigmoid(float x)
        {
            return 1f / (1f + Mathf.Exp(-x));
        }
        
        public float[] GetWeights()
        {
            return weights;
        }
    }
}
