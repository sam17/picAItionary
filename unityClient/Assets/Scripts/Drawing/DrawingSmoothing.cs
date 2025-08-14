using System.Collections.Generic;
using UnityEngine;

namespace Drawing
{
    public static class DrawingSmoothing
    {
        public static List<Vector2> SmoothPoints(List<Vector2> inputPoints, int subdivisions = 3, float tension = 0.5f)
        {
            if (inputPoints == null || inputPoints.Count < 3)
                return inputPoints;
            
            List<Vector2> smoothedPoints = new List<Vector2>();
            
            // Add first point
            smoothedPoints.Add(inputPoints[0]);
            
            // Apply Catmull-Rom spline interpolation
            for (int i = 0; i < inputPoints.Count - 1; i++)
            {
                Vector2 p0 = i == 0 ? inputPoints[0] : inputPoints[i - 1];
                Vector2 p1 = inputPoints[i];
                Vector2 p2 = inputPoints[i + 1];
                Vector2 p3 = i == inputPoints.Count - 2 ? inputPoints[inputPoints.Count - 1] : inputPoints[i + 2];
                
                for (int j = 1; j <= subdivisions; j++)
                {
                    float t = j / (float)(subdivisions + 1);
                    Vector2 interpolated = CatmullRom(p0, p1, p2, p3, t, tension);
                    smoothedPoints.Add(interpolated);
                }
                
                if (i < inputPoints.Count - 2)
                {
                    smoothedPoints.Add(p2);
                }
            }
            
            // Add last point
            smoothedPoints.Add(inputPoints[inputPoints.Count - 1]);
            
            return smoothedPoints;
        }
        
        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t, float tension)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            
            float v0 = (p2.x - p0.x) * tension;
            float v1 = (p3.x - p1.x) * tension;
            float a = 2 * p1.x - 2 * p2.x + v0 + v1;
            float b = -3 * p1.x + 3 * p2.x - 2 * v0 - v1;
            float c = v0;
            float d = p1.x;
            
            float x = a * t3 + b * t2 + c * t + d;
            
            v0 = (p2.y - p0.y) * tension;
            v1 = (p3.y - p1.y) * tension;
            a = 2 * p1.y - 2 * p2.y + v0 + v1;
            b = -3 * p1.y + 3 * p2.y - 2 * v0 - v1;
            c = v0;
            d = p1.y;
            
            float y = a * t3 + b * t2 + c * t + d;
            
            return new Vector2(x, y);
        }
        
        public static List<Vector2> SimplifyDouglasPeucker(List<Vector2> points, float tolerance)
        {
            if (points == null || points.Count < 3)
                return points;
            
            List<Vector2> simplified = new List<Vector2>();
            SimplifyDouglasPeuckerRecursive(points, 0, points.Count - 1, tolerance, simplified);
            return simplified;
        }
        
        private static void SimplifyDouglasPeuckerRecursive(List<Vector2> points, int startIndex, int endIndex, 
            float tolerance, List<Vector2> simplified)
        {
            float maxDistance = 0;
            int maxIndex = 0;
            
            for (int i = startIndex + 1; i < endIndex; i++)
            {
                float distance = PerpendicularDistance(points[i], points[startIndex], points[endIndex]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    maxIndex = i;
                }
            }
            
            if (maxDistance > tolerance)
            {
                SimplifyDouglasPeuckerRecursive(points, startIndex, maxIndex, tolerance, simplified);
                SimplifyDouglasPeuckerRecursive(points, maxIndex, endIndex, tolerance, simplified);
            }
            else
            {
                if (simplified.Count == 0 || simplified[simplified.Count - 1] != points[startIndex])
                {
                    simplified.Add(points[startIndex]);
                }
                simplified.Add(points[endIndex]);
            }
        }
        
        private static float PerpendicularDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            float dx = lineEnd.x - lineStart.x;
            float dy = lineEnd.y - lineStart.y;
            
            if (dx == 0 && dy == 0)
            {
                return Vector2.Distance(point, lineStart);
            }
            
            float t = ((point.x - lineStart.x) * dx + (point.y - lineStart.y) * dy) / (dx * dx + dy * dy);
            t = Mathf.Clamp01(t);
            
            Vector2 projection = new Vector2(lineStart.x + t * dx, lineStart.y + t * dy);
            return Vector2.Distance(point, projection);
        }
        
        public static List<Vector2> ApplyVelocitySmoothing(List<Vector2> points, List<float> timestamps)
        {
            if (points.Count != timestamps.Count || points.Count < 3)
                return points;
            
            List<Vector2> smoothed = new List<Vector2>();
            smoothed.Add(points[0]);
            
            for (int i = 1; i < points.Count - 1; i++)
            {
                float dt1 = timestamps[i] - timestamps[i - 1];
                float dt2 = timestamps[i + 1] - timestamps[i];
                
                if (dt1 > 0 && dt2 > 0)
                {
                    float velocity1 = Vector2.Distance(points[i], points[i - 1]) / dt1;
                    float velocity2 = Vector2.Distance(points[i + 1], points[i]) / dt2;
                    
                    float smoothingFactor = Mathf.Clamp01(1f - Mathf.Abs(velocity1 - velocity2) / 100f);
                    
                    Vector2 smoothedPoint = Vector2.Lerp(
                        points[i],
                        (points[i - 1] + points[i] + points[i + 1]) / 3f,
                        smoothingFactor * 0.5f
                    );
                    
                    smoothed.Add(smoothedPoint);
                }
                else
                {
                    smoothed.Add(points[i]);
                }
            }
            
            smoothed.Add(points[points.Count - 1]);
            return smoothed;
        }
        
        public static List<Vector2> ApplyChaikinSmoothing(List<Vector2> points, int iterations = 2)
        {
            if (points == null || points.Count < 3)
                return points;
            
            List<Vector2> current = new List<Vector2>(points);
            
            for (int iter = 0; iter < iterations; iter++)
            {
                List<Vector2> smoothed = new List<Vector2>();
                smoothed.Add(current[0]);
                
                for (int i = 0; i < current.Count - 1; i++)
                {
                    Vector2 p1 = current[i];
                    Vector2 p2 = current[i + 1];
                    
                    Vector2 q = p1 + 0.25f * (p2 - p1);
                    Vector2 r = p1 + 0.75f * (p2 - p1);
                    
                    if (i > 0)
                    {
                        smoothed.Add(q);
                    }
                    
                    if (i < current.Count - 2)
                    {
                        smoothed.Add(r);
                    }
                }
                
                smoothed.Add(current[current.Count - 1]);
                current = smoothed;
            }
            
            return current;
        }
    }
}