#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CharacterAssetCreator
{
    private static T FindAsset<T>(string filter, string defaultPath) where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets(filter);
        if (guids != null && guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
        }
        return AssetDatabase.LoadAssetAtPath<T>(defaultPath);
    }

    [MenuItem("Tools/Inspect Slots")]
    public static void InspectSlots()
    {
        var slots = Object.FindObjectsByType<CharacterSlotUI>(FindObjectsSortMode.None);
        foreach (var s in slots)
        {
            var btn = s.GetComponent<UnityEngine.UI.Button>();
            var overlay = s.transform.Find("OverlayMask");
            var slotImg = s.GetComponent<UnityEngine.UI.Image>();
            Debug.Log($"SLOT {s.name}: definition={(s.CharacterDefinition != null ? s.CharacterDefinition.name : "null")}, " +
                      $"defWhiteSprite={(s.CharacterDefinition != null && s.CharacterDefinition.MaskWhiteSprite != null ? s.CharacterDefinition.MaskWhiteSprite.name : "null")}, " +
                      $"btnTargetGraphic={(btn != null && btn.targetGraphic != null ? btn.targetGraphic.name : "null")}, " +
                      $"slotImgRaycastTarget={(slotImg != null ? slotImg.raycastTarget.ToString() : "null")}, " +
                      $"overlayExists={(overlay != null)}, " +
                      $"overlaySiblingIndex={(overlay != null ? overlay.GetSiblingIndex().ToString() : "none")}, " +
                      $"overlaySprite={(overlay != null && overlay.GetComponent<UnityEngine.UI.Image>().sprite != null ? overlay.GetComponent<UnityEngine.UI.Image>().sprite.name : "null")}, " +
                      $"overlayColor={(overlay != null ? overlay.GetComponent<UnityEngine.UI.Image>().color.ToString() : "null")}, " +
                      $"overlayRaycast={(overlay != null ? overlay.GetComponent<UnityEngine.UI.Image>().raycastTarget.ToString() : "null")}");
        }
    }
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
                so.FindProperty("lockedByDefault").boolValue = (i != 0);
                so.ApplyModifiedProperties();
                AssetDatabase.CreateAsset(asset, path);
            }
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Fix Mask Overlay And Raycast")]
    public static void FixMaskOverlayAndRaycast()
    {
        // 1. Fix CharacterPreview raycastTarget
        var previewGO = GameObject.Find("Canvas/Backround/WheelPanel/CharacterPreview");
        if (previewGO != null)
        {
            var img = previewGO.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.raycastTarget = false;
                EditorUtility.SetDirty(previewGO);
                EditorUtility.SetDirty(img);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(previewGO.scene);
                Debug.Log("Set CharacterPreview raycastTarget to false.");
            }
        }

        // 2. Configure CatSlot.prefab
        string prefabPath = "Assets/Prefab/UI/CatSlot.prefab";
        string[] slotGuids = AssetDatabase.FindAssets("CatSlot t:Prefab");
        if (slotGuids != null && slotGuids.Length > 0)
        {
            prefabPath = AssetDatabase.GUIDToAssetPath(slotGuids[0]);
        }
        var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot != null)
        {
            var overlay = prefabRoot.transform.Find("OverlayMask");
            UnityEngine.UI.Image overlayImg = null;
            if (overlay == null)
            {
                var go = new GameObject("OverlayMask", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                go.transform.SetParent(prefabRoot.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                overlayImg = go.GetComponent<UnityEngine.UI.Image>();
                overlayImg.raycastTarget = false;
                overlayImg.color = new Color(1f, 1f, 1f, 0f);
            }
            else
            {
                overlayImg = overlay.GetComponent<UnityEngine.UI.Image>();
                overlayImg.raycastTarget = false;
                overlay.SetAsLastSibling();
            }

            var slotUI = prefabRoot.GetComponent<CharacterSlotUI>();
            var btn = prefabRoot.GetComponent<UnityEngine.UI.Button>();
            if (slotUI != null)
            {
                var so = new SerializedObject(slotUI);
                var overlayProp = so.FindProperty("overlayMaskImage");
                if (overlayProp != null)
                {
                    overlayProp.objectReferenceValue = overlayImg;
                    so.ApplyModifiedProperties();
                }
            }

            if (btn != null)
            {
                var soBtn = new SerializedObject(btn);
                var targetProp = soBtn.FindProperty("m_TargetGraphic");
                if (targetProp != null)
                {
                    targetProp.objectReferenceValue = overlayImg;
                    soBtn.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(btn);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            Debug.Log("CatSlot.prefab configured with OverlayMask, overlayMaskImage, and Button.targetGraphic.");
        }

        // 3. Re-run SetupMaskWhiteOverlays to refresh scene instances
        SetupMaskWhiteOverlays();

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("Saved open scenes.");
    }

    [MenuItem("Tools/Setup Mask White Overlays")]
    public static void SetupMaskWhiteOverlays()
    {
        // 1. Map character assets to white mask sprite paths
        var mappings = new (string filter, string defaultPath, string whiteFilter, string defaultWhitePath)[]
        {
            ("Cat_Char t:CharacterDefinition", "Assets/ScriptableObjects/PlayerCharacters/Cat_Char.asset", "Mask_CatWhite t:Sprite", "Assets/Sprites/Player/Masks/Mask_CatWhite.png"),
            ("Rat_Char t:CharacterDefinition", "Assets/ScriptableObjects/PlayerCharacters/Rat_Char.asset", "Mask_RatWhite t:Sprite", "Assets/Sprites/Player/Masks/Mask_RatWhite.png"),
            ("Raven_Char t:CharacterDefinition", "Assets/ScriptableObjects/PlayerCharacters/Raven_Char.asset", "Mask_RabeWhite t:Sprite", "Assets/Sprites/Player/Masks/Mask_RabeWhite.png"),
            ("Moose_Char t:CharacterDefinition", "Assets/ScriptableObjects/PlayerCharacters/Moose_Char.asset", "Mask_HirschWhite t:Sprite", "Assets/Sprites/Player/Masks/Mask_HirschWhite.png"),
        };

        foreach (var (filter, defaultPath, whiteFilter, defaultWhitePath) in mappings)
        {
            var charDef = FindAsset<CharacterDefinition>(filter, defaultPath);
            if (charDef != null)
            {
                var sprite = FindAsset<Sprite>(whiteFilter, defaultWhitePath);
                if (sprite == null)
                {
                    string spritePath = defaultWhitePath;
                    string[] sGuids = AssetDatabase.FindAssets(whiteFilter);
                    if (sGuids != null && sGuids.Length > 0)
                    {
                        spritePath = AssetDatabase.GUIDToAssetPath(sGuids[0]);
                    }
                    var allAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
                    foreach (var a in allAssets)
                    {
                        if (a is Sprite s)
                        {
                            sprite = s;
                            break;
                        }
                    }
                }

                var so = new SerializedObject(charDef);
                so.FindProperty("maskWhiteSprite").objectReferenceValue = sprite;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(charDef);
            }
        }
        AssetDatabase.SaveAssets();

        // 2. Setup slots in current scene
        var slots = Object.FindObjectsByType<CharacterSlotUI>(FindObjectsSortMode.None);
        foreach (var slot in slots)
        {
            Transform overlayTrans = slot.transform.Find("OverlayMask");
            GameObject overlayObj;
            if (overlayTrans == null)
            {
                overlayObj = new GameObject("OverlayMask", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                overlayObj.transform.SetParent(slot.transform, false);
                var rt = overlayObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                overlayObj = overlayTrans.gameObject;
            }

            // Ensure OverlayMask renders on TOP of the base mask Image
            overlayObj.transform.SetAsLastSibling();

            var overlayImg = overlayObj.GetComponent<UnityEngine.UI.Image>();
            overlayImg.raycastTarget = false;
            overlayImg.color = new Color(1f, 1f, 1f, 0f);

            var soSlot = new SerializedObject(slot);
            soSlot.FindProperty("overlayMaskImage").objectReferenceValue = overlayImg;
            soSlot.ApplyModifiedProperties();

            // Explicitly set and serialize Button targetGraphic to the overlay image
            var btn = slot.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                var soBtn = new SerializedObject(btn);
                soBtn.FindProperty("m_TargetGraphic").objectReferenceValue = overlayImg;
                soBtn.ApplyModifiedProperties();
                EditorUtility.SetDirty(btn);
            }

            EditorUtility.SetDirty(slot);
            EditorUtility.SetDirty(overlayObj);

            // Re-apply definition so sprites and button states refresh immediately
            if (slot.CharacterDefinition != null)
            {
                slot.SetDefinition(slot.CharacterDefinition);
            }
        }

        if (slots.Length > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(slots[0].gameObject.scene);
        }
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

        var inputAsset = FindAsset<UnityEngine.InputSystem.InputActionAsset>("t:InputActionAsset", "Assets/InputSystem_Actions.inputactions");

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

        var statsPanelObj = GameObject.Find("StatsPanel");
        if (statsPanelObj != null && statsPanelObj.TryGetComponent<StatsDisplayUI>(out var statDisplayComp))
        {
            soWheel.FindProperty("statsDisplay").objectReferenceValue = statDisplayComp;
        }

        soWheel.ApplyModifiedProperties();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvas.scene);
    }
}
#endif
