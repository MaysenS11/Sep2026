#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CharacterAssetCreator
{
    [MenuItem("Tools/Generate Character Assets")]
    public static void GenerateAssets()
    {
        string folder = "Assets/ScriptableObjects/Characters";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
        {
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        }
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Characters");
        }

        for (int i = 0; i < 4; i++)
        {
            string path = $"{folder}/Char_{i}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            if (existing == null)
            {
                var asset = ScriptableObject.CreateInstance<CharacterDefinition>();
                var so = new SerializedObject(asset);
                so.FindProperty("characterId").intValue = i;
                so.FindProperty("characterName").stringValue = $"Character {i}";
                so.FindProperty("lockedByDefault").boolValue = (i != 0);
                so.FindProperty("startHealth").intValue = 3;
                so.ApplyModifiedProperties();
                AssetDatabase.CreateAsset(asset, path);
            }
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Setup StartMenu Scene")]
    public static void SetupStartMenuScene()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        var menuManager = canvas.GetComponent<MenuManager>();
        if (menuManager == null) menuManager = canvas.AddComponent<MenuManager>();

        var quitBtnObj = GameObject.Find("Canvas/Backround/Buttons/QuitButton");
        if (quitBtnObj != null && quitBtnObj.TryGetComponent<UnityEngine.UI.Button>(out var quitBtn))
        {
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(quitBtn.onClick, menuManager.QuitGame);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(quitBtn.onClick, menuManager.QuitGame);
        }

        var background = GameObject.Find("Canvas/Backround");
        Transform bgTransform = background != null ? background.transform : canvas.transform;

        Transform wheelPanelTrans = bgTransform.Find("WheelPanel");
        GameObject wheelPanelObj;
        if (wheelPanelTrans == null)
        {
            wheelPanelObj = new GameObject("WheelPanel", typeof(RectTransform));
            wheelPanelObj.transform.SetParent(bgTransform, false);
            var rect = wheelPanelObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(-150f, 0f);
            rect.sizeDelta = new Vector2(600f, 600f);
        }
        else
        {
            wheelPanelObj = wheelPanelTrans.gameObject;
        }

        Transform previewTrans = wheelPanelObj.transform.Find("CharacterPreview");
        GameObject previewObj;
        if (previewTrans == null)
        {
            previewObj = new GameObject("CharacterPreview", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            previewObj.transform.SetParent(wheelPanelObj.transform, false);
            var pRect = previewObj.GetComponent<RectTransform>();
            pRect.anchoredPosition = new Vector2(0f, -50f);
            pRect.sizeDelta = new Vector2(300f, 400f);
        }
        else
        {
            previewObj = previewTrans.gameObject;
        }
        var previewImage = previewObj.GetComponent<UnityEngine.UI.Image>();

        CharacterSlotUI[] slots = new CharacterSlotUI[4];
        for (int i = 0; i < 4; i++)
        {
            string slotName = $"Slot_{i}";
            Transform slotTrans = wheelPanelObj.transform.Find(slotName);
            GameObject slotObj;
            if (slotTrans == null)
            {
                slotObj = new GameObject(slotName, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(CharacterSlotUI));
                slotObj.transform.SetParent(wheelPanelObj.transform, false);
                var sRect = slotObj.GetComponent<RectTransform>();
                sRect.sizeDelta = new Vector2(100f, 100f);
            }
            else
            {
                slotObj = slotTrans.gameObject;
            }

            var slotUI = slotObj.GetComponent<CharacterSlotUI>();
            if (slotUI == null) slotUI = slotObj.AddComponent<CharacterSlotUI>();

            var charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>($"Assets/ScriptableObjects/Characters/Char_{i}.asset");
            slotUI.SetDefinition(charDef);

            var soSlot = new SerializedObject(slotUI);
            soSlot.FindProperty("characterDefinition").objectReferenceValue = charDef;
            soSlot.FindProperty("maskImage").objectReferenceValue = slotObj.GetComponent<UnityEngine.UI.Image>();
            soSlot.FindProperty("selectButton").objectReferenceValue = slotObj.GetComponent<UnityEngine.UI.Button>();
            soSlot.ApplyModifiedProperties();

            slots[i] = slotUI;
        }

        var wheelController = wheelPanelObj.GetComponent<CharacterWheelController>();
        if (wheelController == null) wheelController = wheelPanelObj.AddComponent<CharacterWheelController>();

        var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");

        var soWheel = new SerializedObject(wheelController);
        var slotsProp = soWheel.FindProperty("slots");
        slotsProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        }
        soWheel.FindProperty("fullBodyPreview").objectReferenceValue = previewImage;
        soWheel.FindProperty("radius").floatValue = 180f;
        soWheel.FindProperty("inputActionAsset").objectReferenceValue = inputAsset;
        soWheel.ApplyModifiedProperties();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.scene);
    }
}
#endif
