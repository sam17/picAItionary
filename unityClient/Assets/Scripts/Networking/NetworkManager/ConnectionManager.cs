using System.Threading.Tasks;
using UI.Lobby;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    [SerializeField] private int maxConnections = 12;
    [SerializeField] private string connectionType = "udp"; // or "tcp"
    
    public static ConnectionManager Instance;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupNetworkCallbacks();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void SetupNetworkCallbacks()
    {
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
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected");
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected");
    }
    
    public async void OnStartAsHost()
    {
        Debug.Log("Starting as Host");
        var joinCode = await StartHostWithRelay(maxConnections, connectionType);
        
        if (!string.IsNullOrEmpty(joinCode))
        {
            Debug.Log($"Host started successfully with Join Code: {joinCode}");
            UIManager.Instance.StartLobbyForHost(joinCode);
        }
        else
        {
            Debug.LogError("Failed to start host: Join code is null or empty");
        }
    }
    
    public async Task<string> StartHostWithRelay(int maxConnections, string connectionType)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        return NetworkManager.Singleton.StartHost() ? joinCode : null;
    }
    
    
    public async Task<bool> StartClientWithRelay(string joinCode, string connectionType)
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
            return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log($"Failed to join relay: {e.Message} (Error Code: {e.ErrorCode})");
            throw;
        }
    }
    
    public async void OnJoinAsClient(string joinCode)
    {
        Debug.Log($"Joining as Client with Join Code: {joinCode}");
        try
        {
            var success = await StartClientWithRelay(joinCode, connectionType);
            if (success)
            {
                Debug.Log("Client started successfully");
                UIManager.Instance.StartLobbyForClient(joinCode);
            }
            else
            {
                Debug.LogError("Failed to start client");
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to join with code '{joinCode}': {e.Message}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Unexpected error while joining: {e.Message}");
        }
    }
}
