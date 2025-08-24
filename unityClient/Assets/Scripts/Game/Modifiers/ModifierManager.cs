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
            RegisterModifier(new ModifierData("Speed Draw", "Drawing time cut in half!", ModifierType.TimeModifier, 1));
            RegisterModifier(new ModifierData("Lucky!", "No modifier - draw normally!", ModifierType.Other, 2));
            
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