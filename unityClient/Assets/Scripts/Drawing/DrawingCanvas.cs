using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Drawing;
using UI;

namespace Drawing
{
    public class DrawingCanvas : MonoBehaviour, IDrawingCanvas, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Canvas Settings")]
        [SerializeField] private RectTransform drawingArea;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private GameObject lineRendererPrefab;
        
        [Header("Drawing Settings")]
        [SerializeField] private float defaultLineWidth = 0.05f;
        [SerializeField] private Color defaultColor = Color.black;
        [SerializeField] private int minPointDistance = 5;
        [SerializeField] private AnimationCurve widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
        
        [Header("Smoothing Settings")]
        [SerializeField] private bool enableSmoothing = true;
        [SerializeField] private float smoothingAmount = 0.3f;
        [SerializeField] private int interpolationSteps = 3;
        
        [Header("Performance")]
        [SerializeField] private int maxPointsPerStroke = 500;
        [SerializeField] private bool combineStrokes = true;
        
        private DrawingData drawingData;
        private Stroke currentStroke;
        private LineRenderer currentLineRenderer;
        private List<LineRenderer> lineRenderers = new List<LineRenderer>();
        private Vector2 lastMousePosition;
        private bool isDrawing = false;
        private Camera renderCamera;
        
        private float currentLineWidth;
        private Color currentColor;
        
        private Stack<int> undoStack = new Stack<int>();
        private List<GameObject> strokeObjects = new List<GameObject>();
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            drawingData = new DrawingData();
            currentLineWidth = defaultLineWidth;
            currentColor = defaultColor;
            
            if (drawingArea == null)
                drawingArea = GetComponent<RectTransform>();
            
            renderCamera = Camera.main;
            
