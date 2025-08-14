using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Drawing;
using UI;

namespace Drawing
{
    /// <summary>
    /// Alternative drawing canvas with different coordinate mapping approach
    /// Use this if UIDrawingCanvas has offset issues
    /// </summary>
    public class UIDrawingCanvasAlt : MonoBehaviour, IDrawingCanvas, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Canvas Settings")]
        [SerializeField] private RawImage drawingImage;
        [SerializeField] private int textureWidth = 512;
        [SerializeField] private int textureHeight = 512;
        
        [Header("Drawing Settings")]
        [SerializeField] private float brushSize = 5f;
        [SerializeField] private Color brushColor = Color.black;
        [SerializeField] private bool smoothLines = true;
        
        private Texture2D drawingTexture;
        private Color[] cleanColors;
        private DrawingData drawingData;
        private Stroke currentStroke;
        private bool isDrawing = false;
        private Vector2 lastDrawPoint;
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Initialize()
        {
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
            drawingTexture.filterMode = FilterMode.Point; // Use Point for pixel-perfect drawing
            
            // Initialize with white background
            cleanColors = new Color[textureWidth * textureHeight];
            for (int i = 0; i < cleanColors.Length; i++)
            {
                cleanColors[i] = Color.white;
            }
            drawingTexture.SetPixels(cleanColors);
            drawingTexture.Apply();
            
            // Set texture to RawImage
            drawingImage.texture = drawingTexture;
            
            // Initialize drawing data
            drawingData = new DrawingData();
            drawingData.width = textureWidth;
            drawingData.height = textureHeight;
            
            Debug.Log($"UIDrawingCanvasAlt: Initialized with {textureWidth}x{textureHeight} texture");
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            Vector2 texturePoint;
            if (GetTextureCoordinate(eventData, out texturePoint))
            {
                StartNewStroke(texturePoint);
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!isDrawing) return;
            
            Vector2 texturePoint;
            if (GetTextureCoordinate(eventData, out texturePoint))
            {
                AddPointToStroke(texturePoint);
            }
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            if (isDrawing)
            {
                EndStroke();
            }
        }
        
        private bool GetTextureCoordinate(PointerEventData eventData, out Vector2 texturePoint)
        {
            // Get the RectTransform of the RawImage
            RectTransform imageRect = drawingImage.rectTransform;
            
            // Check if the pointer is over the image
            if (!RectTransformUtility.RectangleContainsScreenPoint(imageRect, eventData.position, eventData.pressEventCamera))
            {
                texturePoint = Vector2.zero;
                return false;
            }
            
            // Convert screen position to local position in the RawImage's rect
            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                imageRect, 
                eventData.position, 
                eventData.pressEventCamera, 
                out localCursor))
            {
                texturePoint = Vector2.zero;
                return false;
            }
            
            // Get the rect of the RawImage
            Rect rect = imageRect.rect;
            
            // Calculate UV coordinates (0 to 1)
            float uvX = (localCursor.x - rect.x) / rect.width;
            float uvY = (localCursor.y - rect.y) / rect.height;
            
            // Handle RawImage UV rect (in case the image is cropped or scaled)
            Rect uvRect = drawingImage.uvRect;
            uvX = uvX * uvRect.width + uvRect.x;
            uvY = uvY * uvRect.height + uvRect.y;
            
            // Convert UV to texture pixel coordinates
            texturePoint = new Vector2(
                Mathf.Clamp(uvX * textureWidth, 0, textureWidth - 1),
                Mathf.Clamp(uvY * textureHeight, 0, textureHeight - 1)
            );
            
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
            if (smoothLines && Vector2.Distance(lastDrawPoint, texturePoint) > 0.1f)
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
            
            if (currentStroke.points.Count > 1)
            {
                drawingData.strokes.Add(currentStroke);
            }
            
            currentStroke = null;
        }
        
        private void DrawBrush(float x, float y)
        {
            int centerX = Mathf.RoundToInt(x);
            int centerY = Mathf.RoundToInt(y);
            int radius = Mathf.RoundToInt(brushSize);
            
            // Use a more efficient circle drawing algorithm
            for (int py = -radius; py <= radius; py++)
            {
                for (int px = -radius; px <= radius; px++)
                {
                    if (px * px + py * py <= radius * radius)
                    {
                        int pixelX = centerX + px;
                        int pixelY = centerY + py;
                        
                        if (pixelX >= 0 && pixelX < textureWidth && pixelY >= 0 && pixelY < textureHeight)
                        {
                            // Add anti-aliasing at the edges
                            float distance = Mathf.Sqrt(px * px + py * py);
                            float alpha = 1f;
                            
                            if (distance > radius - 1)
                            {
                                alpha = radius - distance;
                            }
                            
                            if (alpha > 0)
                            {
                                Color currentColor = drawingTexture.GetPixel(pixelX, pixelY);
                                Color newColor = Color.Lerp(currentColor, brushColor, alpha);
                                drawingTexture.SetPixel(pixelX, pixelY, newColor);
                            }
                        }
                    }
                }
            }
            
            drawingTexture.Apply();
        }
        
        private void DrawLine(Vector2 start, Vector2 end)
        {
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.Max(2, Mathf.RoundToInt(distance * 2)); // More steps for smoother lines
            
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                DrawBrush(point.x, point.y);
            }
        }
        
        public byte[] GetDrawingData()
        {
            return drawingData.ToByteArray();
        }
        
        public void ClearCanvas()
        {
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
            foreach (var stroke in drawingData.strokes)
            {
                RedrawStroke(stroke);
            }
        }
        
        private void RedrawStroke(Stroke stroke)
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
                
                // Set brush color temporarily
                Color oldColor = brushColor;
                float oldSize = brushSize;
                brushColor = stroke.color;
                brushSize = stroke.thickness;
                
                DrawLine(start, end);
                
                // Restore brush settings
                brushColor = oldColor;
                brushSize = oldSize;
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
                
                // Clear and redraw
                drawingTexture.SetPixels(cleanColors);
                drawingTexture.Apply();
                
                foreach (var stroke in drawingData.strokes)
                {
                    RedrawStroke(stroke);
                }
            }
        }
    }
}