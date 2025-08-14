using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Drawing
{
    public class DrawingToolsController : MonoBehaviour
    {
        [Header("Canvas Reference")]
        [SerializeField] private UIDrawingCanvas drawingCanvas;
        
        [Header("Brush Size Controls")]
        [SerializeField] private Slider brushSizeSlider;
        [SerializeField] private TextMeshProUGUI brushSizeText;
        [SerializeField] private List<Button> brushSizePresets = new List<Button>();
        [SerializeField] private float[] presetSizes = { 2f, 5f, 10f, 15f };
        
        [Header("Color Controls")]
        [SerializeField] private List<Button> colorButtons = new List<Button>();
        [SerializeField] private Image currentColorDisplay;
        [SerializeField] private Color[] availableColors = {
            Color.black,
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow,
            new Color(1f, 0.5f, 0f), // Orange
            new Color(0.5f, 0f, 1f), // Purple
            new Color(0.6f, 0.3f, 0f), // Brown
            Color.gray,
            Color.white
        };
        
        [Header("Tool Buttons")]
        [SerializeField] private Button undoButton;
        [SerializeField] private Button redoButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Toggle eraserToggle;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject brushPreview;
        [SerializeField] private RectTransform brushPreviewTransform;
        [SerializeField] private Image brushPreviewImage;
        [SerializeField] private float previewScale = 100f;
        
        private float currentBrushSize = 5f;
        private Color currentColor = Color.black;
        private bool isErasing = false;
        private Stack<byte[]> undoHistory = new Stack<byte[]>();
        private Stack<byte[]> redoHistory = new Stack<byte[]>();
        
        private void Start()
        {
            InitializeControls();
            SetupColorPalette();
            UpdateBrushPreview();
        }
        
        private void InitializeControls()
        {
            if (drawingCanvas == null)
            {
                drawingCanvas = FindObjectOfType<UIDrawingCanvas>();
                if (drawingCanvas == null)
                {
                    Debug.LogError("DrawingToolsController: No UIDrawingCanvas found!");
                    return;
                }
            }
            
            // Brush size slider - adjusted for pixel-based drawing
            if (brushSizeSlider != null)
            {
                brushSizeSlider.minValue = 1f;
                brushSizeSlider.maxValue = 20f;
                brushSizeSlider.value = 5f;
                currentBrushSize = 5f;
                brushSizeSlider.onValueChanged.AddListener(OnBrushSizeChanged);
            }
            
            // Brush size preset buttons
            for (int i = 0; i < brushSizePresets.Count && i < presetSizes.Length; i++)
            {
                int index = i;
                brushSizePresets[i].onClick.AddListener(() => SetBrushSize(presetSizes[index]));
            }
            
            // Undo/Redo buttons
            if (undoButton != null)
            {
                undoButton.onClick.AddListener(OnUndo);
            }
            
            if (redoButton != null)
            {
                redoButton.onClick.AddListener(OnRedo);
                redoButton.interactable = false;
            }
            
            // Clear button
            if (clearButton != null)
            {
                clearButton.onClick.AddListener(OnClear);
            }
            
            // Eraser toggle
            if (eraserToggle != null)
            {
                eraserToggle.onValueChanged.AddListener(OnEraserToggled);
            }
        }
        
        private void SetupColorPalette()
        {
            for (int i = 0; i < colorButtons.Count && i < availableColors.Length; i++)
            {
                int index = i;
                Button button = colorButtons[i];
                
                // Set button color
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = availableColors[i];
                }
                
                // Add click listener
                button.onClick.AddListener(() => SetColor(availableColors[index]));
                
                // Add selection indicator
                if (i == 0)
                {
                    AddSelectionIndicator(button);
                }
            }
            
            SetColor(Color.black);
        }
        
        private void OnBrushSizeChanged(float size)
        {
            currentBrushSize = size;
            drawingCanvas.SetBrushSize(size);
            
            if (brushSizeText != null)
            {
                brushSizeText.text = $"{Mathf.RoundToInt(size)}";
            }
            
            UpdateBrushPreview();
        }
        
        private void SetBrushSize(float size)
        {
            currentBrushSize = size;
            drawingCanvas.SetBrushSize(size);
            
            if (brushSizeSlider != null)
            {
                brushSizeSlider.value = size;
            }
            
            UpdateBrushPreview();
        }
        
        private void SetColor(Color color)
        {
            if (isErasing) return;
            
            currentColor = color;
            drawingCanvas.SetBrushColor(color);
            
            if (currentColorDisplay != null)
            {
                currentColorDisplay.color = color;
            }
            
            UpdateColorSelection(color);
            UpdateBrushPreview();
        }
        
        private void UpdateColorSelection(Color selectedColor)
        {
            for (int i = 0; i < colorButtons.Count && i < availableColors.Length; i++)
            {
                Button button = colorButtons[i];
                bool isSelected = availableColors[i] == selectedColor;
                
                Transform indicator = button.transform.Find("SelectionIndicator");
                if (indicator != null)
                {
                    indicator.gameObject.SetActive(isSelected);
                }
                
                // Scale effect for selected button
                button.transform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;
            }
        }
        
        private void AddSelectionIndicator(Button button)
        {
            GameObject indicator = new GameObject("SelectionIndicator");
            indicator.transform.SetParent(button.transform, false);
            
            Image indicatorImage = indicator.AddComponent<Image>();
            indicatorImage.color = new Color(1, 1, 1, 0.5f);
            
            RectTransform rect = indicator.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = new Vector2(-4, -4);
            rect.anchoredPosition = Vector2.zero;
        }
        
        private void OnEraserToggled(bool isOn)
        {
            isErasing = isOn;
            
            if (isErasing)
            {
                drawingCanvas.SetBrushColor(Color.white);
                if (currentColorDisplay != null)
                {
                    currentColorDisplay.color = Color.white;
                }
            }
            else
            {
                drawingCanvas.SetBrushColor(currentColor);
                if (currentColorDisplay != null)
                {
                    currentColorDisplay.color = currentColor;
                }
            }
            
            UpdateBrushPreview();
        }
        
        private void OnUndo()
        {
            SaveCurrentState();
            drawingCanvas.Undo();
            
            if (undoHistory.Count > 0)
            {
                redoHistory.Push(undoHistory.Pop());
                if (redoButton != null)
                {
                    redoButton.interactable = true;
                }
            }
        }
        
        private void OnRedo()
        {
            if (redoHistory.Count > 0)
            {
                byte[] state = redoHistory.Pop();
                drawingCanvas.LoadDrawingData(state);
                undoHistory.Push(state);
                
                if (redoHistory.Count == 0 && redoButton != null)
                {
                    redoButton.interactable = false;
                }
            }
        }
        
        private void OnClear()
        {
            SaveCurrentState();
            drawingCanvas.ClearCanvas();
        }
        
        private void SaveCurrentState()
        {
            byte[] currentState = drawingCanvas.GetDrawingData();
            undoHistory.Push(currentState);
            
            // Limit undo history
            if (undoHistory.Count > 20)
            {
                var tempStack = new Stack<byte[]>();
                for (int i = 0; i < 19; i++)
                {
                    tempStack.Push(undoHistory.Pop());
                }
                undoHistory.Clear();
                while (tempStack.Count > 0)
                {
                    undoHistory.Push(tempStack.Pop());
                }
            }
            
            redoHistory.Clear();
            if (redoButton != null)
            {
                redoButton.interactable = false;
            }
        }
        
        private void Update()
        {
            UpdateBrushPreviewPosition();
        }
        
        private void UpdateBrushPreview()
        {
            if (brushPreview == null || brushPreviewImage == null) return;
            
            float size = currentBrushSize * 2f; // Scale down preview since brush sizes are now in pixels
            brushPreviewTransform.sizeDelta = new Vector2(size, size);
            
            Color previewColor = isErasing ? Color.white : currentColor;
            previewColor.a = 0.5f;
            brushPreviewImage.color = previewColor;
        }
        
        private void UpdateBrushPreviewPosition()
        {
            if (brushPreview == null) return;
            
            Vector3 mousePos = Input.mousePosition;
            brushPreviewTransform.position = mousePos;
            
            // Show/hide based on whether mouse is over canvas
            bool isOverCanvas = RectTransformUtility.RectangleContainsScreenPoint(
                drawingCanvas.GetComponent<RectTransform>(), 
                mousePos, 
                Camera.main
            );
            
            brushPreview.SetActive(isOverCanvas);
        }
        
        private void OnDestroy()
        {
            if (brushSizeSlider != null)
            {
                brushSizeSlider.onValueChanged.RemoveAllListeners();
            }
            
            foreach (var button in brushSizePresets)
            {
                if (button != null)
                    button.onClick.RemoveAllListeners();
            }
            
            foreach (var button in colorButtons)
            {
                if (button != null)
                    button.onClick.RemoveAllListeners();
            }
            
            if (undoButton != null)
                undoButton.onClick.RemoveAllListeners();
                
            if (redoButton != null)
                redoButton.onClick.RemoveAllListeners();
                
            if (clearButton != null)
                clearButton.onClick.RemoveAllListeners();
                
            if (eraserToggle != null)
                eraserToggle.onValueChanged.RemoveAllListeners();
        }
    }
}