using UnityEngine;
using TMPro;

namespace UI
{
    public class WaitingScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private GameObject loadingAnimation;
        
        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }
        
        private void OnEnable()
        {
            // Start loading animation when screen is shown
            if (loadingAnimation != null)
            {
                loadingAnimation.SetActive(true);
            }
        }
        
        private void OnDisable()
        {
            // Stop loading animation when screen is hidden
            if (loadingAnimation != null)
            {
                loadingAnimation.SetActive(false);
            }
        }
    }
}