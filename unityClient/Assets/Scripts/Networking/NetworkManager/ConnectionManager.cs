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
    
    
    public void OnStartAsHost()
    {
        Debug.Log("Starting as Host");
        var joinCodeTask = StartHostWithRelay(maxConnections, connectionType);
        
        joinCodeTask.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log($"Host started successfully with Join Code: {task.Result}");
                UIManager.Instance.StartLobbyForHost(task.Result);
            }
            else
            {
                Debug.LogError($"Failed to start host: {task.Exception?.Message}");
            }
        });
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
    
    public void OnJoinAsClient()
    {
        Debug.Log("Joining as Client");
    }
}
