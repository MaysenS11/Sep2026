using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace Chest.Editor
{
    public static class ChestUIConfigurator
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

        [MenuItem("Tools/Chest UI/Configure Chest UI and Stats")]
        public static void Configure()
        {
            var panelGo = GameObject.Find("/Canvas/ChestPanel");
            if (panelGo == null)
            {
                Debug.LogError("Could not find /Canvas/ChestPanel");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(panelGo, "Configure Chest UI");

            // 1. Hide or Remove ConfirmButton
            var confirmBtn = panelGo.transform.Find("ConfirmButton");
            if (confirmBtn != null)
            {
                confirmBtn.gameObject.SetActive(false);
            }

            // 2. Adjust CardContainer position and placeholders
            var cardContainer = panelGo.transform.Find("CardContainer") as RectTransform;
            if (cardContainer != null)
            {
                // Move card container to the right side to leave room for StatsPanel
                cardContainer.anchoredPosition = new Vector2(100, 0);
            }

            // 3. Make sure Card placeholders have visible sprites in editor
            var statCardDb = FindAsset<StatCardDatabase>("t:StatCardDatabase", "Assets/ScriptableObjects/StatCards/StatCardDatabase.asset");
            Sprite defaultSprite = null;
            if (statCardDb != null)
            {
                defaultSprite = statCardDb.GetSpriteForStat(StatType.AttackDamage);
            }

            if (cardContainer != null)
            {
                StatType[] fallbackTypes = new StatType[] { StatType.AttackRange, StatType.Defence, StatType.AttackDamage };
                for (int i = 0; i < cardContainer.childCount; i++)
                {
                    var child = cardContainer.GetChild(i);
                    var img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        if (statCardDb != null && i < fallbackTypes.Length)
                        {
                            var s = statCardDb.GetSpriteForStat(fallbackTypes[i]);
                            img.sprite = s != null ? s : defaultSprite;
                        }
                        else if (defaultSprite != null && img.sprite == null)
                        {
                            img.sprite = defaultSprite;
                        }
                        img.color = Color.white;
                    }
                    var border = child.Find("highlightBorder");
                    if (border != null)
                    {
                        border.gameObject.SetActive(false);
                    }
                }
            }

            // 4. Create or update StatsPanel
            Transform statsPanelTransform = panelGo.transform.Find("StatsPanel");
            GameObject statsPanelGo;
            if (statsPanelTransform == null)
            {
                statsPanelGo = new GameObject("StatsPanel", typeof(RectTransform));
                statsPanelGo.transform.SetParent(panelGo.transform, false);
            }
            else
            {
                statsPanelGo = statsPanelTransform.gameObject;
            }

            RectTransform statsRect = statsPanelGo.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.5f, 0.5f);
            statsRect.anchorMax = new Vector2(0.5f, 0.5f);
            statsRect.pivot = new Vector2(0.5f, 0.5f);
            statsRect.anchoredPosition = new Vector2(-250, 0);
            statsRect.sizeDelta = new Vector2(220, 240);

            var vlg = statsPanelGo.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = statsPanelGo.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 16f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fontAsset = FindAsset<TMP_FontAsset>("Jacquard12-Regular SDF t:TMP_FontAsset", "Assets/TextMesh Pro/Font/Jacquard12-Regular SDF.asset");
            Sprite unlockedPip = FindAsset<Sprite>("Map_Curr t:Sprite", "Assets/Sprites/Menu/Map_Curr.png");
            Sprite lockedPip = FindAsset<Sprite>("Map_Norm t:Sprite", "Assets/Sprites/Menu/Map_Norm.png");

            var statDisplay = statsPanelGo.GetComponent<ChestStatDisplayUI>();
            if (statDisplay == null) statDisplay = statsPanelGo.AddComponent<ChestStatDisplayUI>();

            SerializedObject displaySo = new SerializedObject(statDisplay);
            displaySo.FindProperty("unlockedSprite").objectReferenceValue = unlockedPip;
            displaySo.FindProperty("lockedSprite").objectReferenceValue = lockedPip;
            displaySo.FindProperty("unlockedColor").colorValue = Color.white;
            displaySo.FindProperty("lockedColor").colorValue = new Color(0.4f, 0.4f, 0.4f, 0.8f);

            var statDefs = new (string label, StatType type)[]
            {
                ("RANGE", StatType.AttackRange),
                ("SPEED", StatType.Speed),
                ("ATTACK", StatType.Defence),
                ("DAMAGE", StatType.AttackDamage)
            };

            var rowsProp = displaySo.FindProperty("rows");
            rowsProp.arraySize = statDefs.Length;

            for (int r = 0; r < statDefs.Length; r++)
            {
                string rowName = $"Row_{statDefs[r].label}";
                Transform rowT = statsPanelGo.transform.Find(rowName);
                GameObject rowGo;
                if (rowT == null)
                {
                    rowGo = new GameObject(rowName, typeof(RectTransform));
                    rowGo.transform.SetParent(statsPanelGo.transform, false);
                }
                else
                {
                    rowGo = rowT.gameObject;
                }

                RectTransform rowRect = rowGo.GetComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(220, 36);

                var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
                if (hlg == null) hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.spacing = 10f;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;

                // Label
                Transform labelT = rowGo.transform.Find("Label");
                GameObject labelGo;
                if (labelT == null)
                {
                    labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    labelGo.transform.SetParent(rowGo.transform, false);
                }
                else
                {
                    labelGo = labelT.gameObject;
                }
                RectTransform labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.sizeDelta = new Vector2(100, 30);
                var tmp = labelGo.GetComponent<TextMeshProUGUI>();
                if (fontAsset != null) tmp.font = fontAsset;
                tmp.fontSize = 24;
                tmp.text = statDefs[r].label;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.color = new Color(0.95f, 0.95f, 0.95f, 1f);

                // Pips Container
                Transform pipsT = rowGo.transform.Find("Pips");
                GameObject pipsGo;
                if (pipsT == null)
                {
                    pipsGo = new GameObject("Pips", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    pipsGo.transform.SetParent(rowGo.transform, false);
                }
                else
                {
                    pipsGo = pipsT.gameObject;
                }
                RectTransform pipsRect = pipsGo.GetComponent<RectTransform>();
                pipsRect.sizeDelta = new Vector2(90, 30);
                var pipsHlg = pipsGo.GetComponent<HorizontalLayoutGroup>();
                pipsHlg.childAlignment = TextAnchor.MiddleRight;
                pipsHlg.spacing = 8f;
                pipsHlg.childControlWidth = false;
                pipsHlg.childControlHeight = false;

                // 3 Pip Images
                var rowElement = rowsProp.GetArrayElementAtIndex(r);
                rowElement.FindPropertyRelative("label").stringValue = statDefs[r].label;
                rowElement.FindPropertyRelative("statType").enumValueIndex = (int)statDefs[r].type;
                rowElement.FindPropertyRelative("labelText").objectReferenceValue = tmp;

                var pipsProp = rowElement.FindPropertyRelative("pips");
                pipsProp.arraySize = 3;

                for (int p = 0; p < 3; p++)
                {
                    string pipName = $"Pip_{p}";
                    Transform pipT = pipsGo.transform.Find(pipName);
                    GameObject pipGo;
                    if (pipT == null)
                    {
                        pipGo = new GameObject(pipName, typeof(RectTransform), typeof(Image));
                        pipGo.transform.SetParent(pipsGo.transform, false);
                    }
                    else
                    {
                        pipGo = pipT.gameObject;
                    }
                    RectTransform pipRect = pipGo.GetComponent<RectTransform>();
                    pipRect.sizeDelta = new Vector2(20, 20);
                    var img = pipGo.GetComponent<Image>();
                    img.sprite = p == 0 ? unlockedPip : lockedPip;
                    img.color = p == 0 ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.8f);

                    pipsProp.GetArrayElementAtIndex(p).objectReferenceValue = img;
                }
            }

            displaySo.ApplyModifiedProperties();

            // 5. Wire up ChestCardManager
            var mgr = panelGo.GetComponent<ChestCardManager>();
            if (mgr != null)
            {
                SerializedObject mgrSo = new SerializedObject(mgr);
                mgrSo.FindProperty("statDisplayUI").objectReferenceValue = statDisplay;
                mgrSo.ApplyModifiedProperties();
            }

            // Ensure panel is active in Scene for editing visibility
            panelGo.SetActive(true);
            var cg = panelGo.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
            }

            EditorUtility.SetDirty(panelGo);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panelGo.scene);
            Debug.Log("[ChestUIConfigurator] Chest UI configured successfully!");
        }
    }
}
