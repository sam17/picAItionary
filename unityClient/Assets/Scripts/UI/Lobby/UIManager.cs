using TMPro;
using UnityEngine;

namespace UI.Lobby
{
    public class UIManager: MonoBehaviour
    {
        [SerializeField] private Lobby lobby;
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject startGameMenu;
        [SerializeField] private GameObject joinMenu;
        [SerializeField] private GameObject settingsScreen;
        [SerializeField] private GameObject unlockGameScreen;
        [SerializeField] private GameObject howToPlayScreen;
        [SerializeField] private GameObject localGameSettingsScreen;
        [SerializeField] private TMP_InputField joinCodeInputField;
        [SerializeField] private TMP_InputField nameInputField;

        public static UIManager Instance;
        
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
        
        public void ShowStartGameMenu()
        {
            Debug.Log("Showing Start Game Menu");
            mainMenu.SetActive(false);
            startGameMenu.SetActive(true);
        }
        
        public void ShowJoinMenu()
        {
            Debug.Log("Showing Join Menu");
            startGameMenu.SetActive(false);
            joinMenu.SetActive(true);
        }
        
        public void ShowSettingsScreen()
        {
            Debug.Log("Showing Settings Screen");
            mainMenu.SetActive(false);
            settingsScreen.SetActive(true);
        }
        
        public void ShowUnlockGameScreen()
        {
            Debug.Log("Showing Unlock Game Screen");
            mainMenu.SetActive(false);
            unlockGameScreen.SetActive(true);
        }
        
        public void ShowHowToPlayScreen()
        {
            Debug.Log("Showing How To Play Screen");
            mainMenu.SetActive(false);
            howToPlayScreen.SetActive(true);
        }
        
        public void OnStartLocalGame()
        {
            Debug.Log("Showing local game settings");
            ShowLocalGameSettingsScreen();
        }
        
        public void ShowLocalGameSettingsScreen()
        {
            Debug.Log("Showing Local Game Settings Screen");
            mainMenu.SetActive(false);
            startGameMenu.SetActive(false);
            localGameSettingsScreen.SetActive(true);
            
            var settingsScreen = localGameSettingsScreen.GetComponent<UI.Screens.LocalGameSettingsScreen>();
            if (settingsScreen != null)
            {
                settingsScreen.ShowScreen();
            }
        }

        public void OnJoinButtonClicked()
        {
            var joinCode = joinCodeInputField.text.Trim();
            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.Log("Join code is empty. Please enter a valid join code.");
                return;
            }
            ConnectionManager.Instance.OnJoinAsClient(joinCode);
        }
        
        public void StartLobbyForHost(string joinCode)
        {
            Debug.Log($"Starting lobby for host with join code: {joinCode}");
            lobby.StartLobbyForHost(joinCode);
            mainMenu.SetActive(false);
            startGameMenu.SetActive(false);
            joinMenu.SetActive(false);
        }
        
        public void StartLobbyForClient(string joinCode)
        {
            Debug.Log($"Starting lobby for client with join code: {joinCode}");
            lobby.StartLobbyForClient(joinCode);
            mainMenu.SetActive(false);
            startGameMenu.SetActive(false);
            joinMenu.SetActive(false);
        }
        
        public string GetPlayerName()
        {
            return nameInputField != null ? nameInputField.text.Trim() : "";
        }
        
        public void RefreshPlayerList()
        {
            if (lobby != null)
            {
                lobby.RefreshPlayerList();
            }
        }
        
        public void ReturnToMainMenu()
        {
            Debug.Log("Returning to main menu");
            mainMenu.SetActive(true);
            startGameMenu.SetActive(false);
            joinMenu.SetActive(false);
            settingsScreen.SetActive(false);
            unlockGameScreen.SetActive(false);
            howToPlayScreen.SetActive(false);
            localGameSettingsScreen.SetActive(false);
        }
        
        public void BackToStartGameMenu()
        {
            Debug.Log("Returning to Start Game Menu");
            joinMenu.SetActive(false);
            startGameMenu.SetActive(true);
        }
       
    }
}