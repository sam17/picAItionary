using UnityEngine;
using Unity.Netcode;
using System.Collections;

namespace Game
{
    public class GameSpawnManager : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private bool isTestLocal = false;
        
        [Header("Prefabs to Spawn")]
        [SerializeField] private GameObject gameControllerPrefab;
        
        private static GameSpawnManager instance;
        private bool hasSpawnedGameController = false;
        
        public bool IsTestLocal => isTestLocal;
        
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
            
            // Check if this is a local game (no networking)
            int gameMode = PlayerPrefs.GetInt("GameMode", 1);
            bool isLocalMode = gameMode == 0 || isTestLocal;
            
            if (isLocalMode)
            {
                Debug.Log("GameSpawnManager: Local mode detected, spawning GameController without networking");
                StartCoroutine(SpawnGameControllerLocalMode());
            }
            else if (NetworkManager.Singleton != null)
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
                Debug.LogWarning("GameSpawnManager: NetworkManager not found in multiplayer mode, falling back to local");
                StartCoroutine(SpawnGameControllerLocalMode());
            }
        }
        
        private IEnumerator SpawnGameControllerLocalMode()
        {
            // Wait a frame to ensure everything is ready
            yield return new WaitForSeconds(0.1f);
            
            if (!hasSpawnedGameController && gameControllerPrefab != null)
            {
                SpawnGameControllerLocal();
            }
            else if (gameControllerPrefab == null)
            {
                Debug.LogError("GameSpawnManager: GameController prefab is not assigned!");
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
        
        private void SpawnGameControllerLocal()
        {
            if (hasSpawnedGameController)
            {
                Debug.LogWarning("GameSpawnManager: GameController already spawned");
                return;
            }
            
            Debug.Log("GameSpawnManager: Spawning GameController for local mode");
            
            // Instantiate the GameController
            GameObject gameControllerInstance = Instantiate(gameControllerPrefab);
            gameControllerInstance.name = "GameController";
            
            // Pass the test local setting to the GameController
            GameController gameController = gameControllerInstance.GetComponent<GameController>();
            if (gameController != null)
            {
                gameController.SetTestLocalMode(true); // Force local mode
                Debug.Log("GameSpawnManager: Set GameController to local mode");
            }
            
            hasSpawnedGameController = true;
            Debug.Log("GameSpawnManager: GameController spawned successfully for local play");
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
            
            // Pass the test local setting to the GameController
            GameController gameController = gameControllerInstance.GetComponent<GameController>();
            if (gameController != null)
            {
                gameController.SetTestLocalMode(isTestLocal);
                Debug.Log($"GameSpawnManager: Set GameController testLocal to {isTestLocal}");
            }
            
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