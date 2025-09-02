using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game;

namespace UI
{
    public class DrawerReadyScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI modifierText;
        [SerializeField] private TextMeshProUGUI modifierDescriptionText;
        [SerializeField] private Button startDrawingButton;
        
        private GameController gameController;
        
        private void Awake()
        {
            if (startDrawingButton != null)
            {
                startDrawingButton.onClick.AddListener(OnStartDrawingClicked);
            }
        }
        
        public void Setup(GameController controller)
        {
            gameController = controller;
            
            if (titleText != null)
            {
                titleText.text = "You're the Drawer!";
            }
            
            if (modifierText != null && controller.CurrentRoundData != null)
            {
                var modifier = controller.CurrentRoundData.activeModifier;
                if (modifier != null)
                {
                    modifierText.gameObject.SetActive(true);
                    modifierText.text = modifier.name;
                    modifierDescriptionText.text = modifier.description;
                }
                else
                {
                    modifierText.gameObject.SetActive(false);
                }
            }
        }
        
        private void OnStartDrawingClicked()
        {
            Debug.Log("DrawerReadyScreen: Start drawing clicked");
            
            if (gameController != null)
            {
                gameController.OnDrawerReadyToStart();
            }
        }
        
        private void OnDestroy()
        {
            if (startDrawingButton != null)
            {
                startDrawingButton.onClick.RemoveListener(OnStartDrawingClicked);
            }
        }
    }
}