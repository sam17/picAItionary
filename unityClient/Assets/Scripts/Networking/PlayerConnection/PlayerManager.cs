using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    
    private Dictionary<ulong, PlayerData> connectedPlayers = new Dictionary<ulong, PlayerData>();
    
    public IReadOnlyDictionary<ulong, PlayerData> ConnectedPlayers => connectedPlayers;
    public int PlayerCount => connectedPlayers.Count;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    public void RegisterPlayer(ulong clientId, PlayerData playerData)
    {
        if (!connectedPlayers.ContainsKey(clientId))
        {
            connectedPlayers[clientId] = playerData;
            Debug.Log($"Player {clientId} registered");
        }
    }
    
    public void UnregisterPlayer(ulong clientId)
    {
        if (connectedPlayers.ContainsKey(clientId))
        {
            connectedPlayers.Remove(clientId);
            Debug.Log($"Player {clientId} unregistered");
        }
    }
    
    public PlayerData GetPlayer(ulong clientId)
    {
        return connectedPlayers.ContainsKey(clientId) ? connectedPlayers[clientId] : null;
    }
    
    public List<PlayerData> GetAllPlayers()
    {
        return connectedPlayers.Values.ToList();
    }
    
    public bool AreAllPlayersReady()
    {
        if (connectedPlayers.Count < 2) return false;
        
        foreach (var player in connectedPlayers.Values)
        {
            if (!player.IsReady) return false;
        }
        
        return true;
    }
    
    public void ResetAllPlayersReady()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            foreach (var player in connectedPlayers.Values)
            {
                player.SetReady(false);
            }
        }
    }
    
    public PlayerData GetCurrentDrawer()
    {
        foreach (var player in connectedPlayers.Values)
        {
            if (player.IsDrawer) return player;
        }
        return null;
    }
}