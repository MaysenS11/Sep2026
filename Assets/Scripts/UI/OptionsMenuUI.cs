using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Settings;

namespace UI
{
    /// <summary>
    /// Options menu controller with FMOD volume sliders, key rebinding with double-binding validation, and credits.
    /// </summary>
    public class OptionsMenuUI : MonoBehaviour
    {
        [Header("Root Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Audio Sliders")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Rebinding UI")]
        [SerializeField] private TMP_Text rebindingPromptText;
        [SerializeField] private TMP_Text errorMessageText;
        [SerializeField] private Button resetBindingsButton;

        [System.Serializable]
        public struct ActionBindButton
        {
            public string actionName;
            public Button button;
            public TMP_Text buttonLabel;
        }

        [SerializeField] private ActionBindButton[] actionButtons;

        [Header("Credits")]
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button closeCreditsButton;

        [Header("Navigation")]
        [SerializeField] private Button closeOptionsButton;

        private string awaitingRebindAction = null;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;

            if (masterVolumeSlider != null)
            {
                float savedMaster = PlayerPrefs.GetFloat("FMOD_MasterVolume", 1f);
                masterVolumeSlider.value = savedMaster;
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                float savedSFX = PlayerPrefs.GetFloat("FMOD_SFXVolume", 1f);
                sfxVolumeSlider.value = savedSFX;
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            if (resetBindingsButton != null)
            {
                resetBindingsButton.onClick.AddListener(OnResetBindingsClicked);
            }

            if (closeOptionsButton != null)
            {
                closeOptionsButton.onClick.AddListener(CloseOptions);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.AddListener(() => SetCreditsVisible(true));
            }

            if (closeCreditsButton != null)
            {
                closeCreditsButton.onClick.AddListener(() => SetCreditsVisible(false));
            }

            BindActionButtons();
        }

        private void OnEnable()
        {
            KeyBindingManager.OnBindingsChanged += RefreshActionLabels;
            RefreshActionLabels();
            ClearMessages();
            if (creditsPanel != null) creditsPanel.SetActive(false);
        }

        private void OnDisable()
        {
            KeyBindingManager.OnBindingsChanged -= RefreshActionLabels;
            awaitingRebindAction = null;
        }

        private void BindActionButtons()
        {
            if (actionButtons == null) return;

            for (int i = 0; i < actionButtons.Length; i++)
            {
                int index = i;
                if (actionButtons[index].button != null)
                {
                    actionButtons[index].button.onClick.AddListener(() =>
                    {
                        StartRebinding(actionButtons[index].actionName);
                    });
                }
            }
        }

        public void OpenOptions()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            RefreshActionLabels();
            ClearMessages();
        }

        public void CloseOptions()
        {
            awaitingRebindAction = null;
            ClearMessages();
            if (panelRoot != null && panelRoot != gameObject) panelRoot.SetActive(false);
            else gameObject.SetActive(false);
        }

        public void SetCreditsVisible(bool visible)
        {
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(visible);
            }
        }

        public void OnMasterVolumeChanged(float value)
        {
            UIAudioManager.SetMasterVolume(value);
            PlayerPrefs.SetFloat("FMOD_MasterVolume", value);
            PlayerPrefs.Save();
        }

        public void OnSFXVolumeChanged(float value)
        {
            UIAudioManager.SetSFXVolume(value);
            PlayerPrefs.SetFloat("FMOD_SFXVolume", value);
            PlayerPrefs.Save();
        }

        public void StartRebinding(string actionName)
        {
            awaitingRebindAction = actionName;
            if (rebindingPromptText != null)
            {
                rebindingPromptText.text = $"Press any key to rebind [{actionName}] (Escape to cancel)...";
                rebindingPromptText.gameObject.SetActive(true);
            }
            if (errorMessageText != null)
            {
                errorMessageText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(awaitingRebindAction)) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                awaitingRebindAction = null;
                ClearMessages();
                return;
            }

            foreach (KeyCode k in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(k))
                {
                    if (k == KeyCode.Escape) continue;

                    string action = awaitingRebindAction;
                    awaitingRebindAction = null;

                    bool success = KeyBindingManager.TryRebind(action, k, out string error);
                    if (success)
                    {
                        ClearMessages();
                        RefreshActionLabels();
                    }
                    else
                    {
                        ShowError(error);
                        RefreshActionLabels();
                    }
                    break;
                }
            }
        }

        public void RefreshActionLabels()
        {
            if (actionButtons == null) return;

            for (int i = 0; i < actionButtons.Length; i++)
            {
                if (actionButtons[i].buttonLabel != null)
                {
                    KeyCode currentKey = KeyBindingManager.GetBinding(actionButtons[i].actionName);
                    actionButtons[i].buttonLabel.text = currentKey != KeyCode.None ? currentKey.ToString() : "None";
                }
            }
        }

        private void OnResetBindingsClicked()
        {
            KeyBindingManager.ResetToDefaults();
            ClearMessages();
            RefreshActionLabels();
        }

        private void ShowError(string message)
        {
            if (rebindingPromptText != null) rebindingPromptText.gameObject.SetActive(false);
            if (errorMessageText != null)
            {
                errorMessageText.text = message;
                errorMessageText.gameObject.SetActive(true);
            }
        }

        private void ClearMessages()
        {
            if (rebindingPromptText != null) rebindingPromptText.gameObject.SetActive(false);
            if (errorMessageText != null) errorMessageText.gameObject.SetActive(false);
        }
    }
}
