using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("UI Panels")]
    [SerializeField] private MainMenuUI mainMenuUI;
    [SerializeField] private LobbyUI lobbyUI;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Only call DontDestroyOnLoad if this is a root object
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }
    
    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            mainMenuUI = FindObjectOfType<MainMenuUI>();
        }
        else if (scene.name == "Game")
        {
            lobbyUI = null;
        }
    }
    
    public void TransitionToLobby(bool isHost, string roomCode)
    {
        if (lobbyUI != null)
        {
            lobbyUI.Initialize(isHost, roomCode);
        }
    }
    
    public void TransitionToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    
    public void TransitionToGame()
    {
        if (Unity.Netcode.NetworkManager.Singleton.IsHost)
        {
            Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
    }
}