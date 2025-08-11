using UnityEngine;

public class PlayerListItem : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject readyIndicator;
    [SerializeField] private GameObject hostIndicator;
    
    public void SetPlayerData(string playerName, bool isReady, bool isHost)
    {
        playerNameText.text = playerName;
        readyIndicator.SetActive(isReady);
        hostIndicator.SetActive(isHost);
    }
}
