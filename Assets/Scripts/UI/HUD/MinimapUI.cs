using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Minimap controller for dungeon navigation.
    /// Reveals rooms on entry, auto-discovers child chest rooms when parent room is visited,
    /// and renders room icons (Map_Normal, Map_Chest, Map_Boss, Map_Curr) and interconnecting corridors.
    /// </summary>
    public class MinimapUI : MonoBehaviour
    {
        public static MinimapUI Instance { get; private set; }

        [Header("Room Icons")]
        [SerializeField] private Sprite mapNormalSprite;
        [SerializeField] private Sprite mapChestSprite;
        [SerializeField] private Sprite mapBossSprite;
        [SerializeField] private Sprite mapCurrSprite;

        [Header("Layout Settings")]
        [SerializeField] private RectTransform mapContainer;
        [SerializeField] private Vector2 iconSize = new Vector2(24f, 24f);
        [SerializeField] private float corridorThickness = 3f;
        [SerializeField] private float iconSpacing = 36f;
        [SerializeField] private Color corridorColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);

        private readonly HashSet<int> revealedRooms = new HashSet<int>();
        private readonly HashSet<int> discoveredRooms = new HashSet<int>();
        private int currentRoomIndex = 0;

        private readonly List<GameObject> spawnedElements = new List<GameObject>();
        private readonly Dictionary<int, RectTransform> roomIconRects = new Dictionary<int, RectTransform>();

        public IReadOnlyCollection<int> RevealedRooms => revealedRooms;
        public IReadOnlyCollection<int> DiscoveredRooms => discoveredRooms;
        public int CurrentRoomIndex => currentRoomIndex;

        public bool IsRoomRevealed(int roomIndex) => revealedRooms.Contains(roomIndex);
        public bool IsRoomDiscovered(int roomIndex) => discoveredRooms.Contains(roomIndex);

        public Sprite MapNormalSprite => mapNormalSprite;
        public Sprite MapChestSprite => mapChestSprite;
        public Sprite MapBossSprite => mapBossSprite;
        public Sprite MapCurrSprite => mapCurrSprite;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            EnsureSprites();
            EnsureContainer();
        }

        private void OnEnable()
        {
            EventBus<RoomEnteredEvent>.Subscribe(OnRoomEntered);
            RefreshMinimap();
        }

        private void OnDisable()
        {
            EventBus<RoomEnteredEvent>.Unsubscribe(OnRoomEntered);
        }

        private void Start()
        {
            RefreshMinimap();
        }

        private void EnsureContainer()
        {
            if (mapContainer == null)
            {
                var found = transform.Find("MinimapContainer") as RectTransform
                         ?? transform.Find("MapContainer") as RectTransform
                         ?? transform.Find("Minimap") as RectTransform;

                if (found != null)
                {
                    mapContainer = found;
                }
                else
                {
                    var go = new GameObject("MinimapContainer", typeof(RectTransform));
                    go.transform.SetParent(transform, false);
                    mapContainer = go.GetComponent<RectTransform>();
                    mapContainer.anchorMin = new Vector2(1f, 1f);
                    mapContainer.anchorMax = new Vector2(1f, 1f);
                    mapContainer.pivot = new Vector2(1f, 1f);
                    mapContainer.anchoredPosition = new Vector2(-20f, -20f);
                    mapContainer.sizeDelta = new Vector2(180f, 180f);
                }
            }
        }

        private void EnsureSprites()
        {
            if (mapNormalSprite == null) mapNormalSprite = Resources.Load<Sprite>("Map_Normal");
            if (mapChestSprite == null) mapChestSprite = Resources.Load<Sprite>("Map_Chest");
            if (mapBossSprite == null) mapBossSprite = Resources.Load<Sprite>("Map_Boss");
            if (mapCurrSprite == null) mapCurrSprite = Resources.Load<Sprite>("Map_Curr");

            if (mapNormalSprite == null) mapNormalSprite = CreateProceduralIcon("Map_Normal", new Color(0.35f, 0.45f, 0.65f), IconShape.Square);
            if (mapChestSprite == null) mapChestSprite = CreateProceduralIcon("Map_Chest", new Color(1f, 0.85f, 0.2f), IconShape.Diamond);
            if (mapBossSprite == null) mapBossSprite = CreateProceduralIcon("Map_Boss", new Color(0.9f, 0.2f, 0.25f), IconShape.Skull);
            if (mapCurrSprite == null) mapCurrSprite = CreateProceduralIcon("Map_Curr", new Color(0.2f, 0.95f, 1f), IconShape.Frame);
        }

        private enum IconShape { Square, Diamond, Skull, Frame }

        private Sprite CreateProceduralIcon(string name, Color color, IconShape shape)
        {
            int size = 24;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            Color clear = Color.clear;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            int margin = 2;
            int innerMin = margin;
            int innerMax = size - 1 - margin;

            for (int y = innerMin; y <= innerMax; y++)
            {
                for (int x = innerMin; x <= innerMax; x++)
                {
                    bool paint = false;
                    switch (shape)
                    {
                        case IconShape.Square:
                            paint = true;
                            break;
                        case IconShape.Diamond:
                            int mid = size / 2;
                            int dist = Mathf.Abs(x - mid) + Mathf.Abs(y - mid);
                            paint = dist <= (size / 2 - margin);
                            break;
                        case IconShape.Skull:
                            paint = (y >= innerMin + 4) || (x >= innerMin + 4 && x <= innerMax - 4);
                            break;
                        case IconShape.Frame:
                            paint = (x <= innerMin + 2 || x >= innerMax - 2 || y <= innerMin + 2 || y >= innerMax - 2);
                            break;
                    }

                    if (paint)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public void OnRoomEntered(RoomEnteredEvent evt)
        {
            if (evt.Room == null) return;

            RevealRoom(evt.Room.RoomIndex);

            // Auto-discover child chest rooms when parent room is entered
            if (evt.Room.HasSpecialChestRoom && evt.Room.SpecialChestRoomIndex >= 0)
            {
                DiscoverRoom(evt.Room.SpecialChestRoomIndex);
            }

            if (GameManager.Instance != null && GameManager.Instance.DungeonDictionary != null)
            {
                foreach (var pair in GameManager.Instance.DungeonDictionary)
                {
                    if (pair.Value.ParentRoomIndex == evt.Room.RoomIndex && pair.Value.Type == RoomType.Chest)
                    {
                        DiscoverRoom(pair.Key);
                    }
                }
            }

            RefreshMinimap();
        }

        public void RevealRoom(int roomIndex)
        {
            revealedRooms.Add(roomIndex);
            currentRoomIndex = roomIndex;
        }

        public void DiscoverRoom(int roomIndex)
        {
            discoveredRooms.Add(roomIndex);
        }

        public void ResetMinimap()
        {
            revealedRooms.Clear();
            discoveredRooms.Clear();
            currentRoomIndex = 0;
            ClearSpawned();
        }

        public void RefreshMinimap()
        {
            if (mapContainer == null) EnsureContainer();
            ClearSpawned();

            if (GameManager.Instance == null || GameManager.Instance.DungeonDictionary == null || GameManager.Instance.DungeonDictionary.Count == 0)
            {
                return;
            }

            var dungeon = GameManager.Instance.DungeonDictionary;

            // Gather all visible rooms
            var visibleRooms = new List<GameManager.RoomData>();
            Vector2 minMacro = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 maxMacro = new Vector2(float.MinValue, float.MinValue);

            foreach (var kvp in dungeon)
            {
                int rIdx = kvp.Key;
                var room = kvp.Value;
                if (revealedRooms.Contains(rIdx) || discoveredRooms.Contains(rIdx))
                {
                    visibleRooms.Add(room);
                    minMacro.x = Mathf.Min(minMacro.x, room.MacroPos.x);
                    minMacro.y = Mathf.Min(minMacro.y, room.MacroPos.y);
                    maxMacro.x = Mathf.Max(maxMacro.x, room.MacroPos.x);
                    maxMacro.y = Mathf.Max(maxMacro.y, room.MacroPos.y);
                }
            }

            if (visibleRooms.Count == 0) return;

            Vector2 centerMacro = (minMacro + maxMacro) * 0.5f;

            // 1. Render interconnecting corridors
            var drawnConnections = new HashSet<(int, int)>();
            for (int i = 0; i < visibleRooms.Count; i++)
            {
                var roomA = visibleRooms[i];
                if (roomA.ParentRoomIndex >= 0 && dungeon.TryGetValue(roomA.ParentRoomIndex, out var parentRoom))
                {
                    if (revealedRooms.Contains(parentRoom.RoomIndex) || discoveredRooms.Contains(parentRoom.RoomIndex))
                    {
                        var key1 = (Mathf.Min(roomA.RoomIndex, parentRoom.RoomIndex), Mathf.Max(roomA.RoomIndex, parentRoom.RoomIndex));
                        if (!drawnConnections.Contains(key1))
                        {
                            drawnConnections.Add(key1);
                            Vector2 posA = (roomA.MacroPos - centerMacro) * iconSpacing;
                            Vector2 posB = (parentRoom.MacroPos - centerMacro) * iconSpacing;
                            DrawCorridorLine(posA, posB);
                        }
                    }
                }
            }

            // 2. Render room icons
            for (int i = 0; i < visibleRooms.Count; i++)
            {
                var room = visibleRooms[i];
                Vector2 anchoredPos = (room.MacroPos - centerMacro) * iconSpacing;

                bool isCurrent = (room.RoomIndex == currentRoomIndex);
                Sprite iconSprite = GetSpriteForRoom(room, isCurrent);

                var iconGo = new GameObject($"RoomIcon_{room.RoomIndex}", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(mapContainer, false);
                spawnedElements.Add(iconGo);

                var rt = iconGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = anchoredPos;
                rt.sizeDelta = iconSize;

                var img = iconGo.GetComponent<Image>();
                img.sprite = iconSprite;
                img.raycastTarget = false;

                // If current room, overlay the current room indicator frame
                if (isCurrent && mapCurrSprite != null)
                {
                    var currOverlay = new GameObject($"CurrOverlay_{room.RoomIndex}", typeof(RectTransform), typeof(Image));
                    currOverlay.transform.SetParent(iconGo.transform, false);
                    var currRt = currOverlay.GetComponent<RectTransform>();
                    currRt.anchorMin = Vector2.zero;
                    currRt.anchorMax = Vector2.one;
                    currRt.sizeDelta = new Vector2(8f, 8f);
                    currRt.anchoredPosition = Vector2.zero;
                    var currImg = currOverlay.GetComponent<Image>();
                    currImg.sprite = mapCurrSprite;
                    currImg.raycastTarget = false;
                }

                // If only discovered (child chest room not yet entered), set slightly translucent
                if (!revealedRooms.Contains(room.RoomIndex) && discoveredRooms.Contains(room.RoomIndex))
                {
                    img.color = new Color(1f, 1f, 1f, 0.7f);
                }

                roomIconRects[room.RoomIndex] = rt;
            }
        }

        private Sprite GetSpriteForRoom(GameManager.RoomData room, bool isCurrent)
        {
            if (isCurrent && mapCurrSprite != null) return mapCurrSprite;

            switch (room.Type)
            {
                case RoomType.Chest:
                    return mapChestSprite;
                case RoomType.Boss:
                    return mapBossSprite;
                case RoomType.Start:
                case RoomType.Normal:
                default:
                    return mapNormalSprite;
            }
        }

        private void DrawCorridorLine(Vector2 posA, Vector2 posB)
        {
            var lineGo = new GameObject("CorridorLine", typeof(RectTransform), typeof(Image));
            lineGo.transform.SetParent(mapContainer, false);
            lineGo.transform.SetAsFirstSibling();
            spawnedElements.Add(lineGo);

            var rt = lineGo.GetComponent<RectTransform>();
            Vector2 dir = posB - posA;
            float distance = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = posA;
            rt.sizeDelta = new Vector2(distance, corridorThickness);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);

            var img = lineGo.GetComponent<Image>();
            img.color = corridorColor;
            img.raycastTarget = false;
        }

        private void ClearSpawned()
        {
            for (int i = 0; i < spawnedElements.Count; i++)
            {
                if (spawnedElements[i] != null)
                {
                    if (Application.isPlaying) Destroy(spawnedElements[i]);
                    else DestroyImmediate(spawnedElements[i]);
                }
            }
            spawnedElements.Clear();
            roomIconRects.Clear();
        }
    }
}
