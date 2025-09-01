using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class AspectRatioHandler : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float targetAspectRatio = 0.5625f; // 9:16 (1080/1920)
        [SerializeField] private bool usePillarboxing = true;
        [SerializeField] private Color backgroundColor = Color.black;
        
        [Header("References")]
        [SerializeField] private RectTransform uiContainer;
        [SerializeField] private CanvasScaler canvasScaler;
        
        [Header("Pillarbox Panels (Optional)")]
        [SerializeField] private GameObject leftPillar;
        [SerializeField] private GameObject rightPillar;
        [SerializeField] private GameObject topLetterbox;
        [SerializeField] private GameObject bottomLetterbox;
        
        private Camera mainCamera;
        private float lastAspectRatio;
        
        private void Awake()
        {
            if (uiContainer == null)
            {
                uiContainer = GetComponent<RectTransform>();
            }
            
            if (canvasScaler == null)
            {
                canvasScaler = GetComponentInParent<CanvasScaler>();
            }
            
            mainCamera = Camera.main;
            
            CreatePillarboxPanels();
            UpdateAspectRatio();
        }
        
        private void Update()
        {
            float currentAspectRatio = (float)Screen.width / Screen.height;
            
            if (Mathf.Abs(currentAspectRatio - lastAspectRatio) > 0.01f)
            {
                UpdateAspectRatio();
                lastAspectRatio = currentAspectRatio;
            }
        }
        
        private void UpdateAspectRatio()
        {
            float currentAspectRatio = (float)Screen.width / Screen.height;
            
            if (usePillarboxing)
            {
                ApplyPillarboxing(currentAspectRatio);
            }
            else
            {
                ApplyScaling(currentAspectRatio);
            }
        }
        
        private void ApplyPillarboxing(float currentAspectRatio)
        {
            if (currentAspectRatio > targetAspectRatio)
            {
                // Screen is wider than target - add side pillars
                float scaleFactor = targetAspectRatio / currentAspectRatio;
                
                if (uiContainer != null)
                {
                    uiContainer.anchorMin = new Vector2(0.5f - scaleFactor * 0.5f, 0);
                    uiContainer.anchorMax = new Vector2(0.5f + scaleFactor * 0.5f, 1);
                    uiContainer.offsetMin = Vector2.zero;
                    uiContainer.offsetMax = Vector2.zero;
                }
                
                // Show side pillars
                if (leftPillar != null && rightPillar != null)
                {
                    leftPillar.SetActive(true);
                    rightPillar.SetActive(true);
                    
                    RectTransform leftRect = leftPillar.GetComponent<RectTransform>();
                    RectTransform rightRect = rightPillar.GetComponent<RectTransform>();
                    
                    leftRect.anchorMin = new Vector2(0, 0);
                    leftRect.anchorMax = new Vector2(0.5f - scaleFactor * 0.5f, 1);
                    leftRect.offsetMin = Vector2.zero;
                    leftRect.offsetMax = Vector2.zero;
                    
                    rightRect.anchorMin = new Vector2(0.5f + scaleFactor * 0.5f, 0);
                    rightRect.anchorMax = new Vector2(1, 1);
                    rightRect.offsetMin = Vector2.zero;
                    rightRect.offsetMax = Vector2.zero;
                }
                
                if (topLetterbox != null) topLetterbox.SetActive(false);
                if (bottomLetterbox != null) bottomLetterbox.SetActive(false);
            }
            else if (currentAspectRatio < targetAspectRatio)
            {
                // Screen is taller than target - add top/bottom letterbox
                float scaleFactor = currentAspectRatio / targetAspectRatio;
                
                if (uiContainer != null)
                {
                    uiContainer.anchorMin = new Vector2(0, 0.5f - scaleFactor * 0.5f);
                    uiContainer.anchorMax = new Vector2(1, 0.5f + scaleFactor * 0.5f);
                    uiContainer.offsetMin = Vector2.zero;
                    uiContainer.offsetMax = Vector2.zero;
                }
                
                // Show top/bottom letterbox
                if (topLetterbox != null && bottomLetterbox != null)
                {
                    topLetterbox.SetActive(true);
                    bottomLetterbox.SetActive(true);
                    
                    RectTransform topRect = topLetterbox.GetComponent<RectTransform>();
                    RectTransform bottomRect = bottomLetterbox.GetComponent<RectTransform>();
                    
                    topRect.anchorMin = new Vector2(0, 0.5f + scaleFactor * 0.5f);
                    topRect.anchorMax = new Vector2(1, 1);
                    topRect.offsetMin = Vector2.zero;
                    topRect.offsetMax = Vector2.zero;
                    
                    bottomRect.anchorMin = new Vector2(0, 0);
                    bottomRect.anchorMax = new Vector2(1, 0.5f - scaleFactor * 0.5f);
                    bottomRect.offsetMin = Vector2.zero;
                    bottomRect.offsetMax = Vector2.zero;
                }
                
                if (leftPillar != null) leftPillar.SetActive(false);
                if (rightPillar != null) rightPillar.SetActive(false);
            }
            else
            {
                // Perfect match
                if (uiContainer != null)
                {
                    uiContainer.anchorMin = Vector2.zero;
                    uiContainer.anchorMax = Vector2.one;
                    uiContainer.offsetMin = Vector2.zero;
                    uiContainer.offsetMax = Vector2.zero;
                }
                
                if (leftPillar != null) leftPillar.SetActive(false);
                if (rightPillar != null) rightPillar.SetActive(false);
                if (topLetterbox != null) topLetterbox.SetActive(false);
                if (bottomLetterbox != null) bottomLetterbox.SetActive(false);
            }
        }
        
        private void ApplyScaling(float currentAspectRatio)
        {
            if (canvasScaler != null)
            {
                // Adjust canvas scaler match based on aspect ratio
                if (currentAspectRatio > targetAspectRatio)
                {
                    // Wider screen - match height more
                    canvasScaler.matchWidthOrHeight = 1f;
                }
                else
                {
                    // Taller screen - match width more
                    canvasScaler.matchWidthOrHeight = 0f;
                }
            }
            
            // Reset UI container to full screen
            if (uiContainer != null)
            {
                uiContainer.anchorMin = Vector2.zero;
                uiContainer.anchorMax = Vector2.one;
                uiContainer.offsetMin = Vector2.zero;
                uiContainer.offsetMax = Vector2.zero;
            }
            
            // Hide all pillarbox/letterbox panels
            if (leftPillar != null) leftPillar.SetActive(false);
            if (rightPillar != null) rightPillar.SetActive(false);
            if (topLetterbox != null) topLetterbox.SetActive(false);
            if (bottomLetterbox != null) bottomLetterbox.SetActive(false);
        }
        
        private void CreatePillarboxPanels()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            
            if (leftPillar == null)
            {
                leftPillar = CreatePanel("LeftPillar", canvas.transform);
            }
            
            if (rightPillar == null)
            {
                rightPillar = CreatePanel("RightPillar", canvas.transform);
            }
            
            if (topLetterbox == null)
            {
                topLetterbox = CreatePanel("TopLetterbox", canvas.transform);
            }
            
            if (bottomLetterbox == null)
            {
                bottomLetterbox = CreatePanel("BottomLetterbox", canvas.transform);
            }
        }
        
        private GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Image image = panel.AddComponent<Image>();
            image.color = backgroundColor;
            
            // Make sure panels are behind UI
            panel.transform.SetAsFirstSibling();
            
            return panel;
        }
    }
}