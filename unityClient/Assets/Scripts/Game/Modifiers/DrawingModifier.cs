using UnityEngine;

namespace Game.Modifiers
{
    [System.Serializable]
    public abstract class DrawingModifier : ScriptableObject
    {
        public string modifierName;
        public string description;
        public ModifierType type;
        
        public abstract void ApplyModifier(UI.DrawingScreen drawingScreen);
        public abstract void RemoveModifier(UI.DrawingScreen drawingScreen);
        
        public virtual string GetInstructions()
        {
            return description;
        }
    }
    
    public enum ModifierType
    {
        DrawingRestriction,
        TimeModifier,
        VisualEffect,
        InputModifier,
        Other
    }
}