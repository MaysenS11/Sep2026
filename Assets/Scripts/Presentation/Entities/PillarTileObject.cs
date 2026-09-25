using System.Collections.Generic;
using UnityEngine;
using Core.Board;
using Core.Occupants;

namespace Presentation.Entities
{
    /// <summary>
    /// Presenter view for indestructible multi-tile pillars (1x1, 1x2, 2x2, etc.).
    /// Maintains a single unified visual GameObject while each occupied tile is registered
    /// as an individual blocking occupant on the authoritative GameBoard.
    /// 
    /// Implements grid-based transparency: if player or entity coordinates sit on tiles
    /// directly behind the pillar relative to the camera (North / +Y), smoothly lerps
    /// the pillar sprite alpha to remain transparent.
    /// </summary>
    public class PillarTileObject : TileObject
    {
        [Header("Footprint Configuration")]
        [Tooltip("Dimensions in grid cells (e.g. 1x1, 1x2, 2x2)")]
        [SerializeField] private Vector2Int size = Vector2Int.one;

        [Tooltip("Bottom-left origin grid coordinate of this pillar")]
        [SerializeField] private Vector2Int footprintOrigin;

        [Header("Grid-Based Transparency")]
        [Range(0f, 1f)]
        [SerializeField] private float transparentAlpha = 0.35f;

        [Range(0f, 1f)]
        [SerializeField] private float opaqueAlpha = 1.0f;

        [Tooltip("Alpha transition speed (units per second)")]
        [SerializeField] private float fadeSpeed = 7.0f;

        [Tooltip("How many tiles directly north behind the pillar are checked for occlusion")]
        [Range(1, 4)]
        [SerializeField] private int behindTileDepth = 2;

        private readonly List<int> _occupantIds = new List<int>();
        private readonly HashSet<Vector2Int> _occupiedTiles = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _behindTiles = new HashSet<Vector2Int>();

        private SpriteRenderer[] _cachedRenderers;
        private float _currentAlpha = 1.0f;
        private GameBoard _boundBoard;

        public Vector2Int Size
        {
            get => (size.x < 1 || size.y < 1) ? Vector2Int.one : size;
            set
            {
                size = new Vector2Int(Mathf.Max(1, value.x), Mathf.Max(1, value.y));
                RecalculateTiles();
            }
        }

        public Vector2Int FootprintOrigin
        {
            get => footprintOrigin;
            set
            {
                footprintOrigin = value;
                RecalculateTiles();
            }
        }

        public float CurrentAlpha => _currentAlpha;

        public float TransparentAlpha
        {
            get => transparentAlpha;
            set => transparentAlpha = Mathf.Clamp01(value);
        }

        public float OpaqueAlpha
        {
            get => opaqueAlpha;
            set => opaqueAlpha = Mathf.Clamp01(value);
        }

        public float FadeSpeed
        {
            get => fadeSpeed;
            set => fadeSpeed = Mathf.Max(0.1f, value);
        }

        public int BehindTileDepth
        {
            get => behindTileDepth;
            set
            {
                behindTileDepth = Mathf.Max(1, value);
                RecalculateTiles();
            }
        }

        public IReadOnlyList<int> OccupantIds => _occupantIds;
        public IReadOnlyCollection<Vector2Int> OccupiedTiles => _occupiedTiles;
        public IReadOnlyCollection<Vector2Int> BehindTiles => _behindTiles;
        public bool IsEntityBehind { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            CacheRenderers();
            RecalculateTiles();
            _currentAlpha = opaqueAlpha;
            ApplyAlpha(_currentAlpha);
        }

        /// <summary>
        /// Initializes the pillar footprint, recalculates covered and behind tiles, and centers visual transform.
        /// </summary>
        public void Initialize(Vector2Int origin, Vector2Int pillarSize, GameBoard board = null)
        {
            footprintOrigin = origin;
            size = new Vector2Int(Mathf.Max(1, pillarSize.x), Mathf.Max(1, pillarSize.y));
            _boundBoard = board;

            RecalculateTiles();
            CacheRenderers();
            SnapToFootprint(origin, size);
        }

        /// <summary>
        /// Associates a simulation occupant ID that belongs to one of this pillar's tiles.
        /// </summary>
        public void RegisterOccupantId(int id)
        {
            if (!_occupantIds.Contains(id))
            {
                _occupantIds.Add(id);
            }
            OccupantId = id; // Update primary ID for base TileObject
        }

        /// <summary>
        /// Clears all associated simulation occupant IDs.
        /// </summary>
        public void ClearOccupantIds()
        {
            _occupantIds.Clear();
        }

