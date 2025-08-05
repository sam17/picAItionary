using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject readyIndicator;
    [SerializeField] private GameObject hostIndicator;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color localPlayerColor = new Color(0.9f, 0.9f, 1f);
    
    public void Setup(string playerName, bool isReady, bool isLocalPlayer)
    {
        playerNameText.text = playerName;
        readyIndicator.SetActive(isReady);
        
        if (isLocalPlayer)
        {
            backgroundImage.color = localPlayerColor;
        }
        else
        {
            backgroundImage.color = normalColor;
        }
    }
}