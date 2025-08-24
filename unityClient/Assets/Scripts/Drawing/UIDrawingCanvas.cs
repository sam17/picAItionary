using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Drawing;
using UI;

namespace Drawing
{
    public class UIDrawingCanvas : MonoBehaviour, IDrawingCanvas, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Canvas Settings")]
        [SerializeField] private RawImage drawingImage;
        [SerializeField] private int textureWidth = 512;
        [SerializeField] private int textureHeight = 512;
        
        [Header("Drawing Settings")]
        [SerializeField] private float brushSize = 5f;
        [SerializeField] private Color brushColor = Color.black;
        [SerializeField] private bool smoothLines = true;
        
        public void ConstrainDrawingPosition(Vector2 screenPosition)
        {
            // Used for straight lines modifier
        }
        
        private Texture2D drawingTexture;
        private Color[] cleanColors;
        private Color[] currentPixels; // Working pixel buffer
        private DrawingData drawingData;
        private Stroke currentStroke;
        private bool isDrawing = false;
        private Vector2 lastDrawPoint;
        private bool needsApply = false;
        private float lastApplyTime = 0f;
        private const float APPLY_INTERVAL = 0.016f; // Apply at ~60fps max
        
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        
        private void Awake()
        {
            Initialize();
        }
        
        private void LateUpdate()
        {
            // Apply any pending pixel changes at the end of frame
            if (needsApply && Time.time - lastApplyTime > APPLY_INTERVAL)
            {
                ApplyPixelBuffer();
            }
        }
        
        private void Initialize()
        {
            // Get components
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = gameObject.AddComponent<RectTransform>();
            
            // Find parent canvas
            parentCanvas = GetComponentInParent<Canvas>();
            
            // Create RawImage if not assigned
            if (drawingImage == null)
            {
                drawingImage = GetComponent<RawImage>();
                if (drawingImage == null)
                {
                    drawingImage = gameObject.AddComponent<RawImage>();
                }
            }
            
            // Create drawing texture
            drawingTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            drawingTexture.filterMode = FilterMode.Bilinear;
            
            // Initialize with white background
            cleanColors = new Color[textureWidth * textureHeight];
            currentPixels = new Color[textureWidth * textureHeight];
            for (int i = 0; i < cleanColors.Length; i++)
            {
                cleanColors[i] = Color.white;
                currentPixels[i] = Color.white;
            }
            drawingTexture.SetPixels(cleanColors);
            drawingTexture.Apply();
            
            // Set texture to RawImage
            drawingImage.texture = drawingTexture;
            
            // Initialize drawing data
            drawingData = new DrawingData();
            drawingData.width = textureWidth;
            drawingData.height = textureHeight;
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isDrawing)
            {
                Vector2 localPoint;
                if (GetDrawingPoint(eventData.position, out localPoint))
                {
                    StartNewStroke(localPoint);
                }
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (isDrawing)
            {
                Vector2 localPoint;
                if (GetDrawingPoint(eventData.position, out localPoint))
                {
                    AddPointToStroke(localPoint);
                }
            }
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            if (isDrawing)
            {
                EndStroke();
            }
        }
        
        private bool GetDrawingPoint(Vector2 screenPosition, out Vector2 texturePoint)
        {
            // First check if we're actually over the RawImage
            RectTransform imageRect = drawingImage.rectTransform;
            
            Camera cam = null;
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = parentCanvas.worldCamera;
            }
            
            // Check if pointer is within the RawImage bounds
            if (!RectTransformUtility.RectangleContainsScreenPoint(imageRect, screenPosition, cam))
            {
                texturePoint = Vector2.zero;
                return false;
            }
            
