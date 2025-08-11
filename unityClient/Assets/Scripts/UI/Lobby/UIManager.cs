using UnityEngine;

namespace UI.Lobby
{
    public class UIManager: MonoBehaviour
    {
        [SerializeField] private Lobby lobby;
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject joinMenu;

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
        
        
        public void StartLobbyForHost(string joinCode)
        {
            Debug.Log($"Starting lobby for host with join code: {joinCode}");
            lobby.StartLobbyForHost(joinCode);
            mainMenu.SetActive(false);
            joinMenu.SetActive(false);
        }
       
    }
}