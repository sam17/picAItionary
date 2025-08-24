using System.Collections.Generic;
using Drawing;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game;

namespace UI
{
    public class GuessingScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private DrawingDisplayCanvas drawingDisplay;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private GameObject halfHiddenOverlay;
        
        [Header("Option Buttons")]
        [SerializeField] private List<Button> optionButtons = new List<Button>(4);
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(0.8f, 0.8f, 1f);
        [SerializeField] private Color disabledColor = Color.gray;
        
        private int selectedOption = -1;
        private bool hasSubmitted = false;
        
        private void Start()
        {
            if (instructionText != null)
            {
                instructionText.text = "What do you think was drawn?";
            }
        }
        
        public void Setup(byte[] drawingData, List<DrawingOption> options, Game.Modifiers.ModifierData modifier = null)
        {
            selectedOption = -1;
            hasSubmitted = false;
            
            HandleHalfHiddenModifier(modifier);
            
            if (drawingDisplay == null)
            {
                Debug.LogError("GuessingScreen: drawingDisplay not assigned in inspector!");
                return;
            }
            
            if (drawingData == null || drawingData.Length == 0)
            {
                Debug.LogError("GuessingScreen: No drawing data provided!");
                return;
            }
            
            drawingDisplay.LoadDrawingData(drawingData);
            
            if (optionButtons.Count < options.Count)
            {
                Debug.LogError($"GuessingScreen: Not enough option buttons! Have {optionButtons.Count}, need {options.Count}");
                return;
            }
            
            for (int i = 0; i < options.Count && i < optionButtons.Count; i++)
            {
                if (optionButtons[i] != null)
                {
                    int optionIndex = i;
                    
                    var textComponent = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        textComponent.text = options[i].text;
                    }
                    
                    var image = optionButtons[i].GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = normalColor;
                    }
                    
                    optionButtons[i].interactable = true;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(optionIndex));
                    
                    optionButtons[i].gameObject.SetActive(true);
                }
            }
            
            for (int i = options.Count; i < optionButtons.Count; i++)
            {
                if (optionButtons[i] != null)
                {
                    optionButtons[i].gameObject.SetActive(false);
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
                
                if (timeRemaining <= 5f)
                {
                    timerText.color = Color.red;
                }
                else if (timeRemaining <= 10f)
                {
                    timerText.color = Color.yellow;
                }
                else
                {
                    timerText.color = Color.white;
                }
            }
        }
        
        private void OnOptionSelected(int index)
        {
            if (hasSubmitted) return;
            
            for (int i = 0; i < optionButtons.Count; i++)
            {
                if (optionButtons[i] != null)
                {
                    var image = optionButtons[i].GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = (i == index) ? selectedColor : normalColor;
                    }
                }
            }
            
            selectedOption = index;
            SubmitGuess();
        }
        
        private void SubmitGuess()
        {
            if (hasSubmitted || selectedOption < 0) return;
            
            hasSubmitted = true;
            
            if (GameController.Instance != null)
            {
                GameController.Instance.SubmitGuess(selectedOption);
            }
            
            if (instructionText != null)
            {
                instructionText.text = "Guess submitted! Waiting for others...";
            }
            
            foreach (var button in optionButtons)
            {
                if (button != null)
                {
                    button.interactable = false;
                    var image = button.GetComponent<Image>();
                    if (image != null && image.color != selectedColor)
                    {
                        image.color = disabledColor;
                    }
                }
            }
        }
        
        private void HandleHalfHiddenModifier(Game.Modifiers.ModifierData modifier)
        {
            if (halfHiddenOverlay != null)
            {
                if (modifier != null && modifier.name == "Half Hidden")
                {
                    halfHiddenOverlay.SetActive(true);
                    bool hideLeft = Random.Range(0, 2) == 0;
                    
                    RectTransform overlayRect = halfHiddenOverlay.GetComponent<RectTransform>();
                    if (overlayRect != null && drawingDisplay != null)
                    {
                        // Make the overlay a child of the drawing display if it isn't already
                        if (halfHiddenOverlay.transform.parent != drawingDisplay.transform)
                        {
                            halfHiddenOverlay.transform.SetParent(drawingDisplay.transform, false);
                        }
                        
                        // Position the overlay to cover half of the drawing image
                        if (hideLeft)
                        {
                            // Hide left half
                            overlayRect.anchorMin = new Vector2(0, 0);
                            overlayRect.anchorMax = new Vector2(0.5f, 1);
                        }
                        else
                        {
                            // Hide right half
                            overlayRect.anchorMin = new Vector2(0.5f, 0);
                            overlayRect.anchorMax = new Vector2(1, 1);
                        }
                        
                        // Reset offsets to fill the anchored area
                        overlayRect.offsetMin = Vector2.zero;
                        overlayRect.offsetMax = Vector2.zero;
                    }
                }
                else
                {
                    halfHiddenOverlay.SetActive(false);
                }
            }
        }
        
        private void OnDestroy()
        {
            foreach (var button in optionButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }
        }
    }
}