        /// <summary>
        /// Computes the exact grid coordinates covered by this pillar and the coordinates
        /// sitting directly behind it relative to the camera (North / +Y axis).
        /// </summary>
        public void RecalculateTiles()
        {
            _occupiedTiles.Clear();
            _behindTiles.Clear();

            int w = Mathf.Max(1, size.x);
            int h = Mathf.Max(1, size.y);

            // 1. Covered tiles: [origin.x .. origin.x + w - 1] x [origin.y .. origin.y + h - 1]
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    _occupiedTiles.Add(new Vector2Int(footprintOrigin.x + x, footprintOrigin.y + y));
                }
            }

            // 2. Behind tiles (relative to camera looking from South to North):
            // Directly north of each column, starting at top row + 1 up to behindTileDepth
            int topY = footprintOrigin.y + h - 1;
            for (int x = 0; x < w; x++)
            {
                int colX = footprintOrigin.x + x;
                for (int d = 1; d <= behindTileDepth; d++)
                {
                    _behindTiles.Add(new Vector2Int(colX, topY + d));
                }
            }
        }

        /// <summary>
        /// Centers the visual GameObject over the pillar's footprint in world space.
        /// </summary>
        public void SnapToFootprint(Vector2Int origin, Vector2Int pillarSize)
        {
            CurrentGridPosition = origin;
            // Center of footprint: origin + size * 0.5
            float worldX = origin.x + (pillarSize.x * 0.5f);
            float worldY = origin.y + (pillarSize.y * 0.5f);
            transform.position = new Vector3(worldX, worldY, transform.position.z);
        }

        private void CacheRenderers()
        {
            _cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            if ((_cachedRenderers == null || _cachedRenderers.Length == 0) && spriteRenderer != null)
            {
                _cachedRenderers = new[] { spriteRenderer };
            }
        }

        private void Update()
        {
            UpdateTransparency(Time.deltaTime);
        }

        /// <summary>
        /// Evaluates whether any entity is sitting on tiles directly behind the pillar and lerps alpha accordingly.
        /// Public and callable deterministically from unit tests without waiting for PlayMode.
        /// </summary>
        public void UpdateTransparency(float deltaTime, GameBoard boardOverride = null)
        {
            IsEntityBehind = EvaluateBehindState(boardOverride);

            float targetAlpha = IsEntityBehind ? transparentAlpha : opaqueAlpha;
            if (deltaTime > 0f)
            {
                _currentAlpha = Mathf.MoveTowards(_currentAlpha, targetAlpha, fadeSpeed * deltaTime);
            }
            else
            {
                _currentAlpha = targetAlpha;
            }

            ApplyAlpha(_currentAlpha);
        }

        /// <summary>
        /// Checks if player or any active entity sits on any tile directly behind the pillar.
        /// </summary>
        public bool EvaluateBehindState(GameBoard boardOverride = null)
        {
            GameBoard board = boardOverride ?? _boundBoard;
            if (board == null && GameManager.Instance != null)
            {
                board = GameManager.Instance.Board;
            }

            // 1. Authoritative simulation GameBoard query
            if (board != null)
            {
                foreach (Vector2Int behindTile in _behindTiles)
                {
                    TileOccupant occ = board.GetOccupant(behindTile);
                    if (occ != null)
                    {
                        // Any occupant on the behind tile (Player, Enemy, etc.) occludes
                        return true;
                    }
                }
            }

            // 2. Direct Player position query (scene / presentation fallback)
            var playerObj = Object.FindAnyObjectByType<PlayerMovement>();
            if (playerObj != null)
            {
                Vector2Int playerGrid = BoardCoordinate.WorldToGrid(playerObj.transform.position);
                if (_behindTiles.Contains(playerGrid))
                {
                    return true;
                }
            }

            // 3. Direct Enemy positions query (scene fallback)
            var enemyPresenters = Object.FindObjectsByType<EnemyTileObject>(FindObjectsSortMode.None);
            if (enemyPresenters != null)
            {
                for (int i = 0; i < enemyPresenters.Length; i++)
                {
                    var enemy = enemyPresenters[i];
                    if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                    Vector2Int enemyGrid = BoardCoordinate.WorldToGrid(enemy.transform.position);
                    if (_behindTiles.Contains(enemyGrid))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Instantly sets the alpha of all sprite renderers without lerping.
        /// </summary>
        public void SetAlphaImmediate(float alpha)
        {
            _currentAlpha = Mathf.Clamp01(alpha);
            ApplyAlpha(_currentAlpha);
        }

        private void ApplyAlpha(float alpha)
        {
            if (_cachedRenderers == null || _cachedRenderers.Length == 0)
            {
                CacheRenderers();
            }

            if (_cachedRenderers == null) return;

            for (int i = 0; i < _cachedRenderers.Length; i++)
            {
                var sr = _cachedRenderers[i];
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = alpha;
                    sr.color = c;
                }
            }
        }

        public bool IsBehindTile(Vector2Int tile)
        {
            return _behindTiles.Contains(tile);
        }

        public bool IsOccupiedTile(Vector2Int tile)
        {
            return _occupiedTiles.Contains(tile);
        }
    }
}
