using UnityEngine;

namespace EnemyInteraction.MachineLearning
{
    public class NeuralNetwork
    {
        // Model parameters for the neural network
        private const int HiddenNeurons = 5;
        private float[,] hiddenWeights;
        private float[] hiddenBiases;
        private float[] outputWeights;
        private float outputBias;
        
        private readonly int inputSize;
        
        public NeuralNetwork(int inputSize)
        {
            this.inputSize = inputSize;
            InitializeNeuralNetwork();
        }
        
        // Initialize the neural network weights and biases
        private void InitializeNeuralNetwork()
        {
            hiddenWeights = new float[inputSize, HiddenNeurons];
            hiddenBiases = new float[HiddenNeurons];
            outputWeights = new float[HiddenNeurons];

            float hiddenScale = Mathf.Sqrt(2f / (inputSize + HiddenNeurons));
            float outputScale = Mathf.Sqrt(2f / (HiddenNeurons + 1));

            for (int i = 0; i < inputSize; i++)
            {
                for (int h = 0; h < HiddenNeurons; h++)
                {
                    hiddenWeights[i, h] = UnityEngine.Random.Range(-hiddenScale, hiddenScale);
                }
            }

            for (int h = 0; h < HiddenNeurons; h++)
            {
                hiddenBiases[h] = UnityEngine.Random.Range(-0.1f, 0.1f);
                outputWeights[h] = UnityEngine.Random.Range(-outputScale, outputScale);
            }

            outputBias = UnityEngine.Random.Range(-0.1f, 0.1f);
        }
        
        // Neural Network prediction method
        public float Predict(float[] features)
        {
            float[] hiddenOutputs = new float[HiddenNeurons];

            for (int h = 0; h < HiddenNeurons; h++)
            {
                float sum = hiddenBiases[h];
                for (int i = 0; i < Mathf.Min(inputSize, features.Length); i++)
                {
                    sum += features[i] * hiddenWeights[i, h];
                }
                hiddenOutputs[h] = Sigmoid(sum);
            }

            float output = outputBias;
            for (int h = 0; h < HiddenNeurons; h++)
            {
                output += hiddenOutputs[h] * outputWeights[h];
            }

            return Sigmoid(output);
        }
        
        // Training for the neural network
        public void Train(float[] features, int actualLabel, float learningRate)
        {
            float target = actualLabel;

            // Forward pass
            float[] hiddenOutputs = new float[HiddenNeurons];

            for (int h = 0; h < HiddenNeurons; h++)
            {
                float sum = hiddenBiases[h];
                for (int i = 0; i < Mathf.Min(inputSize, features.Length); i++)
                {
                    sum += features[i] * hiddenWeights[i, h];
                }
                hiddenOutputs[h] = Sigmoid(sum);
            }

            float output = outputBias;
            for (int h = 0; h < HiddenNeurons; h++)
            {
                output += hiddenOutputs[h] * outputWeights[h];
            }
            float prediction = Sigmoid(output);

            // Backpropagation
            float outputError = target - prediction;
            float outputDelta = outputError * prediction * (1 - prediction);

            // Update weights
            for (int h = 0; h < HiddenNeurons; h++)
            {
                outputWeights[h] += learningRate * outputDelta * hiddenOutputs[h];
            }
            outputBias += learningRate * outputDelta;

            for (int h = 0; h < HiddenNeurons; h++)
            {
                float hiddenError = outputWeights[h] * outputDelta;
                float hiddenDelta = hiddenError * hiddenOutputs[h] * (1 - hiddenOutputs[h]);

                for (int i = 0; i < Mathf.Min(inputSize, features.Length); i++)
                {
                    hiddenWeights[i, h] += learningRate * hiddenDelta * features[i];
                }
                hiddenBiases[h] += learningRate * hiddenDelta;
            }
        }
        
        private float Sigmoid(float x)
        {
            return 1f / (1f + Mathf.Exp(-x));
        }
    }
}
