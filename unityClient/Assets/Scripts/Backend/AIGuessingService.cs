using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Game;

namespace Backend
{
    public class AIGuessingService : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string backendUrl = "http://localhost:5000";
        [SerializeField] private float requestTimeout = 10f;
        [SerializeField] private bool useMockResponse = true; // For testing without backend
        
        public static AIGuessingService Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Send drawing to AI backend and get its guess
        /// </summary>
        public void GetAIGuess(byte[] drawingData, List<DrawingOption> options, Action<int> onGuessReceived)
        {
            if (useMockResponse)
            {
                // Mock response for testing
                StartCoroutine(MockAIGuess(options, onGuessReceived));
            }
            else
            {
                // Real backend call
                StartCoroutine(SendDrawingToBackend(drawingData, options, onGuessReceived));
            }
        }
        
        private IEnumerator MockAIGuess(List<DrawingOption> options, Action<int> onGuessReceived)
        {
            // Simulate network delay
            yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 3f));
            
            // Random guess for now (in real implementation, would be smarter)
            // Could bias towards correct answer to make AI seem smart
            int guess = UnityEngine.Random.Range(0, options.Count);
            
            Debug.Log($"AIGuessingService: Mock AI guessed option {guess}");
            onGuessReceived?.Invoke(guess);
        }
        
        private IEnumerator SendDrawingToBackend(byte[] drawingData, List<DrawingOption> options, Action<int> onGuessReceived)
        {
            // Prepare request data
            var requestData = new DrawingAnalysisRequest
            {
                drawing = Convert.ToBase64String(drawingData),
                options = new string[options.Count]
            };
            
            for (int i = 0; i < options.Count; i++)
            {
                requestData.options[i] = options[i].text;
            }
            
            string jsonData = JsonUtility.ToJson(requestData);
            
            // Create request
            using (UnityWebRequest request = new UnityWebRequest($"{backendUrl}/api/analyze-drawing", "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = (int)requestTimeout;
                
                // Send request
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<DrawingAnalysisResponse>(request.downloadHandler.text);
                        Debug.Log($"AIGuessingService: AI guessed option {response.guessIndex} with confidence {response.confidence}");
                        onGuessReceived?.Invoke(response.guessIndex);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"AIGuessingService: Failed to parse response: {e.Message}");
                        // Fallback to random guess
                        onGuessReceived?.Invoke(UnityEngine.Random.Range(0, options.Count));
                    }
                }
                else
                {
                    Debug.LogError($"AIGuessingService: Request failed: {request.error}");
                    // Fallback to random guess
                    onGuessReceived?.Invoke(UnityEngine.Random.Range(0, options.Count));
                }
            }
        }
        
        [Serializable]
        private class DrawingAnalysisRequest
        {
            public string drawing; // Base64 encoded drawing data
            public string[] options;
        }
        
        [Serializable]
        private class DrawingAnalysisResponse
        {
            public int guessIndex;
            public float confidence;
            public string guessText;
        }
    }
}