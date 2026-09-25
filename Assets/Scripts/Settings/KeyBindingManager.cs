using System;
using System.Collections.Generic;
using UnityEngine;

namespace Settings
{
    /// <summary>
    /// Authoritative manager for keybindings with double-binding validation.
    /// Prevents assigning the same key to multiple actions and persists bindings in PlayerPrefs.
    /// </summary>
    public static class KeyBindingManager
    {
        public const string ActionMoveUp = "MoveUp";
        public const string ActionMoveDown = "MoveDown";
        public const string ActionMoveLeft = "MoveLeft";
        public const string ActionMoveRight = "MoveRight";
        public const string ActionAttack = "Attack";
        public const string ActionThreatOverlay = "ThreatOverlay";

        private static readonly Dictionary<string, KeyCode> s_DefaultBindings = new Dictionary<string, KeyCode>
        {
            { ActionMoveUp, KeyCode.W },
            { ActionMoveDown, KeyCode.S },
            { ActionMoveLeft, KeyCode.A },
            { ActionMoveRight, KeyCode.D },
            { ActionAttack, KeyCode.Space },
            { ActionThreatOverlay, KeyCode.Tab }
        };

        private static readonly Dictionary<string, KeyCode> s_ActiveBindings = new Dictionary<string, KeyCode>();
        private static bool s_IsInitialized = false;

        public static event Action OnBindingsChanged;

        static KeyBindingManager()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (s_IsInitialized) return;

            s_ActiveBindings.Clear();
            foreach (var kvp in s_DefaultBindings)
            {
                string prefKey = $"keybind_{kvp.Key}";
                string savedVal = PlayerPrefs.GetString(prefKey, string.Empty);
                if (!string.IsNullOrEmpty(savedVal) && Enum.TryParse<KeyCode>(savedVal, out var savedCode))
                {
                    s_ActiveBindings[kvp.Key] = savedCode;
                }
                else
                {
                    s_ActiveBindings[kvp.Key] = kvp.Value;
                }
            }

            s_IsInitialized = true;
        }

        public static KeyCode GetBinding(string actionName)
        {
            Initialize();
            if (s_ActiveBindings.TryGetValue(actionName, out var code))
            {
                return code;
            }
            if (s_DefaultBindings.TryGetValue(actionName, out var defCode))
            {
                return defCode;
            }
            return KeyCode.None;
        }

        public static IReadOnlyDictionary<string, KeyCode> GetAllBindings()
        {
            Initialize();
            return new Dictionary<string, KeyCode>(s_ActiveBindings);
        }

        /// <summary>
        /// Attempts to rebind the specified action to a new key.
        /// Validates that the key is valid and prevents double-binding across actions.
        /// </summary>
        public static bool TryRebind(string actionName, KeyCode newKey, out string errorMessage)
        {
            Initialize();
            errorMessage = null;

            if (!s_DefaultBindings.ContainsKey(actionName))
            {
                errorMessage = $"Unknown action '{actionName}'.";
                return false;
            }

            if (newKey == KeyCode.None)
            {
                errorMessage = "Cannot bind action to None.";
                return false;
            }

            // Check for double-binding validation
            foreach (var kvp in s_ActiveBindings)
            {
                if (kvp.Key != actionName && kvp.Value == newKey)
                {
                    errorMessage = $"Key '{newKey}' is already bound to '{kvp.Key}'. Double-binding is not allowed.";
                    return false;
                }
            }

            s_ActiveBindings[actionName] = newKey;
            PlayerPrefs.SetString($"keybind_{actionName}", newKey.ToString());
            PlayerPrefs.Save();

            OnBindingsChanged?.Invoke();
            return true;
        }

        public static void ResetToDefaults()
        {
            s_ActiveBindings.Clear();
            foreach (var kvp in s_DefaultBindings)
            {
                s_ActiveBindings[kvp.Key] = kvp.Value;
                PlayerPrefs.DeleteKey($"keybind_{kvp.Key}");
            }
            PlayerPrefs.Save();
            OnBindingsChanged?.Invoke();
        }

        public static bool IsActionPressed(string actionName)
        {
            KeyCode code = GetBinding(actionName);
            if (code == KeyCode.None) return false;
            try
            {
                return Input.GetKey(code);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsActionDown(string actionName)
        {
            KeyCode code = GetBinding(actionName);
            if (code == KeyCode.None) return false;
            try
            {
                return Input.GetKeyDown(code);
            }
            catch
            {
                return false;
            }
        }
    }
}
