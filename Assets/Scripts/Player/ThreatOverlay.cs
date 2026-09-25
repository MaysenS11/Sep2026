using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Core.Board;
using Core.Occupants;

namespace Player
{
    /// <summary>
    /// Threat overlay controller: when holding Tab, highlights potential enemy move and attack tiles on the board.
    /// </summary>
    public class ThreatOverlay : MonoBehaviour
    {
        public static ThreatOverlay Instance { get; private set; }

        [Header("Colors")]
        [SerializeField] private Color moveTileColor = new Color(1f, 0.85f, 0.1f, 0.45f);
        [SerializeField] private Color attackTileColor = new Color(0.95f, 0.15f, 0.15f, 0.65f);
        [SerializeField] private int sortingOrder = 5;

        private readonly HashSet<Vector2Int> currentMoveTiles = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> currentAttackTiles = new HashSet<Vector2Int>();

        private readonly List<GameObject> activeIndicators = new List<GameObject>();
        private readonly Stack<GameObject> indicatorPool = new Stack<GameObject>();

        private static Sprite s_SquareSprite;
        private bool isOverlayActive = false;

        public bool IsOverlayActive => isOverlayActive;
        public IReadOnlyCollection<Vector2Int> CurrentMoveTiles => currentMoveTiles;
        public IReadOnlyCollection<Vector2Int> CurrentAttackTiles => currentAttackTiles;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Update()
        {
            bool tabHeld = IsTabHeld();

            if (tabHeld && !isOverlayActive)
            {
                SetOverlayActive(true);
            }
            else if (!tabHeld && isOverlayActive)
            {
                SetOverlayActive(false);
            }

            if (isOverlayActive)
            {
                UpdateThreatTiles();
            }
        }

        private bool IsTabHeld()
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.isPressed)
            {
                return true;
            }

            try
            {
                if (Input.GetKey(KeyCode.Tab))
                {
                    return true;
                }
            }
            catch
            {
                // In test or non-legacy input contexts, catch gracefully
            }

