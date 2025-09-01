using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Screens
{
    public class LocalGameSettingsScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Toggle modifierToggle;
        [SerializeField] private TMP_Dropdown roundsDropdown;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button backButton;
        
        [Header("Settings")]
        [SerializeField] private int[] roundOptions = { 3, 5, 10, 15, 20 };
        
        private bool useModifiers = false;
        private int selectedRounds = 5;
        
        private void Start()
        {
            SetupRoundsDropdown();
            
            if (modifierToggle != null)
            {
                modifierToggle.onValueChanged.AddListener(OnModifierToggleChanged);
                modifierToggle.isOn = false;
            }
            
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }
            
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }
            
            if (roundsDropdown != null)
            {
                roundsDropdown.onValueChanged.AddListener(OnRoundsChanged);
            }
        }
        
        private void SetupRoundsDropdown()
        {
            if (roundsDropdown == null) return;
            
            roundsDropdown.ClearOptions();
            
            foreach (int rounds in roundOptions)
            {
                roundsDropdown.options.Add(new TMP_Dropdown.OptionData($"{rounds}"));
            }
            
            // Set default to 5 rounds (index 1)
            roundsDropdown.value = 1;
            selectedRounds = roundOptions[1];
            roundsDropdown.RefreshShownValue();
        }
        
        private void OnModifierToggleChanged(bool isOn)
        {
            useModifiers = isOn;
            Debug.Log($"Modifiers set to: {(useModifiers ? "ON" : "OFF")}");
        }
        
        private void OnRoundsChanged(int index)
        {
            if (index >= 0 && index < roundOptions.Length)
            {
                selectedRounds = roundOptions[index];
                Debug.Log($"Selected rounds: {selectedRounds}");
            }
        }
        
        private void OnStartGameClicked()
        {
            Debug.Log($"Starting local game with {selectedRounds} rounds, Modifiers: {useModifiers}");
            
            // Store settings for game session
            PlayerPrefs.SetInt("LocalGameRounds", selectedRounds);
            PlayerPrefs.SetInt("LocalGameModifiers", useModifiers ? 1 : 0);
            PlayerPrefs.Save();
            
            // Start the local game with these settings
            ConnectionManager.Instance.OnStartLocal();
        }
        
        private void OnBackClicked()
        {
            // Return to previous menu
            if (UI.Lobby.UIManager.Instance != null)
            {
                UI.Lobby.UIManager.Instance.ReturnToMainMenu();
            }
            
            gameObject.SetActive(false);
        }
        
        public void ShowScreen()
        {
            gameObject.SetActive(true);
            
            // Reset to default values
            if (modifierToggle != null)
            {
                modifierToggle.isOn = false;
            }
            
            if (roundsDropdown != null)
            {
                roundsDropdown.value = 1; // Default to 5 rounds
            }
        }
        
        private void OnDestroy()
        {
            if (modifierToggle != null)
            {
                modifierToggle.onValueChanged.RemoveListener(OnModifierToggleChanged);
            }
            
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveListener(OnStartGameClicked);
            }
            
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(OnBackClicked);
            }
            
            if (roundsDropdown != null)
            {
                roundsDropdown.onValueChanged.RemoveListener(OnRoundsChanged);
            }
        }
    }
}