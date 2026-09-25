using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace UI
{
    /// <summary>
    /// Displays Game Over and Win screens.
    /// Tracks run time, records best completion time per mask in PlayerPrefs (best_time_{maskId}),
    /// and awards Mask Keys on first King defeat per unique mask for the Character Wheel.
    /// </summary>
    public class GameOverWinUI : MonoBehaviour
    {
        public static GameOverWinUI Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Win UI Elements")]
        [SerializeField] private TMP_Text winRunTimeText;
        [SerializeField] private TMP_Text winBestTimeText;
        [SerializeField] private GameObject newBestBadge;
        [SerializeField] private GameObject maskKeyAwardedBanner;
        [SerializeField] private Button winPlayAgainButton;
        [SerializeField] private Button winMainMenuButton;

        [Header("Game Over UI Elements")]
        [SerializeField] private TMP_Text gameOverRunTimeText;
        [SerializeField] private TMP_Text gameOverBestTimeText;
        [SerializeField] private Button gameOverRetryButton;
        [SerializeField] private Button gameOverMainMenuButton;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            EnsurePanels();
            BindButtons();
            HideAll();
        }

        private void OnEnable()
        {
            EventBus<GameWonEvent>.Subscribe(OnGameWon);
            EventBus<GameOverEvent>.Subscribe(OnGameOver);
        }

        private void OnDisable()
        {
            EventBus<GameWonEvent>.Unsubscribe(OnGameWon);
            EventBus<GameOverEvent>.Unsubscribe(OnGameOver);
        }

        private void EnsurePanels()
        {
            if (winPanel == null)
            {
                winPanel = transform.Find("WinPanel")?.gameObject
                        ?? transform.Find("WinScreen")?.gameObject
                        ?? transform.Find("VictoryPanel")?.gameObject;
            }

            if (gameOverPanel == null)
            {
                gameOverPanel = transform.Find("GameOverPanel")?.gameObject
                             ?? transform.Find("GameOverScreen")?.gameObject
                             ?? transform.Find("DeathPanel")?.gameObject;
            }
        }

        private void BindButtons()
        {
            if (winPlayAgainButton != null) winPlayAgainButton.onClick.AddListener(RestartGame);
            if (winMainMenuButton != null) winMainMenuButton.onClick.AddListener(LoadMainMenu);
            if (gameOverRetryButton != null) gameOverRetryButton.onClick.AddListener(RestartGame);
            if (gameOverMainMenuButton != null) gameOverMainMenuButton.onClick.AddListener(LoadMainMenu);
        }

        private void HideAll()
        {
            if (winPanel != null) winPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        public void OnGameWon(GameWonEvent evt)
        {
            ShowWinScreen(evt.RunTime, evt.MaskId, evt.IsFirstDefeat);
        }

        public void OnGameOver(GameOverEvent evt)
        {
            ShowGameOverScreen(evt.RunTime, evt.MaskId);
        }

        public void ShowWinScreen(float runTime, string maskId, bool isFirstDefeat = false)
        {
            if (string.IsNullOrEmpty(maskId)) maskId = "Default";

            string bestTimeKey = $"best_time_{maskId}";
            float bestTime = PlayerPrefs.GetFloat(bestTimeKey, float.MaxValue);
            bool isNewBest = false;

            if (runTime < bestTime)
            {
                bestTime = runTime;
                PlayerPrefs.SetFloat(bestTimeKey, bestTime);
                PlayerPrefs.Save();
                isNewBest = true;
            }

            // Check first King defeat for mask key
            string firstDefeatKey = $"first_king_defeat_{maskId}";
            bool awardedKey = isFirstDefeat;
            if (PlayerPrefs.GetInt(firstDefeatKey, 0) == 0)
            {
                PlayerPrefs.SetInt(firstDefeatKey, 1);
                int currentKeys = PlayerPrefs.GetInt("MaskKeys", 0) + 1;
                PlayerPrefs.SetInt("MaskKeys", currentKeys);
                PlayerPrefs.Save();
                awardedKey = true;
            }

            if (winRunTimeText != null)
            {
                winRunTimeText.text = $"Time: {FormatTime(runTime)}";
            }

            if (winBestTimeText != null)
            {
                winBestTimeText.text = $"Best ({maskId}): {FormatTime(bestTime)}";
            }

            if (newBestBadge != null)
            {
                newBestBadge.SetActive(isNewBest);
            }

            if (maskKeyAwardedBanner != null)
            {
                maskKeyAwardedBanner.SetActive(awardedKey);
            }

            if (winPanel != null)
            {
                winPanel.SetActive(true);
            }
        }

        public void ShowGameOverScreen(float runTime, string maskId)
        {
            if (string.IsNullOrEmpty(maskId)) maskId = "Default";

            string bestTimeKey = $"best_time_{maskId}";
            float bestTime = PlayerPrefs.GetFloat(bestTimeKey, float.MaxValue);

            if (gameOverRunTimeText != null)
            {
                gameOverRunTimeText.text = $"Time: {FormatTime(runTime)}";
            }

            if (gameOverBestTimeText != null)
            {
                gameOverBestTimeText.text = bestTime < float.MaxValue 
                    ? $"Best ({maskId}): {FormatTime(bestTime)}" 
                    : $"Best ({maskId}): --:--";
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        public static string FormatTime(float totalSeconds)
        {
            int total = Mathf.Max(0, (int)totalSeconds);
            int hours = total / 3600;
            int minutes = (total % 3600) / 60;
            int seconds = total % 60;

            if (hours > 0)
            {
                return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
            }
            return $"{minutes:D2}:{seconds:D2}";
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadMainMenu()
        {
            SceneManager.LoadScene("StartMenu");
        }
    }
}
