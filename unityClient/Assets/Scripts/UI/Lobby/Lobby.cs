using TMPro;
using UnityEngine;

namespace UI.Lobby
{
    public class Lobby: MonoBehaviour
    {
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private TextMeshProUGUI lobbyCode;
        
        public void StartLobbyForHost(string joinCode)
        {
            lobbyPanel.SetActive(true);
            lobbyCode.text = joinCode;
            Debug.Log($"Lobby started with Join Code: {joinCode}");
        }
    }
}