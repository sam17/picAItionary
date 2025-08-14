using UnityEngine;
using TMPro;

namespace UI
{
    public class ScoreboardScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI playersScoreText;
        [SerializeField] private TextMeshProUGUI aiScoreText;
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private GameObject leadingIndicator;
        
        public void UpdateScores(int playersScore, int aiScore)
        {
            if (playersScoreText != null)
            {
                playersScoreText.text = playersScore.ToString();
            }
            
            if (aiScoreText != null)
            {
                aiScoreText.text = aiScore.ToString();
            }
            
            // Update leading indicator
            if (leadingIndicator != null)
            {
                if (playersScore > aiScore)
                {
                    // Position near players score
                    leadingIndicator.transform.SetParent(playersScoreText.transform, false);
                    leadingIndicator.SetActive(true);
                }
                else if (aiScore > playersScore)
                {
                    // Position near AI score
                    leadingIndicator.transform.SetParent(aiScoreText.transform, false);
                    leadingIndicator.SetActive(true);
                }
                else
                {
                    // Hide if tied
                    leadingIndicator.SetActive(false);
                }
            }
        }
        
        public void UpdateRound(int currentRound, int totalRounds)
        {
            if (roundText != null)
            {
                roundText.text = $"Round {currentRound}/{totalRounds}";
            }
        }
    }
}