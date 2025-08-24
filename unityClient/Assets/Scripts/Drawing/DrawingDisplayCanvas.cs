using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Drawing;
using UI;

namespace Drawing
{
    /// <summary>
    /// Read-only canvas for displaying drawings (used in GuessingScreen, ResultsScreen, etc.)
    /// Inherits from UIDrawingCanvas but disables all input
    /// </summary>
    public class DrawingDisplayCanvas : MonoBehaviour, IDrawingCanvas
    {
        [Header("Display Settings")]
        [SerializeField] private RawImage displayImage;
        [SerializeField] private int textureWidth = 512;
        [SerializeField] private int textureHeight = 512;
        [SerializeField] private Color backgroundColor = Color.white;
        
        private Texture2D displayTexture;
        private DrawingData loadedDrawingData;
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            // Create or get RawImage component
            if (displayImage == null)
            {
                displayImage = GetComponent<RawImage>();
                if (displayImage == null)
                {
                    displayImage = gameObject.AddComponent<RawImage>();
                }
            }
            
            // Create display texture
            displayTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            displayTexture.filterMode = FilterMode.Bilinear;
            
            // Set to RawImage
            displayImage.texture = displayTexture;
            
            // Make sure the RawImage is not raycast target to prevent accidental interactions
            displayImage.raycastTarget = false;
            
            // Clear to background color
            ClearCanvas();
            
        }
        
        public void LoadDrawingData(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                ClearCanvas();
                return;
            }
            
            // Parse the drawing data
            try
            {
                loadedDrawingData = DrawingData.FromByteArray(data);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"DrawingDisplayCanvas: Failed to parse drawing data: {e.Message}");
                ClearCanvas();
                return;
            }
            
            if (loadedDrawingData == null || loadedDrawingData.strokes == null)
            {
                ClearCanvas();
                return;
            }
            
            // Create pixel array
            Color[] pixels = new Color[textureWidth * textureHeight];
            
            // Fill with background color
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = backgroundColor;
            }
            
            // Draw all strokes
            foreach (var stroke in loadedDrawingData.strokes)
            {
                DrawStroke(pixels, stroke);
            }
            
            // Apply to texture
            displayTexture.SetPixels(pixels);
            displayTexture.Apply();
        }
        
        private void DrawStroke(Color[] pixels, Stroke stroke)
        {
            if (stroke.points.Count < 2) return;
            
            for (int i = 1; i < stroke.points.Count; i++)
            {
                DrawLine(pixels, stroke.points[i - 1], stroke.points[i], stroke.color, stroke.thickness);
            }
        }
        
        private void DrawLine(Color[] pixels, Point p1, Point p2, Color color, float thickness)
        {
            // Convert normalized coordinates to texture coordinates
            Vector2 start = new Vector2(p1.x * textureWidth, p1.y * textureHeight);
            Vector2 end = new Vector2(p2.x * textureWidth, p2.y * textureHeight);
            
            float distance = Vector2.Distance(start, end);
            int steps = Mathf.Max(2, Mathf.RoundToInt(distance * 2));
            
            for (int step = 0; step <= steps; step++)
            {
                float t = step / (float)steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                
                DrawPoint(pixels, point, color, thickness);
            }
        }
        
        private void DrawPoint(Color[] pixels, Vector2 point, Color color, float thickness)
        {
            int radius = Mathf.Max(1, Mathf.RoundToInt(thickness));
            int centerX = Mathf.RoundToInt(point.x);
            int centerY = Mathf.RoundToInt(point.y);
            
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        int pixelX = centerX + x;
                        int pixelY = centerY + y;
                        
                        if (pixelX >= 0 && pixelX < textureWidth && pixelY >= 0 && pixelY < textureHeight)
                        {
                            int index = pixelY * textureWidth + pixelX;
                            
                            // Anti-aliasing at edges
                            float distance = Mathf.Sqrt(x * x + y * y);
                            if (distance > radius - 1)
                            {
                                float alpha = radius - distance;
                                pixels[index] = Color.Lerp(pixels[index], color, alpha);
                            }
                            else
                            {
                                pixels[index] = color;
                            }
                        }
                    }
                }
            }
        }
        
        public void ClearCanvas()
        {
            Color[] pixels = new Color[textureWidth * textureHeight];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = backgroundColor;
            }
            displayTexture.SetPixels(pixels);
            displayTexture.Apply();
            
            loadedDrawingData = null;
        }
        
        public byte[] GetDrawingData()
        {
            // Return the loaded data if available
            if (loadedDrawingData != null)
            {
                return loadedDrawingData.ToByteArray();
            }
            return new byte[0];
        }
        
        /// <summary>
        /// Sets the display size while maintaining aspect ratio
        /// </summary>
        public void SetDisplaySize(int width, int height)
        {
            if (width != textureWidth || height != textureHeight)
            {
                textureWidth = width;
                textureHeight = height;
                Initialize();
                
                // Reload drawing if we had one
                if (loadedDrawingData != null)
                {
                    LoadDrawingData(loadedDrawingData.ToByteArray());
                }
            }
        }
    }
}