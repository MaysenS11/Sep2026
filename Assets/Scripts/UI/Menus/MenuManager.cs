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
    [SerializeField] private UnityEngine.UI.Button optionsButton;
    [SerializeField] private UnityEngine.UI.Button quitButton;

    private void Awake()
    {
        AutoWireElements();
        ShowStartMenu();
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

        // Wire buttons under StartMenu
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

            // Arrow buttons: arrow goes right, arrow (1) goes left
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

        // Wire GameOver buttons to return to StartMenu
        if (gameOverPanel != null)
        {
            var gameOverButtons = gameOverPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var btn in gameOverButtons)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(ShowStartMenu);
            }
        }
    }

    public void ShowStartMenu()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
        if (audioMenuPanel != null) audioMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (audioMenuPanel != null) audioMenuPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
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
