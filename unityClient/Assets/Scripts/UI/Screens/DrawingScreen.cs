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
        [SerializeField] private UIDrawingCanvas drawingCanvas;
        [SerializeField] private Button submitButton;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI modifierText;
        
        [Header("Option Text Elements")]
        [SerializeField] private List<TextMeshProUGUI> optionTexts = new List<TextMeshProUGUI>(4);
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = new Color(0.5f, 1f, 0.5f);
        
        private int correctOptionIndex;
        private byte[] currentDrawingData;
        private bool hasSubmitted = false;
        private Drawing.DrawingModifierHandler modifierHandler;
        
        private void Awake()
        {
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(OnSubmitDrawing);
            }
            
            modifierHandler = GetComponentInChildren<Drawing.DrawingModifierHandler>();
            if (modifierHandler == null && drawingCanvas != null)
            {
                modifierHandler = drawingCanvas.GetComponent<Drawing.DrawingModifierHandler>();
            }
        }

        public void Setup(List<DrawingOption> options, int correctIndex, Game.Modifiers.ModifierData modifier = null)
        {
            correctOptionIndex = correctIndex;
            hasSubmitted = false;

            if (drawingCanvas != null)
            {
                drawingCanvas.ClearCanvas();
            }
            
            if (modifierText != null)
            {
                if (modifier != null)
                {
                    modifierText.gameObject.SetActive(true);
                    modifierText.text = modifier.name;
                }
                else
                {
                    modifierText.gameObject.SetActive(false);
                }
            }
            
            if (modifierHandler != null)
            {
                modifierHandler.ApplyModifier(modifier);
            }

            if (optionTexts.Count < options.Count)
            {
                Debug.LogError(
                    $"DrawingScreen: Not enough option text elements! Have {optionTexts.Count}, need {options.Count}");
                return;
            }

            for (int i = 0; i < options.Count && i < optionTexts.Count; i++)
            {
                if (optionTexts[i] != null)
                {
                    bool isBlindOptions = modifier != null && modifier.name == "Blind Options";
                    
                    if (isBlindOptions && i != correctIndex)
                    {
                        optionTexts[i].text = "???";
                        optionTexts[i].color = Color.gray;
                        optionTexts[i].fontStyle = FontStyles.Normal;
                    }
                    else
                    {
                        optionTexts[i].text = options[i].text;
                        
                        if (i == correctIndex)
                        {
                            optionTexts[i].color = highlightColor;
                            optionTexts[i].fontStyle = FontStyles.Bold;
                            optionTexts[i].text = "→ " + options[i].text + " ←";
                        }
                        else
                        {
                            optionTexts[i].color = normalColor;
                            optionTexts[i].fontStyle = FontStyles.Normal;
                        }
                    }
                }
            }

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
            UpdateTimerDisplay();
        }
        
        private void UpdateTimerDisplay()
        {
            if (timerText != null && GameController.Instance != null)
            {
                float timeRemaining = GameController.Instance.GetTimeRemaining();
                int seconds = Mathf.CeilToInt(timeRemaining);
                timerText.text = $"Time: {seconds:00}";
                
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
                    timerText.color = Color.black;
                }
            }
        }
        
        public void ForceSubmitDrawing()
        {
            if (!hasSubmitted)
            {
                OnSubmitDrawing();
            }
        }
        
        private void OnSubmitDrawing()
        {
            if (hasSubmitted) return;
            
            if (drawingCanvas == null)
            {
                Debug.LogError("DrawingScreen: drawingCanvas not assigned in inspector!");
                return;
            }
            
            hasSubmitted = true;
            currentDrawingData = drawingCanvas.GetDrawingData();
            
            if (GameController.Instance != null)
            {
                GameController.Instance.SubmitDrawing(currentDrawingData);
            }
        }
        
        private void OnClearDrawing()
        {
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
        }
    }
    
    public interface IDrawingCanvas
    {
        byte[] GetDrawingData();
        void ClearCanvas();
        void LoadDrawingData(byte[] data);
    }
}