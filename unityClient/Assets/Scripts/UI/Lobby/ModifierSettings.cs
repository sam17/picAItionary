using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Lobby
{
    public class ModifierSettings : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Toggle modifierToggle;
        [SerializeField] private TextMeshProUGUI modifierStatusText;
        [SerializeField] private GameObject hostOnlyPanel;
        
        private void Start()
        {
            if (Game.Modifiers.ModifierManager.Instance != null)
            {
                Game.Modifiers.ModifierManager.Instance.LoadModifierSettings();
                bool enabled = Game.Modifiers.ModifierManager.Instance.AreModifiersEnabled();
                
                if (modifierToggle != null)
                {
                    modifierToggle.isOn = enabled;
                    modifierToggle.onValueChanged.AddListener(OnModifierToggleChanged);
                }
                
                UpdateUI(enabled);
            }
            
            bool isHost = Unity.Netcode.NetworkManager.Singleton == null || 
                         Unity.Netcode.NetworkManager.Singleton.IsHost || 
                         Unity.Netcode.NetworkManager.Singleton.IsServer ||
                         !Unity.Netcode.NetworkManager.Singleton.IsClient;
            
            if (hostOnlyPanel != null)
            {
                hostOnlyPanel.SetActive(isHost);
            }
            
            if (modifierToggle != null)
            {
                modifierToggle.interactable = isHost;
            }
        }
        
        private void OnModifierToggleChanged(bool value)
        {
            if (Game.Modifiers.ModifierManager.Instance != null)
            {
                Game.Modifiers.ModifierManager.Instance.SetModifiersEnabled(value);
            }
            
            if (Game.GameController.Instance != null)
            {
                Game.GameController.Instance.SetModifiersEnabled(value);
            }
            
            UpdateUI(value);
        }
        
        private void UpdateUI(bool enabled)
        {
            if (modifierStatusText != null)
            {
                modifierStatusText.text = enabled ? "Modifiers: ON" : "Modifiers: OFF";
                modifierStatusText.color = enabled ? Color.green : Color.gray;
            }
        }
        
        private void OnDestroy()
        {
            if (modifierToggle != null)
            {
                modifierToggle.onValueChanged.RemoveListener(OnModifierToggleChanged);
            }
        }
    }
}