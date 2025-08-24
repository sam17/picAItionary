using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Game.Modifiers;

namespace Drawing
{
    public class DrawingModifierHandler : MonoBehaviour
    {
        [Header("Drawing Settings")]
        [SerializeField] private float normalBrushSize = 5f;
        [SerializeField] private float bigBrushSize = 20f;
        
        [Header("References")]
        [SerializeField] private UIDrawingCanvas drawingCanvas;
        [SerializeField] private Slider brushSizeSlider;
        
        private ModifierData currentModifier;
        private bool isNoLiftActive = false;
        private bool hasLifted = false;
        private bool isStraightLinesActive = false;
        private Vector2 lastMousePosition;
        private bool isDrawing = false;
        
        private void Start()
        {
            if (drawingCanvas == null)
            {
                drawingCanvas = GetComponent<UIDrawingCanvas>();
            }
        }
        
        public void ApplyModifier(ModifierData modifier)
        {
            currentModifier = modifier;
            
            if (modifier == null)
            {
                ResetToNormal();
                return;
            }
            
            switch (modifier.name)
            {
                case "Big Brush":
                    ApplyBigBrush();
                    break;
                case "No Lift":
                    ApplyNoLift();
                    break;
                case "Straight Lines":
                    ApplyStraightLines();
                    break;
                default:
                    ResetToNormal();
                    break;
            }
        }
        
        private void ApplyBigBrush()
        {
            if (drawingCanvas != null)
            {
                drawingCanvas.SetBrushSize(bigBrushSize);
            }
            
            if (brushSizeSlider != null)
            {
                brushSizeSlider.interactable = false;
                brushSizeSlider.value = bigBrushSize;
            }
        }
        
        private void ApplyNoLift()
        {
            isNoLiftActive = true;
            hasLifted = false;
        }
        
        private void ApplyStraightLines()
        {
            isStraightLinesActive = true;
        }
        
        private void ResetToNormal()
        {
            if (drawingCanvas != null)
            {
                drawingCanvas.SetBrushSize(normalBrushSize);
            }
            
            if (brushSizeSlider != null)
            {
                brushSizeSlider.interactable = true;
                brushSizeSlider.value = normalBrushSize;
            }
            
            isNoLiftActive = false;
            hasLifted = false;
            isStraightLinesActive = false;
        }
        
        private void Update()
        {
            if (isNoLiftActive)
            {
                HandleNoLiftModifier();
            }
            
            if (isStraightLinesActive)
            {
                HandleStraightLinesModifier();
            }
        }
        
        private void HandleNoLiftModifier()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (hasLifted)
                {
                    Debug.Log("DrawingModifierHandler: Pen lifted! Auto-submitting drawing.");
                    var drawingScreen = GetComponentInParent<UI.DrawingScreen>();
                    if (drawingScreen != null)
                    {
                        drawingScreen.ForceSubmitDrawing();
                    }
                }
                isDrawing = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (isDrawing)
                {
                    hasLifted = true;
                    isDrawing = false;
                }
            }
        }
        
        private void HandleStraightLinesModifier()
        {
            if (!drawingCanvas) return;
            
            if (Input.GetMouseButtonDown(0))
            {
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0))
            {
                Vector2 currentPos = Input.mousePosition;
                Vector2 delta = currentPos - lastMousePosition;
                
                if (delta.magnitude > 10f)
                {
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    {
                        currentPos.y = lastMousePosition.y;
                    }
                    else
                    {
                        currentPos.x = lastMousePosition.x;
                    }
                    
                    drawingCanvas.ConstrainDrawingPosition(currentPos);
                    lastMousePosition = currentPos;
                }
            }
        }
        
        private void OnDisable()
        {
            ResetToNormal();
        }
    }
}