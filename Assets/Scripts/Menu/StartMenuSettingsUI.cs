using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartMenuSettingsUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private CanvasGroup settingsCanvasGroup;
    [SerializeField] private Button openSettingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button resetDefaultsButton;

    [Header("Spawn Densities")]
    [SerializeField] private Slider enemyDensitySlider;
    [SerializeField] private TMP_Text enemyDensityValueText;
    [SerializeField] private Slider barrelDensitySlider;
    [SerializeField] private TMP_Text barrelDensityValueText;

    [Header("Enemy Population Weights")]
    [SerializeField] private Slider pawnWeightSlider;
    [SerializeField] private TMP_Text pawnWeightValueText;
    [SerializeField] private Slider knightWeightSlider;
    [SerializeField] private TMP_Text knightWeightValueText;
    [SerializeField] private Slider rookWeightSlider;
    [SerializeField] private TMP_Text rookWeightValueText;
    [SerializeField] private Slider bishopWeightSlider;
    [SerializeField] private TMP_Text bishopWeightValueText;

    private bool isUpdatingUI = false;

    private void Awake()
    {
        if (settingsCanvasGroup == null)
        {
            settingsCanvasGroup = GetComponent<CanvasGroup>();
            if (settingsCanvasGroup == null)
            {
                settingsCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (openSettingsButton != null)
        {
            openSettingsButton.onClick.RemoveAllListeners();
            openSettingsButton.onClick.AddListener(OpenSettings);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveAllListeners();
            closeSettingsButton.onClick.AddListener(CloseSettings);
        }

        if (resetDefaultsButton != null)
        {
            resetDefaultsButton.onClick.RemoveAllListeners();
            resetDefaultsButton.onClick.AddListener(OnResetDefaultsClicked);
        }

        SetupSlider(enemyDensitySlider, OnEnemyDensityChanged);
        SetupSlider(barrelDensitySlider, OnBarrelDensityChanged);
        SetupSlider(pawnWeightSlider, OnPawnWeightChanged);
        SetupSlider(knightWeightSlider, OnKnightWeightChanged);
        SetupSlider(rookWeightSlider, OnRookWeightChanged);
        SetupSlider(bishopWeightSlider, OnBishopWeightChanged);

        SetPanelVisible(false);
    }

    private void Start()
    {
        RefreshUIFromSettings();
        SetPanelVisible(false);
    }

    private void SetupSlider(Slider slider, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(callback);
        }
    }

    public void OpenSettings()
    {
        RefreshUIFromSettings();
        SetPanelVisible(true);
    }

    public void CloseSettings()
    {
        GameSpawnSettings.Save();
        SetPanelVisible(false);
    }

    private void SetPanelVisible(bool visible)
    {
        if (settingsCanvasGroup != null)
        {
            settingsCanvasGroup.alpha = visible ? 1f : 0f;
            settingsCanvasGroup.interactable = visible;
            settingsCanvasGroup.blocksRaycasts = visible;
        }
    }

    private void OnResetDefaultsClicked()
    {
        GameSpawnSettings.ResetToDefaults();
        RefreshUIFromSettings();
    }

    private void RefreshUIFromSettings()
    {
        isUpdatingUI = true;

        if (enemyDensitySlider != null)
        {
            enemyDensitySlider.minValue = 0.01f;
            enemyDensitySlider.maxValue = 0.25f;
            enemyDensitySlider.value = GameSpawnSettings.EnemyDensity;
        }
        UpdateDensityText(enemyDensityValueText, GameSpawnSettings.EnemyDensity);

        if (barrelDensitySlider != null)
        {
            barrelDensitySlider.minValue = 0.01f;
            barrelDensitySlider.maxValue = 0.25f;
            barrelDensitySlider.value = GameSpawnSettings.BarrelDensity;
        }
        UpdateDensityText(barrelDensityValueText, GameSpawnSettings.BarrelDensity);

        UpdateWeightSlider(pawnWeightSlider, pawnWeightValueText, GameSpawnSettings.PawnWeight);
        UpdateWeightSlider(knightWeightSlider, knightWeightValueText, GameSpawnSettings.KnightWeight);
        UpdateWeightSlider(rookWeightSlider, rookWeightValueText, GameSpawnSettings.RookWeight);
        UpdateWeightSlider(bishopWeightSlider, bishopWeightValueText, GameSpawnSettings.BishopWeight);

        isUpdatingUI = false;
    }

    private void UpdateWeightSlider(Slider slider, TMP_Text label, float val)
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = val;
        }
        if (label != null)
        {
            label.text = $"{Mathf.RoundToInt(val * 100f)}%";
        }
    }

    private void UpdateDensityText(TMP_Text label, float density)
    {
        if (label != null)
        {
            label.text = $"{Mathf.RoundToInt(density * 100f)}%";
        }
    }

    private void OnEnemyDensityChanged(float val)
    {
        if (isUpdatingUI) return;
        GameSpawnSettings.EnemyDensity = val;
        UpdateDensityText(enemyDensityValueText, val);
    }

    private void OnBarrelDensityChanged(float val)
    {
        if (isUpdatingUI) return;
        GameSpawnSettings.BarrelDensity = val;
        UpdateDensityText(barrelDensityValueText, val);
    }

    private void OnPawnWeightChanged(float val)
    {
        if (isUpdatingUI) return;
        GameSpawnSettings.PawnWeight = val;
        if (pawnWeightValueText != null) pawnWeightValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
    }

    private void OnKnightWeightChanged(float val)
    {
        if (isUpdatingUI) return;
        GameSpawnSettings.KnightWeight = val;
        if (knightWeightValueText != null) knightWeightValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
    }

    private void OnRookWeightChanged(float val)
    {
        if (isUpdatingUI) return;
        GameSpawnSettings.RookWeight = val;
        if (rookWeightValueText != null) rookWeightValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
    }

    private void OnBishopWeightChanged(float val)
    {
        if (isUpdatingUI) return;
        GameSpawnSettings.BishopWeight = val;
        if (bishopWeightValueText != null) bishopWeightValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
    }
}
