using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace Game
{
    public class GameSpawnManager : MonoBehaviour
    {
        [Header("Prefabs to Spawn")]
        [SerializeField] private GameObject gameControllerPrefab;
        
        private static GameSpawnManager instance;
        private bool hasSpawnedGameController = false;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            Debug.Log("GameSpawnManager: Start called");
            
            // Check if we're in a networked context
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                {
                    Debug.Log("GameSpawnManager: Host/Server detected, spawning GameController");
                    StartCoroutine(SpawnGameControllerWithDelay());
                }
                else if (NetworkManager.Singleton.IsClient)
                {
                    Debug.Log("GameSpawnManager: Client detected, waiting for server to spawn GameController");
                }
            }
            else
            {
                Debug.LogWarning("GameSpawnManager: NetworkManager not found, cannot spawn GameController");
            }
        }
        
        private IEnumerator SpawnGameControllerWithDelay()
        {
            // Wait a frame to ensure network is ready
            yield return new WaitForSeconds(0.1f);
            
            if (!hasSpawnedGameController && gameControllerPrefab != null)
            {
                SpawnGameController();
            }
            else if (gameControllerPrefab == null)
            {
                Debug.LogError("GameSpawnManager: GameController prefab is not assigned!");
            }
        }
        
        private void SpawnGameController()
        {
            if (hasSpawnedGameController)
            {
                Debug.LogWarning("GameSpawnManager: GameController already spawned");
                return;
            }
            
            Debug.Log("GameSpawnManager: Spawning GameController");
            
            // Instantiate the GameController
            GameObject gameControllerInstance = Instantiate(gameControllerPrefab);
            gameControllerInstance.name = "GameController";
            
            // Get the NetworkObject component
            NetworkObject networkObject = gameControllerInstance.GetComponent<NetworkObject>();
            
            if (networkObject != null)
            {
                // Spawn it on the network
                networkObject.Spawn();
                hasSpawnedGameController = true;
                Debug.Log("GameSpawnManager: GameController spawned successfully on network");
            }
            else
            {
                Debug.LogError("GameSpawnManager: GameController prefab doesn't have NetworkObject component!");
                Destroy(gameControllerInstance);
            }
        }
    }
}