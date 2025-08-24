using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Lobby
{
    public class Lobby: MonoBehaviour
    {
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private TextMeshProUGUI lobbyCode;
        [SerializeField] private GameObject playerListItemPrefab;
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button startButton;
        [SerializeField] private TextMeshProUGUI readyButtonText;
        
        private List<GameObject> playerListItems = new List<GameObject>();
        private bool isReady = false;
        
        public void StartLobbyForHost(string joinCode)
        {
            lobbyPanel.SetActive(true);
            lobbyCode.text = joinCode;
            Debug.Log($"Lobby started with Join Code: {joinCode}");
            SetupButtons();
        }
        
        public void StartLobbyForClient(string joinCode)
        {
            lobbyPanel.SetActive(true);
            lobbyCode.text = joinCode;
            Debug.Log($"Lobby started for client with Join Code: {joinCode}");
            SetupButtons();
        }
        
        public void RefreshPlayerList()
        {
            foreach (var item in playerListItems)
            {
                Destroy(item);
            }
            playerListItems.Clear();
            
            var players = Player.AllPlayers;
            foreach (var player in players)
            {
                if (player != null)
                {
                    var listItem = Instantiate(playerListItemPrefab, playerListContainer);
                    var playerListItem = listItem.GetComponent<PlayerListItem>();
                    
                    if (playerListItem != null)
                    {
                        var isHost = player.OwnerClientId == 0;
                        playerListItem.SetPlayerData(
                            player.PlayerName.Value.ToString(),
                            player.IsReady.Value,
                            isHost
                        );
                    }
                    
                    playerListItems.Add(listItem);
                }
            }
            
            Debug.Log($"Refreshed player list with {players.Count} players");
            UpdateStartButtonVisibility();
        }
        
        private void SetupButtons()
        {
            if (readyButton != null)
            {
                readyButton.onClick.RemoveAllListeners();
                readyButton.onClick.AddListener(OnReadyButtonClicked);
            }
            
            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveAllListeners();
                leaveButton.onClick.AddListener(OnLeaveButtonClicked);
            }
            
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(OnStartButtonClicked);
                startButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);
            }
            
            UpdateStartButtonVisibility();
        }
        
        private void OnReadyButtonClicked()
        {
            var localPlayer = GetLocalPlayer();
            if (localPlayer != null)
            {
                isReady = !isReady;
                localPlayer.SetReady(isReady);
                
                if (readyButtonText != null)
                {
                    readyButtonText.text = isReady ? "Unready" : "Ready";
                }
                
                Debug.Log($"Player ready state changed to: {isReady}");
            }
        }
        
        private void OnLeaveButtonClicked()
        {
            Debug.Log("Leaving lobby...");
            
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.IsHost)
                {
                    NetworkManager.Singleton.Shutdown();
                    Debug.Log("Host shutdown the lobby");
                }
                else if (NetworkManager.Singleton.IsClient)
                {
                    NetworkManager.Singleton.Shutdown();
                    Debug.Log("Client left the lobby");
                }
            }
            
            lobbyPanel.SetActive(false);
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ReturnToMainMenu();
            }
        }
        
        private void OnStartButtonClicked()
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                Debug.LogWarning("Only the host can start the game");
                return;
            }
            
            if (AreAllPlayersReady())
            {
                Debug.Log("Starting game...");
                // Set game mode to multiplayer before loading the scene
                PlayerPrefs.SetInt("GameMode", 1); // 1 = Multiplayer
                PlayerPrefs.Save();
                NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                Debug.LogWarning("Not all players are ready");
            }
        }
        
        private void UpdateStartButtonVisibility()
        {
            if (startButton != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                bool allReady = AreAllPlayersReady();
                startButton.interactable = allReady;
                
                if (startButton.GetComponentInChildren<TextMeshProUGUI>() != null)
                {
                    var buttonText = startButton.GetComponentInChildren<TextMeshProUGUI>();
                    buttonText.text = allReady ? "Start Game" : "Waiting for players...";
                }
            }
        }
        
        private bool AreAllPlayersReady()
        {
            var players = Player.AllPlayers;
            
            if (players.Count == 0)
                return false;
            
            return players.All(p => p != null && p.IsReady.Value);
        }
        
        private Player GetLocalPlayer()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
                return null;
            
            var localClientId = NetworkManager.Singleton.LocalClientId;
            return Player.AllPlayers.FirstOrDefault(p => p != null && p.OwnerClientId == localClientId);
        }
        
    }
}