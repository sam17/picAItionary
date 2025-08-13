using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    public class ResultsScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI correctAnswerText;
        [SerializeField] private TextMeshProUGUI playersResultText;
        [SerializeField] private TextMeshProUGUI aiResultText;
        [SerializeField] private TextMeshProUGUI scoreUpdateText;
        [SerializeField] private Image playersResultIcon;
        [SerializeField] private Image aiResultIcon;
        [SerializeField] private Sprite correctIcon;
        [SerializeField] private Sprite wrongIcon;
        
        public void Setup(string correctAnswer, bool playersCorrect, bool aiCorrect, int playersScore, int aiScore)
        {
            // Show the correct answer
            if (correctAnswerText != null)
            {
                correctAnswerText.text = $"The correct answer was: <b>{correctAnswer}</b>";
            }
            
            // Show players result
            if (playersResultText != null)
            {
                playersResultText.text = playersCorrect ? "Players: CORRECT!" : "Players: Wrong";
                playersResultText.color = playersCorrect ? Color.green : Color.red;
            }
            
            if (playersResultIcon != null && correctIcon != null && wrongIcon != null)
            {
                playersResultIcon.sprite = playersCorrect ? correctIcon : wrongIcon;
            }
            
            // Show AI result
            if (aiResultText != null)
            {
                aiResultText.text = aiCorrect ? "AI: CORRECT!" : "AI: Wrong";
                aiResultText.color = aiCorrect ? Color.green : Color.red;
            }
            
            if (aiResultIcon != null && correctIcon != null && wrongIcon != null)
            {
                aiResultIcon.sprite = aiCorrect ? correctIcon : wrongIcon;
            }
            
            // Show score update
            if (scoreUpdateText != null)
            {
                scoreUpdateText.text = $"Score: Players {playersScore} - {aiScore} AI";
            }
            
            Debug.Log($"ResultsScreen: Correct answer was {correctAnswer}, Players: {playersCorrect}, AI: {aiCorrect}");
        }
    }
}