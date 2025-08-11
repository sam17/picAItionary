using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace UI.Lobby
{
    public class Lobby: MonoBehaviour
    {
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private TextMeshProUGUI lobbyCode;
        [SerializeField] private GameObject playerListItemPrefab;
        [SerializeField] private Transform playerListContainer;
        
        private List<GameObject> playerListItems = new List<GameObject>();
        
        public void StartLobbyForHost(string joinCode)
        {
            lobbyPanel.SetActive(true);
            lobbyCode.text = joinCode;
            Debug.Log($"Lobby started with Join Code: {joinCode}");
        }
        
        public void StartLobbyForClient(string joinCode)
        {
            lobbyPanel.SetActive(true);
            lobbyCode.text = joinCode;
            Debug.Log($"Lobby started for client with Join Code: {joinCode}");
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
        }
    }
}