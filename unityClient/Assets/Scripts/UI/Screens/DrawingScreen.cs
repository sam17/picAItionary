using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game;

namespace UI
{
    public class DrawingScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject drawingCanvas; // The actual drawing area
        [SerializeField] private Button submitButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private TextMeshProUGUI timerText;
        
        [Header("Option Text Elements")]
        [SerializeField] private List<TextMeshProUGUI> optionTexts = new List<TextMeshProUGUI>(4);
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(0.5f, 1f, 0.5f);
        
        private int correctOptionIndex;
        private byte[] currentDrawingData;
        
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
            
            // Validate we have enough text elements
            if (optionTexts.Count < options.Count)
            {
                Debug.LogError($"DrawingScreen: Not enough option text elements! Have {optionTexts.Count}, need {options.Count}");
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
            
            Debug.Log($"DrawingScreen: Setup with {options.Count} options, correct: {options[correctIndex].text}");
        }
        
        private void OnSubmitDrawing()
        {
            // Get drawing data from canvas
            var drawingComponent = drawingCanvas?.GetComponent<IDrawingCanvas>();
            if (drawingComponent != null)
            {
                currentDrawingData = drawingComponent.GetDrawingData();
            }
            else
            {
                // Create dummy data for testing
                currentDrawingData = new byte[] { 1, 2, 3, 4, 5 };
            }
            
            Debug.Log($"DrawingScreen: Submitting drawing ({currentDrawingData.Length} bytes)");
            
            // Submit to game controller
            if (GameController.Instance != null)
            {
                GameController.Instance.SubmitDrawing(currentDrawingData);
            }
        }
        
        private void OnClearDrawing()
        {
            // Clear the canvas
            var drawingComponent = drawingCanvas?.GetComponent<IDrawingCanvas>();
            if (drawingComponent != null)
            {
                drawingComponent.ClearCanvas();
            }
            
            Debug.Log("DrawingScreen: Canvas cleared");
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