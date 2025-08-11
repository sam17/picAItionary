using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(
        "Player",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    
    public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);
    
    private static List<Player> allPlayers = new List<Player>();
    
    public static List<Player> AllPlayers => allPlayers;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!allPlayers.Contains(this))
        {
            allPlayers.Add(this);
        }
        
        PlayerName.OnValueChanged += OnPlayerNameChanged;
        IsReady.OnValueChanged += OnReadyStateChanged;
        
        if (IsOwner)
        {
            var playerName = UI.Lobby.UIManager.Instance.GetPlayerName();
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = $"Player {OwnerClientId}";
            }
            PlayerName.Value = playerName;
        }
        
        UpdatePlayerList();
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        allPlayers.Remove(this);
        
        PlayerName.OnValueChanged -= OnPlayerNameChanged;
        IsReady.OnValueChanged -= OnReadyStateChanged;
        
        UpdatePlayerList();
    }
    
    private void OnPlayerNameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        UpdatePlayerList();
    }
    
    private void OnReadyStateChanged(bool oldValue, bool newValue)
    {
        UpdatePlayerList();
    }
    
    private void UpdatePlayerList()
    {
        if (UI.Lobby.UIManager.Instance != null)
        {
            UI.Lobby.UIManager.Instance.RefreshPlayerList();
        }
    }
    
    public void SetReady(bool ready)
    {
        if (IsOwner)
        {
            IsReady.Value = ready;
        }
    }
}