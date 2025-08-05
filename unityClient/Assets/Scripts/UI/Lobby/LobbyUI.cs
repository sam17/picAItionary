using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    
    private bool isHost;
    private bool isReady;
    private Dictionary<ulong, GameObject> playerListItems = new Dictionary<ulong, GameObject>();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        readyButton.onClick.AddListener(OnReadyButtonClicked);
        startGameButton.onClick.AddListener(OnStartGameClicked);
        leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        
        GameNetworkManager.Instance.OnPlayerConnected += UpdatePlayerList;
        GameNetworkManager.Instance.OnPlayerDisconnected += UpdatePlayerList;
    }
    
    private void OnDestroy()
    {
        if (GameNetworkManager.Instance != null)
        {
            GameNetworkManager.Instance.OnPlayerConnected -= UpdatePlayerList;
            GameNetworkManager.Instance.OnPlayerDisconnected -= UpdatePlayerList;
        }
    }
    
    public void Initialize(bool isHost, string roomCode)
    {
        this.isHost = isHost;
        roomCodeText.text = $"Room Code: {roomCode}";
        
        startGameButton.gameObject.SetActive(isHost);
        UpdateStartButton();
        
        lobbyPanel.SetActive(true);
        UpdatePlayerList();
    }
    
    private void OnReadyButtonClicked()
    {
        isReady = !isReady;
        readyButtonText.text = isReady ? "Not Ready" : "Ready";
        
        if (PlayerData.LocalInstance != null)
        {
            PlayerData.LocalInstance.SetReady(isReady);
        }
        
        UpdateStartButton();
    }
    
    private void OnStartGameClicked()
    {
        if (isHost && PlayerManager.Instance.AreAllPlayersReady())
        {
            StartGameServerRpc();
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
    
    private void OnLeaveButtonClicked()
    {
        GameNetworkManager.Instance.LeaveRoom();
        UIManager.Instance.TransitionToMainMenu();
    }
    
    public void UpdatePlayerList()
    {
        foreach (var kvp in playerListItems)
        {
            Destroy(kvp.Value);
        }
        playerListItems.Clear();
        
        if (PlayerManager.Instance == null) return;
        
        var players = PlayerManager.Instance.GetAllPlayers();
        
        foreach (var player in players)
        {
            GameObject listItem = Instantiate(playerListItemPrefab, playerListContainer);
            PlayerListItem itemComponent = listItem.GetComponent<PlayerListItem>();
            
            if (itemComponent != null)
            {
                itemComponent.Setup(player.PlayerName, player.IsReady, player.IsOwner);
            }
            
            playerListItems[player.OwnerClientId] = listItem;
        }
        
        UpdateStartButton();
    }
    
    private void UpdateStartButton()
    {
        if (isHost && startGameButton != null)
        {
            bool canStart = PlayerManager.Instance != null && 
                           PlayerManager.Instance.PlayerCount >= 2 && 
                           PlayerManager.Instance.AreAllPlayersReady();
            
            startGameButton.interactable = canStart;
        }
    }
}