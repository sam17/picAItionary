using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject joinPanel;
    [SerializeField] private GameObject loadingPanel;
    
    [Header("Main Panel")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    
    [Header("Host Panel")]
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button backFromHostButton;
    
    [Header("Join Panel")]
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private Button backFromJoinButton;
    
    [Header("Loading Panel")]
    [SerializeField] private TextMeshProUGUI loadingText;
    
    private string playerName;
    
    private void Start()
    {
        playerName = PlayerPrefs.GetString("PlayerName", "Player" + Random.Range(1000, 9999));
        playerNameInput.text = playerName;
       
        hostButton.onClick.AddListener(OnHostButtonClicked);
        joinButton.onClick.AddListener(OnJoinButtonClicked);
        startHostButton.onClick.AddListener(OnStartHostClicked);
        backFromHostButton.onClick.AddListener(() => ShowPanel(mainPanel));
        joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
        backFromJoinButton.onClick.AddListener(() => ShowPanel(mainPanel));
        playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
        
        ShowPanel(mainPanel);
    }
    
    private void OnPlayerNameChanged(string newName)
    {
        playerName = newName;
        PlayerPrefs.SetString("PlayerName", playerName);
    }
    
    
    private void ShowPanel(GameObject panel)
    {
        Debug.Log($"Showing panel: {panel?.name ?? "null"}");
        
        if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
        if (hostPanel != null) hostPanel.SetActive(panel == hostPanel);
        if (joinPanel != null) joinPanel.SetActive(panel == joinPanel);
        if (loadingPanel != null) loadingPanel.SetActive(panel == loadingPanel);
    }
    
    private void OnHostButtonClicked()
    {
        ShowPanel(hostPanel);
        roomCodeText.text = "Click 'Start' to create room";
    }
    
    private void OnJoinButtonClicked()
    {
        ShowPanel(joinPanel);
        roomCodeInput.text = "";
    }
    
    private void OnStartHostClicked()
    {
        ShowPanel(loadingPanel);
        loadingText.text = "Creating room...";
        
        bool success = GameNetworkManager.Instance.StartHost(playerName);
        
        if (success)
        {
            string roomCode = GameNetworkManager.Instance.GetCurrentRoomCode();
            UIManager.Instance.TransitionToLobby(true, roomCode);
        }
        else
        {
            ShowPanel(hostPanel);
            roomCodeText.text = "Failed to create room";
        }
    }
    
    private void OnJoinRoomClicked()
    {
        string roomCode = roomCodeInput.text.ToUpper();
        
        if (string.IsNullOrWhiteSpace(roomCode) || roomCode.Length != 6)
        {
            Debug.LogError("Invalid room code");
            return;
        }
        
        ShowPanel(loadingPanel);
        loadingText.text = "Joining room...";
        
        bool success = GameNetworkManager.Instance.StartClient(roomCode, playerName);
        
        if (success)
        {
            UIManager.Instance.TransitionToLobby(false, roomCode);
        }
        else
        {
            ShowPanel(joinPanel);
            Debug.LogError("Failed to join room");
        }
    }
}