            return false;
        }

        public void SetOverlayActive(bool active)
        {
            isOverlayActive = active;
            if (isOverlayActive)
            {
                UpdateThreatTiles();
            }
            else
            {
                HideAllIndicators();
                currentMoveTiles.Clear();
                currentAttackTiles.Clear();
            }
        }

        public void UpdateThreatTiles()
        {
            currentMoveTiles.Clear();
            currentAttackTiles.Clear();

            GameBoard board = GameManager.Instance != null ? GameManager.Instance.Board : null;
            int roomIndex = GameManager.Instance != null ? GameManager.Instance.CurrentRoomIndex : -1;

            if (board != null)
            {
                CalculateThreatTiles(board, roomIndex, currentMoveTiles, currentAttackTiles);
            }

            RenderIndicators();
        }

        public static void CalculateThreatTiles(
            GameBoard board,
            int currentRoomIndex,
            HashSet<Vector2Int> outMoveTiles,
            HashSet<Vector2Int> outAttackTiles)
        {
            if (board == null) return;

            var player = board.FindPlayer();

            GameManager.RoomData currentRoom = null;
            if (GameManager.Instance != null && GameManager.Instance.DungeonDictionary != null && currentRoomIndex >= 0)
            {
                GameManager.Instance.DungeonDictionary.TryGetValue(currentRoomIndex, out currentRoom);
            }

            foreach (var enemy in board.GetOccupantsOfType<EnemyOccupant>())
            {
                if (enemy == null || enemy.IsDead) continue;

                // Only evaluate enemies in current room if room data is available
                if (currentRoom != null)
                {
                    Vector2Int origin = currentRoom.WorldOriginTile;
                    Vector2Int size = currentRoom.Size;
                    if (enemy.GridPosition.x < origin.x || enemy.GridPosition.x >= origin.x + size.x ||
                        enemy.GridPosition.y < origin.y || enemy.GridPosition.y >= origin.y + size.y)
                    {
                        continue;
                    }
                }

                CalculateIndividualEnemyThreat(board, enemy, player, outMoveTiles, outAttackTiles);
            }
        }

        private static void CalculateIndividualEnemyThreat(
            GameBoard board,
            EnemyOccupant enemy,
            PlayerOccupant player,
            HashSet<Vector2Int> outMoveTiles,
            HashSet<Vector2Int> outAttackTiles)
        {
            switch (enemy.Archetype)
            {
                case EnemyArchetype.Knight:
                    Vector2Int[] knightOffsets = new Vector2Int[]
                    {
                        new Vector2Int(1, 2), new Vector2Int(2, 1),
                        new Vector2Int(-1, 2), new Vector2Int(-2, 1),
                        new Vector2Int(1, -2), new Vector2Int(2, -1),
                        new Vector2Int(-1, -2), new Vector2Int(-2, -1)
                    };
                    for (int o = 0; o < knightOffsets.Length; o++)
                    {
                        Vector2Int target = enemy.GridPosition + knightOffsets[o];
                        if (!board.IsInBounds(target) || board.IsWall(target)) continue;

                        var occ = board.GetOccupant(target);
                        if (occ is PlayerOccupant)
                        {
                            outAttackTiles.Add(target);
                        }
                        else if (occ == null)
                        {
                            outMoveTiles.Add(target);
                        }
                    }
                    break;

                case EnemyArchetype.Pawn:
                    Vector2Int[] pawnDirs = new Vector2Int[] { BoardCoordinate.North, BoardCoordinate.South, BoardCoordinate.East, BoardCoordinate.West };
                    for (int d = 0; d < pawnDirs.Length; d++)
                    {
                        Vector2Int target = enemy.GridPosition + pawnDirs[d];
                        if (!board.IsInBounds(target) || board.IsWall(target)) continue;

                        var occ = board.GetOccupant(target);
                        if (occ is PlayerOccupant)
                        {
                            outAttackTiles.Add(target);
                        }
                        else if (occ == null)
                        {
                            outMoveTiles.Add(target);
                        }
                    }
                    break;

                case EnemyArchetype.Rook:
                    EvaluateRayThreat(board, enemy, new Vector2Int[] { BoardCoordinate.North, BoardCoordinate.South, BoardCoordinate.East, BoardCoordinate.West }, outMoveTiles, outAttackTiles);
                    break;

                case EnemyArchetype.Bishop:
                    EvaluateRayThreat(board, enemy, new Vector2Int[] { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) }, outMoveTiles, outAttackTiles);
                    break;

                case EnemyArchetype.Queen:
                default:
                    EvaluateRayThreat(board, enemy, new Vector2Int[]
                    {
                        BoardCoordinate.North, BoardCoordinate.South, BoardCoordinate.East, BoardCoordinate.West,
                        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                    }, outMoveTiles, outAttackTiles);
                    break;
            }
        }

        private static void EvaluateRayThreat(
            GameBoard board,
            EnemyOccupant enemy,
            Vector2Int[] directions,
            HashSet<Vector2Int> outMoveTiles,
            HashSet<Vector2Int> outAttackTiles)
        {
            int maxRange = enemy.DetectionRange > 0 ? enemy.DetectionRange : 12;

            for (int d = 0; d < directions.Length; d++)
            {
                Vector2Int dir = directions[d];
                for (int step = 1; step <= maxRange; step++)
                {
                    Vector2Int target = enemy.GridPosition + (dir * step);
                    if (!board.IsInBounds(target) || board.IsWall(target))
                    {
                        break;
                    }

                    var occ = board.GetOccupant(target);
                    if (occ != null)
                    {
                        if (occ is PlayerOccupant)
                        {
                            outAttackTiles.Add(target);
                        }
                        break;
                    }

                    outMoveTiles.Add(target);
                }
            }
        }

        private void RenderIndicators()
        {
            HideAllIndicators();

            // Render move tiles
            foreach (var tile in currentMoveTiles)
            {
                // If tile is also an attack tile, attack priority wins
                if (currentAttackTiles.Contains(tile)) continue;

                var go = GetPooledIndicator();
                go.transform.position = BoardCoordinate.GridToWorldCenter(tile);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.color = moveTileColor;
                sr.sortingOrder = sortingOrder;
                go.SetActive(true);
                activeIndicators.Add(go);
            }

            // Render attack tiles
            foreach (var tile in currentAttackTiles)
            {
                var go = GetPooledIndicator();
                go.transform.position = BoardCoordinate.GridToWorldCenter(tile);
                var sr = go.GetComponent<SpriteRenderer>();
                sr.color = attackTileColor;
                sr.sortingOrder = sortingOrder + 1;
                go.SetActive(true);
                activeIndicators.Add(go);
            }
        }

        private GameObject GetPooledIndicator()
        {
            if (indicatorPool.Count > 0)
            {
                return indicatorPool.Pop();
            }

            var go = new GameObject("ThreatIndicator", typeof(SpriteRenderer));
            go.transform.SetParent(transform);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateSquareSprite();
            return go;
        }

        private void HideAllIndicators()
        {
            for (int i = 0; i < activeIndicators.Count; i++)
            {
                if (activeIndicators[i] != null)
                {
                    activeIndicators[i].SetActive(false);
                    indicatorPool.Push(activeIndicators[i]);
                }
            }
            activeIndicators.Clear();
        }

        public static Sprite GetOrCreateSquareSprite()
        {
            if (s_SquareSprite != null) return s_SquareSprite;

            int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            Color fill = Color.white;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, fill);
                }
            }
            texture.Apply();
            s_SquareSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
            return s_SquareSprite;
        }
    }
}
