using System.Collections.Generic;
using UnityEngine;

namespace Game.Modifiers
{
    [System.Serializable]
    public class ModifierData
    {
        public string name;
        public string description;
        public ModifierType type;
        public int id;
        
        public ModifierData(string name, string description, ModifierType type, int id)
        {
            this.name = name;
            this.description = description;
            this.type = type;
            this.id = id;
        }
    }
    
    public class ModifierManager : MonoBehaviour
    {
        private static ModifierManager instance;
        public static ModifierManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ModifierManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("ModifierManager");
                        instance = go.AddComponent<ModifierManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        private List<ModifierData> availableModifiers = new List<ModifierData>();
        private ModifierData currentModifier;
        private bool modifiersEnabled = false;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeModifiers();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeModifiers()
        {
            availableModifiers.Clear();
            
            // Register all available modifiers
            RegisterModifier(new ModifierData("Speed Draw", "Drawing time cut in half - 30 seconds only!", ModifierType.TimeModifier, 1));
            RegisterModifier(new ModifierData("Blind Options", "You only see the correct option while drawing!", ModifierType.DrawingRestriction, 2));
            RegisterModifier(new ModifierData("Half Hidden", "Half of your drawing will be hidden from guessers!", ModifierType.VisualEffect, 3));
            RegisterModifier(new ModifierData("Big Brush", "You're stuck with a huge brush size!", ModifierType.InputModifier, 4));
            RegisterModifier(new ModifierData("No Lift", "Don't lift your pen - drawing auto-submits when you do!", ModifierType.DrawingRestriction, 5));
            RegisterModifier(new ModifierData("Straight Lines", "All your lines snap to 90-degree angles!", ModifierType.InputModifier, 6));
            RegisterModifier(new ModifierData("Non-Dominant Hand", "Draw with your non-dominant hand!", ModifierType.Other, 7));
            RegisterModifier(new ModifierData("Lucky!", "No modifier - draw normally!", ModifierType.Other, 8));
            
            Debug.Log($"ModifierManager: Initialized with {availableModifiers.Count} modifiers");
        }
        
        public void RegisterModifier(ModifierData modifier)
        {
            if (!availableModifiers.Exists(m => m.id == modifier.id))
            {
                availableModifiers.Add(modifier);
            }
        }
        
        public void SetModifiersEnabled(bool enabled)
        {
            modifiersEnabled = enabled;
            PlayerPrefs.SetInt("ModifiersEnabled", enabled ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"ModifierManager: Modifiers set to {(enabled ? "ENABLED" : "DISABLED")}");
        }
        
        public bool AreModifiersEnabled()
        {
            return modifiersEnabled;
        }
        
        public ModifierData SelectRandomModifier()
        {
            Debug.Log($"ModifierManager: SelectRandomModifier called. Enabled: {modifiersEnabled}, Count: {availableModifiers.Count}");
            
            if (!modifiersEnabled || availableModifiers.Count == 0)
            {
                currentModifier = null;
                Debug.Log("ModifierManager: No modifier selected (disabled or no modifiers available)");
                return null;
            }
            
            int randomIndex = Random.Range(0, availableModifiers.Count);
            currentModifier = availableModifiers[randomIndex];
            Debug.Log($"ModifierManager: Selected modifier: {currentModifier.name}");
            return currentModifier;
        }
        
        public ModifierData GetCurrentModifier()
        {
            return currentModifier;
        }
        
        public void ClearCurrentModifier()
        {
            currentModifier = null;
        }
        
        public List<ModifierData> GetAvailableModifiers()
        {
            return new List<ModifierData>(availableModifiers);
        }
        
        public void LoadModifierSettings()
        {
            modifiersEnabled = PlayerPrefs.GetInt("ModifiersEnabled", 0) == 1;
        }
    }
}