using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Core.Board;
using Core.Occupants;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Presentation.Board
{
    [ExecuteAlways]
    public class BoardGridOverlay : MonoBehaviour
    {
        private static BoardGridOverlay _instance;
        public static BoardGridOverlay Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<BoardGridOverlay>();
                }
                return _instance;
            }
        }

        [SerializeField] private bool showOverlay = false;
        [SerializeField] private bool showGridLines = true;
        [SerializeField] private bool showOccupantLabels = true;
        [SerializeField] private bool showCoordinates = true;
        [SerializeField] private bool showFloatingButton = true;
        [SerializeField] private bool showPanel = true;

        private readonly Color colorPlayer = new Color(0.2f, 0.6f, 1f, 0.85f);
        private readonly Color colorEnemy = new Color(1f, 0.25f, 0.25f, 0.85f);
        private readonly Color colorBarrel = new Color(1f, 0.6f, 0.15f, 0.85f);
        private readonly Color colorChest = new Color(1f, 0.9f, 0.2f, 0.85f);
        private readonly Color colorPillar = new Color(0.75f, 0.35f, 1f, 0.85f);
        private readonly Color colorDoorIn = new Color(0.2f, 0.95f, 0.4f, 0.85f);
        private readonly Color colorDoorOut = new Color(0.1f, 0.8f, 0.8f, 0.85f);
        private readonly Color colorDoorSpec = new Color(1f, 0.3f, 0.8f, 0.85f);
        private readonly Color colorFloor = new Color(1f, 1f, 1f, 0.15f);

        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;
        private GUIStyle buttonStyle;
        private Texture2D whiteTexture;

        public bool ShowOverlay
        {
            get => showOverlay;
            set => showOverlay = value;
        }

        public static BoardGridOverlay GetOrCreate()
        {
            if (Instance != null) return Instance;

            var existing = UnityEngine.Object.FindAnyObjectByType<BoardGridOverlay>();
            if (existing != null)
            {
                _instance = existing;
                return _instance;
            }

            var go = new GameObject("[BoardGridOverlay]");
            _instance = go.AddComponent<BoardGridOverlay>();
            return _instance;
        }

        public static void Toggle()
        {
            var overlay = GetOrCreate();
            overlay.ShowOverlay = !overlay.ShowOverlay;
            Debug.Log($"BoardGridOverlay is now {(overlay.ShowOverlay ? "ENABLED" : "DISABLED")}");
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }

        private void Update()
        {
            if (CheckToggleInput())
            {
                showOverlay = !showOverlay;
            }
        }

        private bool CheckToggleInput()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.f3Key.wasPressedThisFrame ||
                    Keyboard.current.f1Key.wasPressedThisFrame ||
                    Keyboard.current.backquoteKey.wasPressedThisFrame)
                {
                    return true;
                }
            }

            try
            {
                if (Input.GetKeyDown(KeyCode.F3) || Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.BackQuote))
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private void InitGUIStyles()
        {
            if (whiteTexture == null)
            {
                whiteTexture = new Texture2D(1, 1);
                whiteTexture.SetPixel(0, 0, Color.white);
                whiteTexture.Apply();
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };
                labelStyle.normal.textColor = Color.white;
            }

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
                headerStyle.normal.textColor = Color.yellow;
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box);
            }

            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold
                };
            }
        }

        private void OnGUI()
        {
            InitGUIStyles();

            var gm = GameManager.Instance;
            var board = gm != null ? gm.Board : null;
            var doors = BoardPrinter.ExtractDoorsFromGameManager();

            Camera cam = Camera.main;
            if (cam == null) cam = Camera.current;

            if (showFloatingButton)
            {
                DrawFloatingToggleButton();
            }

            if (!showOverlay) return;

            if (cam != null && board != null)
            {
                DrawOnScreenGrid(cam, board, doors);
            }

            if (showPanel)
            {
                DrawControlPanel(board, doors);
            }
        }

        private void DrawFloatingToggleButton()
        {
            Rect btnRect = new Rect(10, 10, 160, 26);
            string btnText = showOverlay ? "👁 Debug Board: ON (F3)" : "👁 Debug Board: OFF (F3)";
            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = showOverlay ? new Color(0.2f, 0.8f, 0.3f, 0.9f) : new Color(0.3f, 0.3f, 0.3f, 0.7f);

            if (GUI.Button(btnRect, btnText, buttonStyle))
            {
                showOverlay = !showOverlay;
            }

            GUI.backgroundColor = originalBg;
        }

        private void DrawOnScreenGrid(Camera cam, GameBoard board, Dictionary<Vector2Int, DoorInfo> doors)
        {
            Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
            Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));

            int minX = Mathf.Max(board.Origin.x, Mathf.FloorToInt(Mathf.Min(bl.x, tr.x)) - 1);
            int maxX = Mathf.Min(board.Origin.x + board.Width - 1, Mathf.CeilToInt(Mathf.Max(bl.x, tr.x)) + 1);
            int minY = Mathf.Max(board.Origin.y, Mathf.FloorToInt(Mathf.Min(bl.y, tr.y)) - 1);
            int maxY = Mathf.Min(board.Origin.y + board.Height - 1, Mathf.CeilToInt(Mathf.Max(bl.y, tr.y)) + 1);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var cell = board.GetCell(pos);
                    bool isFloor = cell != null && cell.Terrain == TerrainType.Floor;
                    var occ = board.GetOccupant(pos);
                    bool hasDoor = doors.TryGetValue(pos, out var door);

                    if (!isFloor && occ == null && !hasDoor) continue;

                    Vector3 worldMin = new Vector3(x, y, 0);
                    Vector3 worldMax = new Vector3(x + 1, y + 1, 0);
                    Vector3 screenMin = cam.WorldToScreenPoint(worldMin);
                    Vector3 screenMax = cam.WorldToScreenPoint(worldMax);

                    float rectX = Mathf.Min(screenMin.x, screenMax.x);
                    float rectY = Screen.height - Mathf.Max(screenMin.y, screenMax.y);
                    float rectW = Mathf.Abs(screenMax.x - screenMin.x);
                    float rectH = Mathf.Abs(screenMax.y - screenMin.y);

                    if (rectW < 6f || rectH < 6f) continue;

                    Rect screenRect = new Rect(rectX, rectY, rectW, rectH);

                    Color cellColor = colorFloor;
                    string symbol = ".";
                    string nameTag = "";
                    int occId = 0;

                    if (occ != null)
                    {
                        occId = occ.Id;
                        if (occ is PlayerOccupant)
                        {
                            cellColor = colorPlayer;
                            symbol = "P";
                            nameTag = "Player";
                        }
                        else if (occ is EnemyOccupant)
                        {
                            cellColor = colorEnemy;
                            symbol = "E";
                            nameTag = occ.Name;
                        }
                        else if (occ is DestructiblePropOccupant)
                        {
                            cellColor = colorBarrel;
                            symbol = "B";
                            nameTag = "Barrel";
                        }
                        else if (occ is ChestOccupant)
                        {
                            cellColor = colorChest;
                            symbol = "C";
                            nameTag = "Chest";
                        }
                        else if (occ is PillarOccupant pillar)
                        {
                            cellColor = colorPillar;
                            symbol = "∏";
                            nameTag = $"{pillar.FootprintSize.x}x{pillar.FootprintSize.y}";
                        }
                        else
                        {
                            cellColor = colorPillar;
                            symbol = occ.Name.Length > 0 ? occ.Name[0].ToString() : "O";
                            nameTag = occ.Name;
                        }
                    }
                    else if (hasDoor)
                    {
                        if (door.DoorType == "D_in") cellColor = colorDoorIn;
                        else if (door.DoorType == "D_out") cellColor = colorDoorOut;
                        else cellColor = colorDoorSpec;
                        symbol = "D";
                        nameTag = door.DoorType;
                    }

                    if (showGridLines)
                    {
                        Color fill = new Color(cellColor.r, cellColor.g, cellColor.b, occ != null || hasDoor ? 0.35f : 0.08f);
                        DrawScreenRect(screenRect, fill);
                        DrawScreenOutline(screenRect, cellColor, occ != null || hasDoor ? 2f : 1f);
                    }

                    if (showOccupantLabels && (occ != null || hasDoor))
                    {
                        string labelText = $"{symbol}\n#{occId}";
                        if (rectH > 35 && !string.IsNullOrEmpty(nameTag))
                        {
                            labelText = $"{symbol} {nameTag}\n#{occId}";
                        }

                        Color oldColor = labelStyle.normal.textColor;
                        labelStyle.normal.textColor = Color.white;
                        GUI.Label(screenRect, labelText, labelStyle);
                        labelStyle.normal.textColor = oldColor;
                    }

                    if (showCoordinates && rectH > 24)
                    {
                        Rect coordRect = new Rect(screenRect.x, screenRect.yMax - 14, screenRect.width, 14);
                        var oldAlign = labelStyle.alignment;
                        var oldSize = labelStyle.fontSize;
                        labelStyle.alignment = TextAnchor.LowerCenter;
                        labelStyle.fontSize = 9;
                        labelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);
                        GUI.Label(coordRect, $"{x},{y}", labelStyle);
                        labelStyle.fontSize = oldSize;
                        labelStyle.alignment = oldAlign;
                    }
                }
            }

            DrawPillarFootprintOutlines(cam, board);
        }

        private void DrawPillarFootprintOutlines(Camera cam, GameBoard board)
        {
            var pillarGroups = new Dictionary<Vector2Int, Vector2Int>();
            foreach (var occ in board.GetOccupantsOfType<PillarOccupant>())
            {
                if (occ != null && !pillarGroups.ContainsKey(occ.FootprintOrigin))
                {
                    pillarGroups[occ.FootprintOrigin] = occ.FootprintSize;
                }
            }

            foreach (var kvp in pillarGroups)
            {
                Vector2Int origin = kvp.Key;
                Vector2Int size = kvp.Value;

                Vector3 worldMin = new Vector3(origin.x, origin.y, 0);
                Vector3 worldMax = new Vector3(origin.x + size.x, origin.y + size.y, 0);
                Vector3 screenMin = cam.WorldToScreenPoint(worldMin);
                Vector3 screenMax = cam.WorldToScreenPoint(worldMax);

                float rx = Mathf.Min(screenMin.x, screenMax.x);
                float ry = Screen.height - Mathf.Max(screenMin.y, screenMax.y);
                float rw = Mathf.Abs(screenMax.x - screenMin.x);
                float rh = Mathf.Abs(screenMax.y - screenMin.y);

                if (rw > 0 && rh > 0)
                {
                    DrawScreenOutline(new Rect(rx, ry, rw, rh), new Color(1f, 0.4f, 1f, 1f), 3f);
                }
            }
        }

        private void DrawControlPanel(GameBoard board, Dictionary<Vector2Int, DoorInfo> doors)
        {
            Rect legendRect = new Rect(10, 42, 230, 210);
            GUI.Box(legendRect, GUIContent.none, boxStyle);

            GUILayout.BeginArea(new Rect(15, 47, 220, 200));
            GUILayout.Label("Overlay Controls", headerStyle);

            showGridLines = GUILayout.Toggle(showGridLines, "Grid Outlines");
            showOccupantLabels = GUILayout.Toggle(showOccupantLabels, "Occupant Labels");
            showCoordinates = GUILayout.Toggle(showCoordinates, "Coordinates (x,y)");

            GUILayout.Space(4);
            int occCount = board != null ? board.GetAllOccupants().Count : 0;
            int doorCount = doors != null ? doors.Count : 0;
            GUILayout.Label($"Occupants: {occCount} | Doors: {doorCount}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dump ASCII", GUILayout.Height(22)))
            {
                BoardPrinter.DumpToFile();
            }
            if (GUILayout.Button("Console", GUILayout.Height(22)))
            {
                BoardPrinter.DumpToConsole();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Close Overlay (F3)", GUILayout.Height(22)))
            {
                showOverlay = false;
            }

            GUILayout.EndArea();
        }

        private void DrawScreenRect(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture);
            GUI.color = old;
        }

        private void DrawScreenOutline(Rect rect, Color color, float thickness)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), whiteTexture);
            GUI.color = old;
        }

        private void OnDrawGizmos()
        {
            if (!showOverlay) return;

            var gm = GameManager.Instance;
            var board = gm != null ? gm.Board : null;
            if (board == null) return;

            var doors = BoardPrinter.ExtractDoorsFromGameManager();

            foreach (var kvp in doors)
            {
                Vector2Int pos = kvp.Key;
                DoorInfo door = kvp.Value;
                Vector3 center = BoardCoordinate.GridToWorldCenter(pos);
                Gizmos.color = door.DoorType == "D_in" ? colorDoorIn : (door.DoorType == "D_out" ? colorDoorOut : colorDoorSpec);
                Gizmos.DrawWireCube(center, new Vector3(0.95f, 0.95f, 0f));
            }

            foreach (var occ in board.GetAllOccupants())
            {
                if (occ == null) continue;
                Vector3 center = BoardCoordinate.GridToWorldCenter(occ.GridPosition);

                Color c = colorEnemy;
                if (occ is PlayerOccupant) c = colorPlayer;
                else if (occ is DestructiblePropOccupant) c = colorBarrel;
                else if (occ is ChestOccupant) c = colorChest;
                else if (occ is PillarOccupant) c = colorPillar;

                Gizmos.color = c;
                Gizmos.DrawWireCube(center, new Vector3(0.95f, 0.95f, 0f));
            }

            var pillarGroups = new Dictionary<Vector2Int, Vector2Int>();
            foreach (var occ in board.GetOccupantsOfType<PillarOccupant>())
            {
                if (occ != null && !pillarGroups.ContainsKey(occ.FootprintOrigin))
                {
                    pillarGroups[occ.FootprintOrigin] = occ.FootprintSize;
                }
            }

            foreach (var kvp in pillarGroups)
            {
                Vector2Int origin = kvp.Key;
                Vector2Int size = kvp.Value;
                Vector3 footprintCenter = new Vector3(origin.x + size.x * 0.5f, origin.y + size.y * 0.5f, 0);
                Vector3 footprintSize = new Vector3(size.x, size.y, 0);

                Gizmos.color = new Color(1f, 0.3f, 1f, 0.9f);
                Gizmos.DrawWireCube(footprintCenter, footprintSize);
            }
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Board/Toggle In-Game Grid Overlay %&b")]
        public static void MenuItemToggleOverlay()
        {
            Toggle();
        }

        [MenuItem("Tools/Board/Attach Board Grid Overlay to Scene")]
        public static void MenuItemAttachOverlay()
        {
            var overlay = GetOrCreate();
            overlay.ShowOverlay = true;
            Selection.activeGameObject = overlay.gameObject;
            Debug.Log("Attached BoardGridOverlay to active scene.");
        }
#endif
    }
}
