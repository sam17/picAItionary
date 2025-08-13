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
        [SerializeField] private TextMeshProUGUI instructionText;
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
            
            // Update UI text based on game mode
            if (titleText != null)
            {
                titleText.text = "You're the Drawer!";
            }
            
            if (instructionText != null)
            {
                instructionText.text = controller.CurrentGameMode == GameMode.Local ?
                    "Get ready to draw! You'll see 4 options with one highlighted." :
                    "Get ready to draw for the other players!";
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