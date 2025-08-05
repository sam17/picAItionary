using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections.Generic;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; }
    
    [Header("Connection Settings")]
    [SerializeField] private string connectionAddress = "127.0.0.1";
    [SerializeField] private ushort connectionPort = 7777;
    
    public event Action<string> OnRoomCodeGenerated;
    public event Action OnPlayerConnected;
    public event Action OnPlayerDisconnected;
    
    private string currentRoomCode;
    private Dictionary<string, string> roomCodes = new Dictionary<string, string>();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        // Setup callbacks
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }
    
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
    
    public bool StartHost(string playerName)
    {
        try
        {
            // Generate room code
            currentRoomCode = GenerateRoomCode();
            
            // For now, we'll use direct connection
            // In production, you'd use Unity Relay here
            
            if (NetworkManager.Singleton.StartHost())
            {
                OnRoomCodeGenerated?.Invoke(currentRoomCode);
                roomCodes[currentRoomCode] = connectionAddress;
                return true;
            }
            
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start host: {e.Message}");
            return false;
        }
    }
    
    public bool StartClient(string roomCode, string playerName)
    {
        try
        {
            // For now, we'll use direct connection
            // In production, you'd look up the room code and get relay info
            
            currentRoomCode = roomCode;
            
            return NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join room: {e.Message}");
            return false;
        }
    }
    
    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] code = new char[6];
        
        for (int i = 0; i < code.Length; i++)
        {
            code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        }
        
        return new string(code);
    }
    
    public string GetCurrentRoomCode()
    {
        return currentRoomCode;
    }
    
    private void OnClientConnected(ulong clientId)
    {
        OnPlayerConnected?.Invoke();
        Debug.Log($"Client {clientId} connected");
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        OnPlayerDisconnected?.Invoke();
        Debug.Log($"Client {clientId} disconnected");
    }
    
    public void LeaveRoom()
    {
        currentRoomCode = null;
        NetworkManager.Singleton.Shutdown();
    }
}