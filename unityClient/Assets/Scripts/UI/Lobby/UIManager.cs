using TMPro;
using UnityEngine;

namespace UI.Lobby
{
    public class UIManager: MonoBehaviour
    {
        [SerializeField] private Lobby lobby;
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject joinMenu;
        [SerializeField] private TMP_InputField joinCodeInputField;

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
        
        public void ShowJoinMenu()
        {
            Debug.Log("Showing Join Menu");
            mainMenu.SetActive(false);
            joinMenu.SetActive(true);
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
            joinMenu.SetActive(false);
        }
        
        public void StartLobbyForClient(string joinCode)
        {
            Debug.Log($"Starting lobby for client with join code: {joinCode}");
            lobby.StartLobbyForClient(joinCode);
            mainMenu.SetActive(false);
            joinMenu.SetActive(false);
        }
       
    }
}