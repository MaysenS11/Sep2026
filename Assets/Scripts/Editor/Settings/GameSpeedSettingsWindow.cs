using UnityEditor;
using UnityEngine;
using Infrastructure;

namespace GameSettings.Editor
{
    public class GameSpeedSettingsWindow : EditorWindow
    {
        private const string EnemySettingsPath = "Assets/ScriptableObjects/EnemySettings.asset";

        private float playerMoveDuration = 0.18f;
        private float playerAttackDuration = 0.18f;
        private float enemyMoveDuration = 0.24f;

        private EnemySettings cachedEnemySettings;
        private GameManager cachedGameManager;
        private Vector2 scrollPos;

        [MenuItem("Tools/Game Speed Settings")]
        [MenuItem("Window/Game Speed Settings")]
        public static void Open()
        {
            var window = GetWindow<GameSpeedSettingsWindow>("Game Speed");
            window.minSize = new Vector2(340, 360);
            window.Show();
        }

        private void OnEnable()
        {
            LoadCurrentValues();
        }

        private void OnFocus()
        {
            LoadCurrentValues();
        }

        private void LoadCurrentValues()
        {
            cachedEnemySettings = AssetDatabase.LoadAssetAtPath<EnemySettings>(EnemySettingsPath);
            if (cachedEnemySettings != null)
            {
                enemyMoveDuration = cachedEnemySettings.EnemyMoveDuration;
            }

            cachedGameManager = Object.FindAnyObjectByType<GameManager>();
            if (cachedGameManager != null)
            {
                playerMoveDuration = cachedGameManager.PlayerMoveDuration;
                playerAttackDuration = cachedGameManager.PlayerAttackDuration;
                if (cachedGameManager.EnemyTurnTotalDuration > 0f)
                {
                    enemyMoveDuration = cachedGameManager.EnemyTurnTotalDuration;
                }
            }
            else if (Application.isPlaying && GameManager.Instance != null && GameManager.Instance.TurnCoordinator != null)
            {
                playerMoveDuration = GameManager.Instance.TurnCoordinator.PlayerMoveDuration;
                playerAttackDuration = GameManager.Instance.TurnCoordinator.PlayerAttackDuration;
                enemyMoveDuration = GameManager.Instance.TurnCoordinator.EnemyTurnTotalDuration;
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Turn & Step Speed Controls", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                Application.isPlaying
                    ? "Play Mode active: adjustments apply immediately in real time."
                    : "Edit Mode: adjustments save to EnemySettings asset and scene GameManager.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUI.BeginChangeCheck();

            playerMoveDuration = EditorGUILayout.Slider("Player Step Duration (s)", playerMoveDuration, 0.05f, 0.50f);
            playerAttackDuration = EditorGUILayout.Slider("Player Attack Duration (s)", playerAttackDuration, 0.05f, 0.50f);
            enemyMoveDuration = EditorGUILayout.Slider("Enemy Step Duration (s)", enemyMoveDuration, 0.05f, 0.50f);

            if (EditorGUI.EndChangeCheck())
            {
                ApplyValues();
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Speed Presets", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Very Fast (0.12s)"))
            {
                SetPreset(0.12f, 0.15f, 0.15f);
            }
            if (GUILayout.Button("Fast (0.15s)"))
            {
                SetPreset(0.15f, 0.18f, 0.20f);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Normal (0.18s)"))
            {
                SetPreset(0.18f, 0.18f, 0.24f);
            }
            if (GUILayout.Button("Slow (0.24s)"))
            {
                SetPreset(0.24f, 0.24f, 0.30f);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(16);
            EditorGUILayout.LabelField("References & Assets", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping EnemySettings Asset"))
            {
                if (cachedEnemySettings != null)
                {
                    EditorGUIUtility.PingObject(cachedEnemySettings);
                    Selection.activeObject = cachedEnemySettings;
                }
            }
            if (GUILayout.Button("Ping GameManager"))
            {
                if (cachedGameManager == null) cachedGameManager = Object.FindAnyObjectByType<GameManager>();
                if (cachedGameManager != null)
                {
                    EditorGUIUtility.PingObject(cachedGameManager.gameObject);
                    Selection.activeGameObject = cachedGameManager.gameObject;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Save Assets", GUILayout.Height(28)))
            {
                ApplyValues();
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.EndScrollView();
        }

        private void SetPreset(float pMove, float pAtk, float eMove)
        {
            playerMoveDuration = pMove;
            playerAttackDuration = pAtk;
            enemyMoveDuration = eMove;
            ApplyValues();
        }

        private void ApplyValues()
        {
            if (cachedEnemySettings == null)
            {
                cachedEnemySettings = AssetDatabase.LoadAssetAtPath<EnemySettings>(EnemySettingsPath);
            }

            if (cachedEnemySettings != null)
            {
                var so = new SerializedObject(cachedEnemySettings);
                var prop = so.FindProperty("enemyMoveDuration");
                if (prop != null)
                {
                    prop.floatValue = enemyMoveDuration;
                    so.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(cachedEnemySettings);
            }

            if (cachedGameManager == null)
            {
                cachedGameManager = Object.FindAnyObjectByType<GameManager>();
            }

            if (cachedGameManager != null)
            {
                var so = new SerializedObject(cachedGameManager);
                var pMoveProp = so.FindProperty("playerMoveDuration");
                var pAtkProp = so.FindProperty("playerAttackDuration");
                var eMoveProp = so.FindProperty("enemyTurnTotalDuration");
                var eSettingsProp = so.FindProperty("enemySettings");

                if (pMoveProp != null) pMoveProp.floatValue = playerMoveDuration;
                if (pAtkProp != null) pAtkProp.floatValue = playerAttackDuration;
                if (eMoveProp != null) eMoveProp.floatValue = enemyMoveDuration;
                if (eSettingsProp != null && eSettingsProp.objectReferenceValue == null && cachedEnemySettings != null)
                {
                    eSettingsProp.objectReferenceValue = cachedEnemySettings;
                }

                so.ApplyModifiedProperties();
                cachedGameManager.ApplySpeedSettings();
                EditorUtility.SetDirty(cachedGameManager);
            }

            if (Application.isPlaying && GameManager.Instance != null && GameManager.Instance.TurnCoordinator != null)
            {
                GameManager.Instance.TurnCoordinator.PlayerMoveDuration = playerMoveDuration;
                GameManager.Instance.TurnCoordinator.PlayerAttackDuration = playerAttackDuration;
                GameManager.Instance.TurnCoordinator.EnemyTurnTotalDuration = enemyMoveDuration;
                if (cachedEnemySettings != null)
                {
                    GameManager.Instance.TurnCoordinator.EnemySettings = cachedEnemySettings;
                }
            }
        }
    }
}
