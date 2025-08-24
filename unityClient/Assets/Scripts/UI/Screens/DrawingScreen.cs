using System.Collections.Generic;
using Drawing;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game;

namespace UI
{
    public class DrawingScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private UIDrawingCanvas drawingCanvas; // The actual drawing area
        [SerializeField] private Button submitButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private TextMeshProUGUI timerText;
        
        [Header("Option Text Elements")]
        [SerializeField] private List<TextMeshProUGUI> optionTexts = new List<TextMeshProUGUI>(4);
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(0.5f, 1f, 0.5f);
        
        private int correctOptionIndex;
        private byte[] currentDrawingData;
        private bool hasSubmitted = false;
        
        private void Awake()
        {
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(OnSubmitDrawing);
            }
            
            if (clearButton != null)
            {
                clearButton.onClick.AddListener(OnClearDrawing);
            }
        }

        public void Setup(List<DrawingOption> options, int correctIndex)
        {
            correctOptionIndex = correctIndex;
            hasSubmitted = false; // Reset submission flag

            // Clear the canvas for a fresh start
            if (drawingCanvas != null)
            {
                drawingCanvas.ClearCanvas();
            }

            // Validate we have enough text elements
            if (optionTexts.Count < options.Count)
            {
                Debug.LogError(
                    $"DrawingScreen: Not enough option text elements! Have {optionTexts.Count}, need {options.Count}");
                return;
            }

            // Update the text elements with options
            for (int i = 0; i < options.Count && i < optionTexts.Count; i++)
            {
                if (optionTexts[i] != null)
                {
                    optionTexts[i].text = options[i].text;

                    // Highlight the correct option
                    if (i == correctIndex)
                    {
                        optionTexts[i].color = highlightColor;
                        optionTexts[i].fontStyle = FontStyles.Bold;

                        // Optionally add a marker
                        optionTexts[i].text = "→ " + options[i].text + " ←";
                    }
                    else
                    {
                        optionTexts[i].color = normalColor;
                        optionTexts[i].fontStyle = FontStyles.Normal;
                    }
                }
            }

            // Hide any extra text elements
            for (int i = options.Count; i < optionTexts.Count; i++)
            {
                if (optionTexts[i] != null)
                {
                    optionTexts[i].gameObject.SetActive(false);
                }
            }
        }

        private void Update()
        {
            // Update timer display using GameController's centralized timer
            UpdateTimerDisplay();
        }
        
        private void UpdateTimerDisplay()
        {
            if (timerText != null && GameController.Instance != null)
            {
                float timeRemaining = GameController.Instance.GetTimeRemaining();
                int seconds = Mathf.CeilToInt(timeRemaining);
                timerText.text = $"Time: {seconds:00}";
                
                // Change color when time is running out
                if (timeRemaining <= 10f)
                {
                    timerText.color = Color.red;
                }
                else if (timeRemaining <= 20f)
                {
                    timerText.color = Color.yellow;
                }
                else
                {
                    timerText.color = Color.white;
                }
            }
        }
        
        public void ForceSubmitDrawing()
        {
            if (!hasSubmitted)
            {
                Debug.Log("DrawingScreen: Force submitting drawing due to timer expiration");
                OnSubmitDrawing();
            }
        }
        
        private void OnSubmitDrawing()
        {
            if (hasSubmitted)
            {
                Debug.Log("DrawingScreen: Already submitted, ignoring");
                return;
            }
            
            if (drawingCanvas == null)
            {
                Debug.LogError("DrawingScreen: drawingCanvas not assigned in inspector!");
                return;
            }
            
            hasSubmitted = true;
            currentDrawingData = drawingCanvas.GetDrawingData();
            Debug.Log($"DrawingScreen: Submitting drawing with {currentDrawingData?.Length ?? 0} bytes");
            
            // Submit to game controller
            if (GameController.Instance != null)
            {
                GameController.Instance.SubmitDrawing(currentDrawingData);
            }
        }
        
        private void OnClearDrawing()
        {
            // Clear the canvas
            if (drawingCanvas != null)
            {
                drawingCanvas.ClearCanvas();
            }
        }
        
        private void OnDestroy()
        {
            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(OnSubmitDrawing);
            }
            
            if (clearButton != null)
            {
                clearButton.onClick.RemoveListener(OnClearDrawing);
            }
        }
    }
    
    // Interface for drawing canvas implementation
    public interface IDrawingCanvas
    {
        byte[] GetDrawingData();
        void ClearCanvas();
        void LoadDrawingData(byte[] data);
    }
}