            if (lineRendererPrefab == null)
            {
                CreateDefaultLineRendererPrefab();
            }
        }
        
        private void CreateDefaultLineRendererPrefab()
        {
            lineRendererPrefab = new GameObject("LineRendererPrefab");
            var lr = lineRendererPrefab.AddComponent<LineRenderer>();
            
            lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
            lr.widthCurve = widthCurve;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCapVertices = 10;
            lr.numCornerVertices = 10;
            lr.useWorldSpace = false;
            lr.sortingOrder = 1;
            
            lineRendererPrefab.SetActive(false);
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isDrawing)
            {
                StartNewStroke(eventData.position);
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (isDrawing)
            {
                AddPointToStroke(eventData.position);
            }
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            if (isDrawing)
            {
                EndStroke();
            }
        }
        
        private void StartNewStroke(Vector2 screenPosition)
        {
            isDrawing = true;
            
            currentStroke = new Stroke
            {
                color = currentColor,
                thickness = currentLineWidth * 10f
            };
            
            // Use object pool if available
            if (LineRendererPool.Instance != null)
            {
                currentLineRenderer = LineRendererPool.Instance.GetRenderer();
                if (currentLineRenderer != null)
                {
                    currentLineRenderer.transform.SetParent(transform, false);
                }
            }
            
            // Fallback to instantiation if pool not available or full
            if (currentLineRenderer == null)
            {
                GameObject strokeObj = Instantiate(lineRendererPrefab, transform);
                strokeObj.SetActive(true);
                strokeObjects.Add(strokeObj);
                currentLineRenderer = strokeObj.GetComponent<LineRenderer>();
            }
            
            currentLineRenderer.startWidth = currentLineWidth;
            currentLineRenderer.endWidth = currentLineWidth;
            currentLineRenderer.startColor = currentColor;
            currentLineRenderer.endColor = currentColor;
            currentLineRenderer.positionCount = 0;
            
            lineRenderers.Add(currentLineRenderer);
            
            Vector2 localPoint = ScreenToCanvasPosition(screenPosition);
            AddPointInternal(localPoint, true);
            lastMousePosition = screenPosition;
        }
        
        private void AddPointToStroke(Vector2 screenPosition)
        {
            if (!isDrawing || currentStroke == null) return;
            
            float distance = Vector2.Distance(screenPosition, lastMousePosition);
            if (distance < minPointDistance) return;
            
            if (currentStroke.points.Count >= maxPointsPerStroke)
            {
                EndStroke();
                StartNewStroke(screenPosition);
                return;
            }
            
            Vector2 localPoint = ScreenToCanvasPosition(screenPosition);
            
            if (enableSmoothing && currentLineRenderer.positionCount > 2)
            {
                SmoothLastPoints();
            }
            
            AddPointInternal(localPoint, false);
            lastMousePosition = screenPosition;
        }
        
        private void AddPointInternal(Vector2 localPoint, bool isFirstPoint)
        {
            float normalizedX = (localPoint.x + drawingArea.rect.width * 0.5f) / drawingArea.rect.width;
            float normalizedY = (localPoint.y + drawingArea.rect.height * 0.5f) / drawingArea.rect.height;
            
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);
            
            currentStroke.AddPoint(normalizedX, normalizedY);
            
            Vector3 worldPoint = new Vector3(localPoint.x, localPoint.y, 0);
            currentLineRenderer.positionCount++;
            currentLineRenderer.SetPosition(currentLineRenderer.positionCount - 1, worldPoint);
            
            if (!isFirstPoint && enableSmoothing && interpolationSteps > 0)
            {
                InterpolatePoints();
            }
        }
        
        private void SmoothLastPoints()
        {
            if (currentLineRenderer.positionCount < 3) return;
            
            int count = currentLineRenderer.positionCount;
            Vector3 p0 = currentLineRenderer.GetPosition(count - 3);
            Vector3 p1 = currentLineRenderer.GetPosition(count - 2);
            Vector3 p2 = currentLineRenderer.GetPosition(count - 1);
            
            Vector3 smoothed = p1 + (p0 + p2 - 2 * p1) * smoothingAmount;
            currentLineRenderer.SetPosition(count - 2, smoothed);
        }
        
        private void InterpolatePoints()
        {
            if (currentLineRenderer.positionCount < 2) return;
            
            int lastIndex = currentLineRenderer.positionCount - 1;
            Vector3 lastPoint = currentLineRenderer.GetPosition(lastIndex);
            Vector3 previousPoint = currentLineRenderer.GetPosition(lastIndex - 1);
            
            float distance = Vector3.Distance(lastPoint, previousPoint);
            if (distance > minPointDistance * 2)
            {
                int steps = Mathf.Min(interpolationSteps, Mathf.FloorToInt(distance / minPointDistance));
                for (int i = 1; i < steps; i++)
                {
                    float t = i / (float)steps;
                    Vector3 interpolated = Vector3.Lerp(previousPoint, lastPoint, t);
                    
                    currentLineRenderer.positionCount++;
                    for (int j = currentLineRenderer.positionCount - 1; j > lastIndex; j--)
                    {
                        currentLineRenderer.SetPosition(j, currentLineRenderer.GetPosition(j - 1));
                    }
                    currentLineRenderer.SetPosition(lastIndex, interpolated);
                    lastIndex++;
                }
            }
        }
        
        private void EndStroke()
        {
            if (!isDrawing || currentStroke == null) return;
            
            if (currentStroke.points.Count > 1)
            {
                drawingData.strokes.Add(currentStroke);
                undoStack.Push(drawingData.strokes.Count - 1);
                
                if (combineStrokes && lineRenderers.Count > 5)
                {
                    CombineOldStrokes();
                }
            }
            else
            {
                if (strokeObjects.Count > 0 && currentLineRenderer != null)
                {
                    Destroy(strokeObjects[strokeObjects.Count - 1]);
                    strokeObjects.RemoveAt(strokeObjects.Count - 1);
                    lineRenderers.Remove(currentLineRenderer);
                }
            }
            
            isDrawing = false;
            currentStroke = null;
            currentLineRenderer = null;
        }
        
        private Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                drawingArea, 
                screenPosition, 
                renderCamera, 
                out Vector2 localPoint
            );
            return localPoint;
        }
        
        private void CombineOldStrokes()
        {
            // Combine older strokes into a single mesh for performance
            // This is an optimization for complex drawings
        }
        
        public byte[] GetDrawingData()
        {
            return drawingData.ToByteArray();
        }
        
        public void ClearCanvas()
        {
            foreach (var obj in strokeObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            
            strokeObjects.Clear();
            lineRenderers.Clear();
            drawingData = new DrawingData();
            undoStack.Clear();
            isDrawing = false;
            currentStroke = null;
            currentLineRenderer = null;
        }
        
        public void LoadDrawingData(byte[] data)
        {
            ClearCanvas();
            drawingData = DrawingData.FromByteArray(data);
            
            foreach (var stroke in drawingData.strokes)
            {
                DrawStoredStroke(stroke);
            }
        }
        
        private void DrawStoredStroke(Stroke stroke)
        {
            if (stroke.points.Count < 2) return;
            
            GameObject strokeObj = Instantiate(lineRendererPrefab, transform);
            strokeObj.SetActive(true);
            strokeObjects.Add(strokeObj);
            
            LineRenderer lr = strokeObj.GetComponent<LineRenderer>();
            lr.startWidth = stroke.thickness / 10f;
            lr.endWidth = stroke.thickness / 10f;
            lr.startColor = stroke.color;
            lr.endColor = stroke.color;
            lr.positionCount = stroke.points.Count;
            
            for (int i = 0; i < stroke.points.Count; i++)
            {
                float x = stroke.points[i].x * drawingArea.rect.width - drawingArea.rect.width * 0.5f;
                float y = stroke.points[i].y * drawingArea.rect.height - drawingArea.rect.height * 0.5f;
                lr.SetPosition(i, new Vector3(x, y, 0));
            }
            
            lineRenderers.Add(lr);
        }
        
        public void Undo()
        {
            if (undoStack.Count > 0 && strokeObjects.Count > 0)
            {
                int lastIndex = undoStack.Pop();
                
                if (lastIndex < strokeObjects.Count)
                {
                    Destroy(strokeObjects[lastIndex]);
                    strokeObjects.RemoveAt(lastIndex);
                    
                    if (lastIndex < lineRenderers.Count)
                        lineRenderers.RemoveAt(lastIndex);
                    
                    if (lastIndex < drawingData.strokes.Count)
                        drawingData.strokes.RemoveAt(lastIndex);
                }
            }
        }
        
        public void SetBrushSize(float size)
        {
            currentLineWidth = Mathf.Clamp(size, 0.01f, 0.2f);
        }
        
        public void SetBrushColor(Color color)
        {
            currentColor = color;
        }
        
        private void OnDestroy()
        {
            if (lineRendererPrefab != null)
                Destroy(lineRendererPrefab);
        }
    }
}