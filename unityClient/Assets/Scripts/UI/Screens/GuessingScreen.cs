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
        [SerializeField] private DrawingDisplayCanvas drawingDisplay; // Component to display the drawing
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private TextMeshProUGUI timerText;
        
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
        
        public void Setup(byte[] drawingData, List<DrawingOption> options)
        {
            selectedOption = -1;
            hasSubmitted = false;
            
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