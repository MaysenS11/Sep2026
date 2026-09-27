using UnityEditor;
using UnityEngine;
using Core.Board;
using Presentation.Board;

namespace Presentation.Board.Editor
{
    public class BoardViewerWindow : EditorWindow
    {
        [MenuItem("Window/Board/Debug Board Viewer")]
        public static void Open()
        {
            var win = GetWindow<BoardViewerWindow>("Board Viewer");
            win.minSize = new Vector2(300, 360);
            win.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("GameBoard Debug Viewer", EditorStyles.boldLabel);
            GUILayout.Label("Toggle visual overlays, inspect cells, or dump ASCII grids.", EditorStyles.miniLabel);
            EditorGUILayout.Space();

            var overlay = BoardGridOverlay.GetOrCreate();

            bool currentShow = overlay.ShowOverlay;
            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = currentShow ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.9f, 0.3f, 0.3f);

            string btnText = currentShow ? "Overlay is ENABLED (Click to Hide)" : "Overlay is DISABLED (Click to Show)";
            if (GUILayout.Button(btnText, GUILayout.Height(36)))
            {
                overlay.ShowOverlay = !overlay.ShowOverlay;
                EditorUtility.SetDirty(overlay);
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = originalBg;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hotkeys in Game", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Press F1, F3, or ~ (Tilde) in-game to toggle the overlay on/off at runtime.\nClick the on-screen pill button in the top-left corner anytime.", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ASCII Board Dumps", EditorStyles.boldLabel);
            if (GUILayout.Button("Dump ASCII to Console", GUILayout.Height(26)))
            {
                BoardPrinter.DumpToConsole();
            }

            if (GUILayout.Button("Dump ASCII to File (GameBoard_Dump.txt)", GUILayout.Height(26)))
            {
                BoardPrinter.DumpToFile();
            }

            EditorGUILayout.Space();
            var gm = GameManager.Instance;
            var board = gm != null ? gm.Board : null;

            EditorGUILayout.LabelField("Live Board Info", EditorStyles.boldLabel);
            if (board != null)
            {
                EditorGUILayout.LabelField("Origin", $"({board.Origin.x}, {board.Origin.y})");
                EditorGUILayout.LabelField("Dimensions", $"{board.Width} x {board.Height}");
                EditorGUILayout.LabelField("Total Occupants", board.GetAllOccupants().Count.ToString());
            }
            else
            {
                EditorGUILayout.HelpBox("No active GameBoard loaded in current scene.", MessageType.Warning);
            }
        }
    }
}