            // Convert screen position to local position within the RawImage rect
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                imageRect, 
                screenPosition, 
                cam, 
                out localPoint))
            {
                texturePoint = Vector2.zero;
                return false;
            }
            
            // Get the rect of the RawImage
            Rect rect = imageRect.rect;
            
            // Calculate position within the rect (0 to 1)
            // localPoint is relative to the center of the rect, so we need to offset
            float normalizedX = (localPoint.x - rect.xMin) / rect.width;
            float normalizedY = (localPoint.y - rect.yMin) / rect.height;
            
            // Clamp to 0-1 range
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);
            
            // Convert to texture pixel coordinates
            float x = normalizedX * textureWidth;
            float y = normalizedY * textureHeight;
            
            texturePoint = new Vector2(x, y);
            return true;
        }
        
        private void StartNewStroke(Vector2 texturePoint)
        {
            isDrawing = true;
            
            currentStroke = new Stroke
            {
                color = brushColor,
                thickness = brushSize
            };
            
            // Add first point
            float normalizedX = texturePoint.x / textureWidth;
            float normalizedY = texturePoint.y / textureHeight;
            currentStroke.AddPoint(normalizedX, normalizedY);
            
            // Draw initial point
            DrawBrush(texturePoint.x, texturePoint.y);
            lastDrawPoint = texturePoint;
        }
        
        private void AddPointToStroke(Vector2 texturePoint)
        {
            if (!isDrawing || currentStroke == null) return;
            
            // Add point to stroke data
            float normalizedX = texturePoint.x / textureWidth;
            float normalizedY = texturePoint.y / textureHeight;
            currentStroke.AddPoint(normalizedX, normalizedY);
            
            // Draw line from last point to current point
            if (smoothLines)
            {
                DrawLine(lastDrawPoint, texturePoint);
            }
            else
            {
                DrawBrush(texturePoint.x, texturePoint.y);
            }
            
            lastDrawPoint = texturePoint;
        }
        
        private void EndStroke()
        {
            if (!isDrawing || currentStroke == null) return;
            
            isDrawing = false;
            
            // Make sure to apply any pending changes
            if (needsApply)
            {
                ApplyPixelBuffer();
            }
            
            if (currentStroke.points.Count > 1)
            {
                drawingData.strokes.Add(currentStroke);
            }
            
            currentStroke = null;
        }
        
        private void DrawBrush(float x, float y, bool applyImmediately = true)
        {
            int brushRadius = Mathf.RoundToInt(brushSize);
            int centerX = Mathf.RoundToInt(x);
            int centerY = Mathf.RoundToInt(y);
            
            // Draw to pixel buffer instead of texture directly
            for (int i = -brushRadius; i <= brushRadius; i++)
            {
                for (int j = -brushRadius; j <= brushRadius; j++)
                {
                    if (i * i + j * j <= brushRadius * brushRadius)
                    {
                        int pixelX = centerX + i;
                        int pixelY = centerY + j;
                        
                        if (pixelX >= 0 && pixelX < textureWidth && pixelY >= 0 && pixelY < textureHeight)
                        {
                            int index = pixelY * textureWidth + pixelX;
                            currentPixels[index] = brushColor;
                        }
                    }
                }
            }
            
            needsApply = true;
            
            if (applyImmediately && Time.time - lastApplyTime > APPLY_INTERVAL)
            {
                ApplyPixelBuffer();
            }
        }
        
        private void ApplyPixelBuffer()
        {
            drawingTexture.SetPixels(currentPixels);
            drawingTexture.Apply();
            lastApplyTime = Time.time;
            needsApply = false;
        }
        
        private void DrawLine(Vector2 start, Vector2 end)
        {
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.Max(2, Mathf.RoundToInt(distance));
            
            // Draw all points without applying
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                DrawBrush(point.x, point.y, false); // Don't apply yet
            }
            
            // Apply at controlled rate
            if (Time.time - lastApplyTime > APPLY_INTERVAL)
            {
                ApplyPixelBuffer();
            }
        }
        
        public byte[] GetDrawingData()
        {
            return drawingData.ToByteArray();
        }
        
        public void ClearCanvas()
        {
            // Reset both buffers
            System.Array.Copy(cleanColors, currentPixels, cleanColors.Length);
            drawingTexture.SetPixels(cleanColors);
            drawingTexture.Apply();
            
            drawingData = new DrawingData();
            drawingData.width = textureWidth;
            drawingData.height = textureHeight;
        }
        
        public void LoadDrawingData(byte[] data)
        {
            ClearCanvas();
            drawingData = DrawingData.FromByteArray(data);
            
            // Redraw all strokes
            Color[] pixels = (Color[])cleanColors.Clone();
            
            foreach (var stroke in drawingData.strokes)
            {
                DrawStrokeToPixels(pixels, stroke);
            }
            
            drawingTexture.SetPixels(pixels);
            drawingTexture.Apply();
        }
        
        private void DrawStrokeToPixels(Color[] pixels, Stroke stroke)
        {
            if (stroke.points.Count < 2) return;
            
            for (int i = 1; i < stroke.points.Count; i++)
            {
                Vector2 start = new Vector2(
                    stroke.points[i - 1].x * textureWidth,
                    stroke.points[i - 1].y * textureHeight
                );
                Vector2 end = new Vector2(
                    stroke.points[i].x * textureWidth,
                    stroke.points[i].y * textureHeight
                );
                
                DrawLineToPixels(pixels, start, end, stroke.color, stroke.thickness);
            }
        }
        
        private void DrawLineToPixels(Color[] pixels, Vector2 start, Vector2 end, Color color, float thickness)
        {
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.Max(2, Mathf.RoundToInt(distance));
            int radius = Mathf.Max(1, Mathf.RoundToInt(thickness));
            
            for (int step = 0; step <= steps; step++)
            {
                float t = step / (float)steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                
                for (int i = -radius; i <= radius; i++)
                {
                    for (int j = -radius; j <= radius; j++)
                    {
                        if (i * i + j * j <= radius * radius)
                        {
                            int x = Mathf.RoundToInt(point.x) + i;
                            int y = Mathf.RoundToInt(point.y) + j;
                            
                            if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
                            {
                                pixels[y * textureWidth + x] = color;
                            }
                        }
                    }
                }
            }
        }
        
        public void SetBrushSize(float size)
        {
            brushSize = Mathf.Clamp(size, 1f, 50f);
        }
        
        public void SetBrushColor(Color color)
        {
            brushColor = color;
        }
        
        public void Undo()
        {
            if (drawingData.strokes.Count > 0)
            {
                drawingData.strokes.RemoveAt(drawingData.strokes.Count - 1);
                
                // Redraw everything
                Color[] pixels = (Color[])cleanColors.Clone();
                foreach (var stroke in drawingData.strokes)
                {
                    DrawStrokeToPixels(pixels, stroke);
                }
                drawingTexture.SetPixels(pixels);
                drawingTexture.Apply();
            }
        }
    }
}