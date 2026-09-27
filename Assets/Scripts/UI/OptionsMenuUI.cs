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

        [Header("Sub-Panels")]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject controlsPanel;
        [SerializeField] private GameObject audioPanel;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;

            DiscoverPanelsAndButtons();
            SetupSliders();
            SetupActionButtons();

            if (resetBindingsButton != null)
            {
                resetBindingsButton.onClick.AddListener(OnResetBindingsClicked);
            }

            if (closeOptionsButton != null)
            {
                closeOptionsButton.onClick.AddListener(OnBackClicked);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.AddListener(ShowCredits);
            }

            if (closeCreditsButton != null)
            {
                closeCreditsButton.onClick.AddListener(ShowOptionsHub);
            }

            BindActionButtons();
        }

        private void OnEnable()
        {
            KeyBindingManager.OnBindingsChanged += RefreshActionLabels;
            RefreshActionLabels();
            ClearMessages();
            ShowOptionsHub();
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

        public void SetCreditsVisible(bool visible)
        {
            if (visible) ShowCredits();
            else ShowOptionsHub();
        }

        private void DiscoverPanelsAndButtons()
        {
            // Auto discover subpanels under panelRoot or this object
            Transform root = panelRoot != null ? panelRoot.transform : transform;

            if (optionsPanel == null)
            {
                var t = root.Find("OptionsPanel");
                if (t != null) optionsPanel = t.gameObject;
            }

            if (controlsPanel == null)
            {
                var t = root.Find("ControlsPanel");
                if (t != null) controlsPanel = t.gameObject;
            }

            if (audioPanel == null)
            {
                var t = root.Find("AudioPanel");
                if (t != null) audioPanel = t.gameObject;
            }

            if (creditsPanel == null)
            {
                var t = root.Find("CreditsPanel");
                if (t != null) creditsPanel = t.gameObject;
            }

            // Hook up options navigation buttons (Controls, Audio, Settings/Empty, Credits)
            if (optionsPanel != null)
            {
                // In hierarchy: BackButon (1) = Controls, BackButon (2) = Audio, BackButon (3) = Credits
                var b1 = optionsPanel.transform.Find("BackButon (1)")?.GetComponent<Button>();
                var b2 = optionsPanel.transform.Find("BackButon (2)")?.GetComponent<Button>();
                var b3 = optionsPanel.transform.Find("BackButon (3)")?.GetComponent<Button>();
                var b4 = optionsPanel.transform.Find("BackButon (4)")?.GetComponent<Button>();

                if (b1 != null) b1.onClick.AddListener(ShowControls);
                if (b2 != null) b2.onClick.AddListener(ShowAudio);
                if (b3 != null) b3.onClick.AddListener(ShowCredits);
                if (b4 != null) b4.onClick.AddListener(ShowOptionsHub);
            }

            // Hook up top-level Back button in AudioMenu (BackButon)
            if (closeOptionsButton == null)
            {
                var backBtn = root.Find("BackButon")?.GetComponent<Button>();
                if (backBtn != null)
                {
                    closeOptionsButton = backBtn;
                }
            }
        }

        [SerializeField] private Slider musicVolumeSlider;

        private void SetupSliders()
        {
            Transform root = panelRoot != null ? panelRoot.transform : transform;
            Transform ap = audioPanel != null ? audioPanel.transform : root.Find("AudioPanel");

            if (ap != null)
            {
                if (masterVolumeSlider == null)
                {
                    masterVolumeSlider = ap.Find("general/slider_general")?.GetComponent<Slider>();
                }
                if (sfxVolumeSlider == null)
                {
                    sfxVolumeSlider = ap.Find("effects/slider_general")?.GetComponent<Slider>();
                }
                if (musicVolumeSlider == null)
                {
                    musicVolumeSlider = ap.Find("music/slider_general")?.GetComponent<Slider>();
                }
            }

            if (masterVolumeSlider != null)
            {
                float savedMaster = PlayerPrefs.GetFloat("FMOD_MasterVolume", 1f);
                masterVolumeSlider.value = savedMaster;
                masterVolumeSlider.onValueChanged.RemoveAllListeners();
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                float savedSFX = PlayerPrefs.GetFloat("FMOD_SFXVolume", 1f);
                sfxVolumeSlider.value = savedSFX;
                sfxVolumeSlider.onValueChanged.RemoveAllListeners();
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            if (musicVolumeSlider != null)
            {
                float savedMusic = PlayerPrefs.GetFloat("FMOD_MusicVolume", 1f);
                musicVolumeSlider.value = savedMusic;
                musicVolumeSlider.onValueChanged.RemoveAllListeners();
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
        }

        public void OnMusicVolumeChanged(float value)
        {
            try
            {
                var bus = FMODUnity.RuntimeManager.GetBus("bus:/Music");
                bus.setVolume(Mathf.Clamp01(value));
            }
            catch {}
            PlayerPrefs.SetFloat("FMOD_MusicVolume", value);
            PlayerPrefs.Save();
        }

        private void SetupActionButtons()
        {
            if (actionButtons != null && actionButtons.Length > 0) return;

            Transform root = panelRoot != null ? panelRoot.transform : transform;
            Transform cp = controlsPanel != null ? controlsPanel.transform : root.Find("ControlsPanel");
            if (cp == null) return;

            // Map GameObject children to KeyBindingManager actions:
            // GameObject/BackButon (1) -> MoveUp (W, ButtonPromt_forward)
            // GameObject/BackButon (2) -> MoveDown (S, ButtonPromt_backwards)
            // GameObject (1)/BackButon (3) -> MoveLeft (A, ButtonPromt_left)
            // GameObject (1)/BackButon (4) -> MoveRight (D, ButtonPromt_right)
            // GameObject (2)/BackButon (5) -> Attack (Space, ButtonPromt_attack)
            // GameObject (3)/BackButon (8) -> ThreatOverlay (Tab, ButtonPromt_vision)

            var list = new List<ActionBindButton>();

            void TryAddBind(string path, string action)
            {
                var tr = cp.Find(path);
                if (tr != null)
                {
                    var btn = tr.GetComponent<Button>() ?? tr.gameObject.AddComponent<Button>();
                    var txt = tr.GetComponentInChildren<TMP_Text>();
                    list.Add(new ActionBindButton
                    {
                        actionName = action,
                        button = btn,
                        buttonLabel = txt
                    });
                }
            }

            TryAddBind("GameObject/BackButon (1)", KeyBindingManager.ActionMoveUp);
            TryAddBind("GameObject/BackButon (2)", KeyBindingManager.ActionMoveDown);
            TryAddBind("GameObject (1)/BackButon (3)", KeyBindingManager.ActionMoveLeft);
            TryAddBind("GameObject (1)/BackButon (4)", KeyBindingManager.ActionMoveRight);
            TryAddBind("GameObject (2)/BackButon (5)", KeyBindingManager.ActionAttack);
            TryAddBind("GameObject (3)/BackButon (8)", KeyBindingManager.ActionThreatOverlay);

            actionButtons = list.ToArray();
        }

        public void ShowOptionsHub()
        {
            if (optionsPanel != null) optionsPanel.SetActive(true);
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (audioPanel != null) audioPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
        }

        public void ShowControls()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(true);
            if (audioPanel != null) audioPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            RefreshActionLabels();
        }

        public void ShowAudio()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (audioPanel != null) audioPanel.SetActive(true);
            if (creditsPanel != null) creditsPanel.SetActive(false);
        }

        public void ShowCredits()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (audioPanel != null) audioPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(true);
        }

        public void OpenOptions()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            ShowOptionsHub();
            RefreshActionLabels();
            ClearMessages();
        }

        public void CloseOptions()
        {
            awaitingRebindAction = null;
            ClearMessages();
            if (panelRoot != null && panelRoot != gameObject) panelRoot.SetActive(false);
            else gameObject.SetActive(false);

            var mm = Object.FindAnyObjectByType<MenuManager>(FindObjectsInactive.Include);
            if (mm != null)
            {
                mm.ShowStartMenu();
            }
        }

        public void OnBackClicked()
        {
            // If in a subpanel (Controls, Audio, Credits), return to Options hub
            if ((controlsPanel != null && controlsPanel.activeSelf) ||
                (audioPanel != null && audioPanel.activeSelf) ||
                (creditsPanel != null && creditsPanel.activeSelf))
            {
                ShowOptionsHub();
            }
            else
            {
                CloseOptions();
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
