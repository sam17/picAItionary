using System;
using System.Collections.Generic;
using UnityEngine;

namespace Drawing
{
    /// <summary>
    /// Represents a complete drawing with all strokes
    /// </summary>
    [Serializable]
    public class DrawingData
    {
        public List<Stroke> strokes = new List<Stroke>();
        public int width = 512;
        public int height = 512;
        public long timestamp;
        
        public DrawingData()
        {
            timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
        
        /// <summary>
        /// Convert to byte array for network transmission
        /// </summary>
        public byte[] ToByteArray()
        {
            string json = JsonUtility.ToJson(this);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
        
        /// <summary>
        /// Create from byte array
        /// </summary>
        public static DrawingData FromByteArray(byte[] data)
        {
            if (data == null || data.Length == 0)
                return new DrawingData();
                
            string json = System.Text.Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<DrawingData>(json);
        }
        
        /// <summary>
        /// Convert to texture for display
        /// </summary>
        public Texture2D ToTexture()
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            
            // Clear to white
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            
            // Draw all strokes
            foreach (var stroke in strokes)
            {
                DrawStroke(pixels, stroke);
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
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
            int x0 = Mathf.RoundToInt(p1.x * width);
            int y0 = Mathf.RoundToInt(p1.y * height);
            int x1 = Mathf.RoundToInt(p2.x * width);
            int y1 = Mathf.RoundToInt(p2.y * height);
            
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            
            int radius = Mathf.Max(1, Mathf.RoundToInt(thickness));
            
            while (true)
            {
                // Draw a circle at this point for thickness
                for (int cy = -radius; cy <= radius; cy++)
                {
                    for (int cx = -radius; cx <= radius; cx++)
                    {
                        if (cx * cx + cy * cy <= radius * radius)
                        {
                            int px = x0 + cx;
                            int py = y0 + cy;
                            
                            if (px >= 0 && px < width && py >= 0 && py < height)
                            {
                                pixels[py * width + px] = color;
                            }
                        }
                    }
                }
                
                if (x0 == x1 && y0 == y1) break;
                
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
    }
    
    /// <summary>
    /// Represents a single stroke in the drawing
    /// </summary>
    [Serializable]
    public class Stroke
    {
        public List<Point> points = new List<Point>();
        public Color color = Color.black;
        public float thickness = 2f;
        
        public void AddPoint(float x, float y)
        {
            points.Add(new Point { x = x, y = y });
        }
    }
    
    /// <summary>
    /// Represents a point in the drawing (normalized 0-1)
    /// </summary>
    [Serializable]
    public struct Point
    {
        public float x;
        public float y;
    }
}