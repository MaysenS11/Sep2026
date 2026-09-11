#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class StartMenuSettingsBuilder
{
    [MenuItem("Tools/Build Start Menu Settings UI")]
    public static void BuildUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("Canvas not found in active scene!");
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Jacquard12-Regular SDF.asset");
        if (font == null)
        {
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        Transform existingBtn = canvas.transform.Find("SettingsButton");
        if (existingBtn != null) Object.DestroyImmediate(existingBtn.gameObject);

        Transform existingPanel = canvas.transform.Find("SettingsPanel");
        if (existingPanel != null) Object.DestroyImmediate(existingPanel.gameObject);

        // 1. Settings Button (Top Left)
        GameObject btnObj = new GameObject("SettingsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvas.transform, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0f, 1f);
        btnRect.anchorMax = new Vector2(0f, 1f);
        btnRect.pivot = new Vector2(0f, 1f);
        btnRect.anchoredPosition = new Vector2(25f, -25f);
        btnRect.sizeDelta = new Vector2(160f, 48f);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.18f, 0.16f, 0.22f, 0.95f);
        Button btnComp = btnObj.GetComponent<Button>();

        GameObject btnTextObj = CreateText("Text", btnObj.transform, "settings", 32, font, Color.white);
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;

        // Add StartMenuSettingsUI to Canvas
        StartMenuSettingsUI uiComp = canvas.GetComponent<StartMenuSettingsUI>();
        if (uiComp == null) uiComp = canvas.AddComponent<StartMenuSettingsUI>();

        // 2. Settings Panel Modal (Centered)
        GameObject panelObj = new GameObject("SettingsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panelObj.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(620f, 580f);

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.10f, 0.08f, 0.14f, 0.96f);

        CanvasGroup cg = panelObj.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // Title
        GameObject titleObj = CreateText("Title", panelObj.transform, "spawn settings", 32, font, Color.white);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);
        titleRect.sizeDelta = new Vector2(400f, 40f);

        // Sliders Container
        float startY = -80f;
        float spacingY = 52f;

        var (eSlider, eVal) = CreateSliderRow("EnemyDensityRow", panelObj.transform, "enemy density", startY, font);
        var (bSlider, bVal) = CreateSliderRow("BarrelDensityRow", panelObj.transform, "barrel density", startY - spacingY, font);

        // Section header: Enemy Population Weights
        GameObject popTitleObj = CreateText("PopTitle", panelObj.transform, "- population weights -", 32, font, new Color(0.85f, 0.75f, 0.5f, 1f));
        RectTransform popTitleRect = popTitleObj.GetComponent<RectTransform>();
        popTitleRect.anchorMin = new Vector2(0.5f, 1f);
        popTitleRect.anchorMax = new Vector2(0.5f, 1f);
        popTitleRect.pivot = new Vector2(0.5f, 1f);
        popTitleRect.anchoredPosition = new Vector2(0f, startY - spacingY * 2f + 4f);
        popTitleRect.sizeDelta = new Vector2(400f, 36f);

        float popStartY = startY - spacingY * 2.6f;
        var (pSlider, pVal) = CreateSliderRow("PawnWeightRow", panelObj.transform, "pawn weight", popStartY, font);
        var (kSlider, kVal) = CreateSliderRow("KnightWeightRow", panelObj.transform, "knight weight", popStartY - spacingY, font);
        var (rSlider, rVal) = CreateSliderRow("RookWeightRow", panelObj.transform, "rook weight", popStartY - spacingY * 2f, font);
        var (biSlider, biVal) = CreateSliderRow("BishopWeightRow", panelObj.transform, "bishop weight", popStartY - spacingY * 3f, font);

        // Reset Button
        GameObject resetBtnObj = CreateButton("ResetDefaultsButton", panelObj.transform, "reset defaults", new Vector2(-125f, 35f), new Vector2(210f, 44f), font, new Color(0.35f, 0.22f, 0.22f, 1f));
        // Close Button
        GameObject closeBtnObj = CreateButton("CloseSettingsButton", panelObj.transform, "save & close", new Vector2(125f, 35f), new Vector2(210f, 44f), font, new Color(0.22f, 0.35f, 0.25f, 1f));

        Button closeBtnComp = closeBtnObj.GetComponent<Button>();
        Button resetBtnComp = resetBtnObj.GetComponent<Button>();

        // Wire persistent UnityEvents
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnComp.onClick, uiComp.OpenSettings);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(closeBtnComp.onClick, uiComp.CloseSettings);

        // Wire fields via SerializedObject
        SerializedObject so = new SerializedObject(uiComp);
        so.FindProperty("settingsCanvasGroup").objectReferenceValue = cg;
        so.FindProperty("openSettingsButton").objectReferenceValue = btnComp;
        so.FindProperty("closeSettingsButton").objectReferenceValue = closeBtnComp;
        so.FindProperty("resetDefaultsButton").objectReferenceValue = resetBtnComp;

        so.FindProperty("enemyDensitySlider").objectReferenceValue = eSlider;
        so.FindProperty("enemyDensityValueText").objectReferenceValue = eVal;

        so.FindProperty("barrelDensitySlider").objectReferenceValue = bSlider;
        so.FindProperty("barrelDensityValueText").objectReferenceValue = bVal;

        so.FindProperty("pawnWeightSlider").objectReferenceValue = pSlider;
        so.FindProperty("pawnWeightValueText").objectReferenceValue = pVal;

        so.FindProperty("knightWeightSlider").objectReferenceValue = kSlider;
        so.FindProperty("knightWeightValueText").objectReferenceValue = kVal;

        so.FindProperty("rookWeightSlider").objectReferenceValue = rSlider;
        so.FindProperty("rookWeightValueText").objectReferenceValue = rVal;

        so.FindProperty("bishopWeightSlider").objectReferenceValue = biSlider;
        so.FindProperty("bishopWeightValueText").objectReferenceValue = biVal;

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(panelObj);
        EditorUtility.SetDirty(btnObj);
        EditorUtility.SetDirty(canvas);

        Debug.Log("Successfully built StartMenu Settings UI!");
    }

    private static (Slider slider, TMP_Text valText) CreateSliderRow(string name, Transform parent, string label, float yPos, TMP_FontAsset font)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, yPos);
        rowRect.sizeDelta = new Vector2(-60f, 36f);

        // Label
        GameObject lblObj = CreateText("Label", row.transform, label, 32, font, Color.white);
        RectTransform lblRect = lblObj.GetComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0f, 0.5f);
        lblRect.anchorMax = new Vector2(0f, 0.5f);
        lblRect.pivot = new Vector2(0f, 0.5f);
        lblRect.anchoredPosition = new Vector2(10f, 0f);
        lblRect.sizeDelta = new Vector2(190f, 36f);
        lblObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

        // Slider Object
        GameObject sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(row.transform, false);
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(40f, 0f);
        sliderRect.sizeDelta = new Vector2(-310f, 20f);

        Slider slider = sliderObj.GetComponent<Slider>();

        // Background
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.sizeDelta = new Vector2(-10f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImg = fill.GetComponent<Image>();
        fillImg.color = new Color(0.85f, 0.55f, 0.2f, 1f);

        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(16f, 24f);
        Image handleImg = handle.GetComponent<Image>();
        handleImg.color = Color.white;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;

        // Value text
        GameObject valObj = CreateText("ValueText", row.transform, "50%", 32, font, Color.white);
        RectTransform valRect = valObj.GetComponent<RectTransform>();
        valRect.anchorMin = new Vector2(1f, 0.5f);
        valRect.anchorMax = new Vector2(1f, 0.5f);
        valRect.pivot = new Vector2(1f, 0.5f);
        valRect.anchoredPosition = new Vector2(-5f, 0f);
        valRect.sizeDelta = new Vector2(70f, 36f);
        TextMeshProUGUI valText = valObj.GetComponent<TextMeshProUGUI>();
        valText.alignment = TextAlignmentOptions.Right;

        return (slider, valText);
    }

    private static GameObject CreateButton(string name, Transform parent, string text, Vector2 pos, Vector2 size, TMP_FontAsset font, Color bgColor)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = pos;
        btnRect.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        GameObject txtObj = CreateText("Text", btnObj.transform, text, 32, font, Color.white);
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        txtObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        return btnObj;
    }

    private static GameObject CreateText(string name, Transform parent, string content, float size, TMP_FontAsset font, Color color)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = content.ToLowerInvariant();
        text.fontSize = size;
        text.characterSpacing = 2f;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        return textObj;
    }
}
#endif
