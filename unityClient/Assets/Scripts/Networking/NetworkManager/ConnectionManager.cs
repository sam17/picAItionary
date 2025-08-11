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
        }
        else
        {
            Destroy(gameObject);
        }
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
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
        return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
    }
    
    public void OnJoinAsClient(string joinCode)
    {
        Debug.Log($"Joining as Client with Join Code: {joinCode}");
        StartClientWithRelay(joinCode, connectionType).ContinueWith(task =>
        {
            if (task.Result)
            {
                Debug.Log("Client started successfully");
                UIManager.Instance.StartLobbyForClient(joinCode);
            }
            else
            {
                Debug.LogError("Failed to start client");
            }
        });
    }
}
