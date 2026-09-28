using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Level";

    private void OnEnable()
    {
        EventBus<CharacterSelectedEvent>.Subscribe(OnCharacterSelected);
    }

    private void OnDisable()
    {
        EventBus<CharacterSelectedEvent>.Unsubscribe(OnCharacterSelected);
    }

    private void OnCharacterSelected(CharacterSelectedEvent evt)
    {
        if (evt.Character != null)
        {
            CharacterSelectData.SelectedCharacter = evt.Character;
            SceneManager.LoadScene(gameSceneName);
        }
    }

    [SerializeField] private UI.OptionsMenuUI optionsMenu;
    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject audioMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject dungeonClearedPanel;
    [SerializeField] private UnityEngine.UI.Button optionsButton;
    [SerializeField] private UnityEngine.UI.Button quitButton;

    public static bool ShowGameOverOnStart { get; set; } = false;
    public static bool ShowWinOnStart { get; set; } = false;

    private void Awake()
    {
        AutoWireElements();
        if (ShowWinOnStart)
        {
            ShowWinOnStart = false;
            ShowDungeonCleared();
        }
        else if (ShowGameOverOnStart)
        {
            ShowGameOverOnStart = false;
            ShowGameOver();
        }
        else
        {
            ShowStartMenu();
        }
    }

    private void AutoWireElements()
    {
        Transform canvasT = transform;

        if (startMenuPanel == null)
        {
            var t = canvasT.Find("StartMenu");
            if (t != null) startMenuPanel = t.gameObject;
        }

        if (audioMenuPanel == null)
        {
            var t = canvasT.Find("AudioMenu");
            if (t != null) audioMenuPanel = t.gameObject;
        }

        if (gameOverPanel == null)
        {
            var t = canvasT.Find("GameOver");
            if (t != null) gameOverPanel = t.gameObject;
        }

        if (dungeonClearedPanel == null)
        {
            var t = canvasT.Find("DungeonCleared") ?? canvasT.Find("WinPanel") ?? canvasT.Find("VictoryPanel");
            if (t != null) dungeonClearedPanel = t.gameObject;
        }

        if (optionsMenu == null && audioMenuPanel != null)
        {
            optionsMenu = audioMenuPanel.GetComponent<UI.OptionsMenuUI>();
            if (optionsMenu == null)
            {
                optionsMenu = audioMenuPanel.AddComponent<UI.OptionsMenuUI>();
            }
        }

        if (optionsMenu == null)
        {
            optionsMenu = FindAnyObjectByType<UI.OptionsMenuUI>(FindObjectsInactive.Include);
        }

        if (startMenuPanel != null)
        {
            if (optionsButton == null)
            {
                optionsButton = startMenuPanel.transform.Find("Buttons/OptionsButton")?.GetComponent<UnityEngine.UI.Button>();
            }
            if (quitButton == null)
            {
                quitButton = startMenuPanel.transform.Find("Buttons/QuitButton")?.GetComponent<UnityEngine.UI.Button>();
            }

            var wheel = startMenuPanel.GetComponentInChildren<CharacterWheelController>(true);
            var arrowRight = startMenuPanel.transform.Find("WheelPanel/Arrows/arrow");
            var arrowLeft = startMenuPanel.transform.Find("WheelPanel/Arrows/arrow (1)");

            if (arrowRight != null && wheel != null)
            {
                var btn = arrowRight.GetComponent<UnityEngine.UI.Button>() ?? arrowRight.gameObject.AddComponent<UnityEngine.UI.Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => wheel.RotateNext());
            }

            if (arrowLeft != null && wheel != null)
            {
                var btn = arrowLeft.GetComponent<UnityEngine.UI.Button>() ?? arrowLeft.gameObject.AddComponent<UnityEngine.UI.Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => wheel.RotatePrevious());
            }
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveAllListeners();
            optionsButton.onClick.AddListener(OpenOptions);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }

        if (gameOverPanel != null)
        {
            WireEndPanelButtons(gameOverPanel);
        }

        if (dungeonClearedPanel != null)
        {
            WireEndPanelButtons(dungeonClearedPanel);
        }
    }

    private void WireEndPanelButtons(GameObject panel)
    {
        if (panel == null) return;
        var buttons = panel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        if (buttons == null || buttons.Length == 0) return;

        UnityEngine.UI.Button menuButton = null;
        UnityEngine.UI.Button quitBtn = null;

        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn == null) continue;
            string bName = btn.name.ToLower();
            string txt = "";
            var tmp = btn.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmp != null) txt = tmp.text.ToLower();

            if (bName.Contains("quit") || bName.Contains("exit") || txt.Contains("quit") || txt.Contains("exit"))
            {
                quitBtn = btn;
            }
            else if (bName.Contains("menu") || bName.Contains("start") || txt.Contains("menu") || txt.Contains("start") || txt.Contains("main"))
            {
                menuButton = btn;
            }
        }

        if (menuButton == null && buttons.Length > 0)
        {
            menuButton = buttons[0];
        }
        if (quitBtn == null && buttons.Length > 1)
        {
            quitBtn = buttons[1];
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(ShowStartMenu);
        }

        if (quitBtn != null)
        {
            quitBtn.onClick.RemoveAllListeners();
            quitBtn.onClick.AddListener(QuitGame);
        }
    }

    public void ShowStartMenu()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (audioMenuPanel != null) audioMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (dungeonClearedPanel != null) dungeonClearedPanel.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (audioMenuPanel != null) audioMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (dungeonClearedPanel != null) dungeonClearedPanel.SetActive(false);
    }

    public void ShowDungeonCleared()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (audioMenuPanel != null) audioMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (dungeonClearedPanel != null) dungeonClearedPanel.SetActive(true);
    }

    public void PlayGame()
    {
        if (CharacterSelectData.SelectedCharacter != null)
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            var wheel = FindAnyObjectByType<CharacterWheelController>();
            if (wheel != null)
            {
                wheel.SelectCurrent();
            }
            else
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }
    }

    public void OpenOptions()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (audioMenuPanel != null)
        {
            audioMenuPanel.SetActive(true);
            if (optionsMenu != null)
            {
                optionsMenu.ShowOptionsHub();
            }
        }
        else if (optionsMenu != null)
        {
            optionsMenu.OpenOptions();
        }
    }

    public void CloseOptions()
    {
        if (audioMenuPanel != null) audioMenuPanel.SetActive(false);
        if (optionsMenu != null)
        {
            optionsMenu.CloseOptions();
        }
        ShowStartMenu();
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
