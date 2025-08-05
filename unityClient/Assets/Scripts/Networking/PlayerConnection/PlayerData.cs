using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    [SerializeField] private NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>("", 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    [SerializeField] private NetworkVariable<int> playerScore = new NetworkVariable<int>(0, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    [SerializeField] private NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    [SerializeField] private NetworkVariable<bool> isCurrentDrawer = new NetworkVariable<bool>(false, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public static PlayerData LocalInstance { get; private set; }
    
    public string PlayerName => playerName.Value.ToString();
    public int Score => playerScore.Value;
    public bool IsReady => isReady.Value;
    public bool IsDrawer => isCurrentDrawer.Value;
    
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalInstance = this;
            SetPlayerName(PlayerPrefs.GetString("PlayerName", "Player"));
        }
        
        playerName.OnValueChanged += OnPlayerNameChanged;
        playerScore.OnValueChanged += OnScoreChanged;
        isReady.OnValueChanged += OnReadyStateChanged;
        
        if (IsServer)
        {
            PlayerManager.Instance.RegisterPlayer(OwnerClientId, this);
        }
    }
    
    public override void OnNetworkDespawn()
    {
        playerName.OnValueChanged -= OnPlayerNameChanged;
        playerScore.OnValueChanged -= OnScoreChanged;
        isReady.OnValueChanged -= OnReadyStateChanged;
        
        if (IsServer)
        {
            PlayerManager.Instance.UnregisterPlayer(OwnerClientId);
        }
        
        if (LocalInstance == this)
        {
            LocalInstance = null;
        }
    }
    
    public void SetPlayerName(string name)
    {
        if (IsOwner)
        {
            playerName.Value = name;
            PlayerPrefs.SetString("PlayerName", name);
        }
    }
    
    public void SetReady(bool ready)
    {
        if (IsOwner)
        {
            isReady.Value = ready;
        }
    }
    
    [ServerRpc]
    public void UpdateScoreServerRpc(int points)
    {
        playerScore.Value += points;
    }
    
    [ServerRpc]
    public void SetAsDrawerServerRpc(bool isDrawer)
    {
        isCurrentDrawer.Value = isDrawer;
    }
    
    private void OnPlayerNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.UpdatePlayerList();
        }
    }
    
    private void OnScoreChanged(int oldValue, int newValue)
    {
        Debug.Log($"{PlayerName}'s score changed: {oldValue} -> {newValue}");
    }
    
    private void OnReadyStateChanged(bool oldValue, bool newValue)
    {
        if (LobbyUI.Instance != null)
        {
            LobbyUI.Instance.UpdatePlayerList();
        }
    }
}