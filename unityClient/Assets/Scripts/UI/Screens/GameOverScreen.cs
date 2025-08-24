using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game;

namespace UI
{
    public class GameOverScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI congratulationsText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private GameObject winEffects;
        [SerializeField] private GameObject loseEffects;
        
        private void Awake()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(OnPlayAgain);
            }
            
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenu);
            }
        }
        
        public void Setup(bool playersWon, int playersScore, int aiScore)
        {
            // Set winner text
            if (winnerText != null)
            {
                if (playersScore == aiScore)
                {
                    winnerText.text = "It's a TIE!";
                    winnerText.color = Color.yellow;
                }
                else if (playersWon)
                {
                    winnerText.text = "PLAYERS WIN!";
                    winnerText.color = Color.green;
                }
                else
                {
                    winnerText.text = "AI WINS!";
                    winnerText.color = Color.red;
                }
            }
            
            // Set final score
            if (finalScoreText != null)
            {
                finalScoreText.text = $"Final Score\nPlayers: {playersScore}\nAI: {aiScore}";
            }
            
            // Set congratulations message
            if (congratulationsText != null)
            {
                if (playersScore == aiScore)
                {
                    congratulationsText.text = "Well matched! Try again?";
                }
                else if (playersWon)
                {
                    congratulationsText.text = "Congratulations! You beat the AI!";
                }
                else
                {
                    congratulationsText.text = "The AI was too smart this time. Try again!";
                }
            }
            
            // Show appropriate effects
            if (winEffects != null)
            {
                winEffects.SetActive(playersWon);
            }
            
            if (loseEffects != null)
            {
                loseEffects.SetActive(!playersWon && playersScore != aiScore);
            }
            
            Debug.Log($"GameOverScreen: Game ended - Players {playersScore} vs AI {aiScore}");
        }
        
        private void OnPlayAgain()
        {
            Debug.Log("GameOverScreen: Play again clicked");
            
            // Restart the game
            if (GameController.Instance != null)
            {
                GameController.Instance.RestartGame();
            }
        }
        
        private void OnMainMenu()
        {
            Debug.Log("GameOverScreen: Main menu clicked");
            
            // Return to main menu
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
        
        private void OnDestroy()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveListener(OnPlayAgain);
            }
            
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenu);
            }
        }
    }
}