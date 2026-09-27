using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using FMODUnity;
using Core.Board;
using Core.Occupants;
using Infrastructure;
using Presentation.Board;
using Presentation.Entities;

namespace Dungeon
{
    public class BossRoom : MonoBehaviour
    {
        [System.Serializable]
        public class BannerHealthSlot
        {
            public SpriteRenderer bannerRenderer;
            public Sprite fullBannerSprite;
            public Sprite emptyBannerSprite;
            public List<GameObject> heartFills = new List<GameObject>();
        }

        [Header("Room Layout")]
        [SerializeField] private Vector2Int roomSize = new Vector2Int(18, 18);
        [SerializeField] private Transform doorMarker;

        [Header("Authored Layer Tilemaps")]
        [SerializeField] private Tilemap floorTilemap;
        [SerializeField] private Tilemap wallTilemap;

        [Header("King Boss Reference")]
        [SerializeField] private EnemyBase kingEnemy;
        [SerializeField] private Animator kingAnimator;
        [SerializeField] private EventReference kingDamageSound;
        [SerializeField] private EventReference kingDeathSound;

        [Header("Boss Health & Banner Display")]
        [Min(1)] [SerializeField] private int bossMaxHealth = 20;
        [SerializeField] private List<BannerHealthSlot> bannerSlots = new List<BannerHealthSlot>();

        [Header("Boss Damage On Minion Death")]
        [Min(0)] [SerializeField] private int pawnBossDamage = 1;
        [Min(0)] [SerializeField] private int knightBossDamage = 2;
        [Min(0)] [SerializeField] private int bishopBossDamage = 2;
        [Min(0)] [SerializeField] private int rookBossDamage = 3;
        [Min(0)] [SerializeField] private int queenBossDamage = 4;

        private int currentBossHealth = 20;
        private int currentRoomIndex = -1;
        private bool isRegistered = false;
        private bool isDefeated = false;
        private readonly HashSet<GameObject> registeredMinions = new HashSet<GameObject>();

        public Vector2Int RoomSize => roomSize;
        public Transform DoorMarker => doorMarker;
        public int BossMaxHealth => bossMaxHealth;
        public int CurrentBossHealth => currentBossHealth;
        public int CurrentRoomIndex => currentRoomIndex;
        public List<BannerHealthSlot> BannerSlots => bannerSlots;

        public int PawnBossDamage { get => pawnBossDamage; set => pawnBossDamage = Mathf.Max(0, value); }
        public int KnightBossDamage { get => knightBossDamage; set => knightBossDamage = Mathf.Max(0, value); }
        public int BishopBossDamage { get => bishopBossDamage; set => bishopBossDamage = Mathf.Max(0, value); }
        public int RookBossDamage { get => rookBossDamage; set => rookBossDamage = Mathf.Max(0, value); }
        public int QueenBossDamage { get => queenBossDamage; set => queenBossDamage = Mathf.Max(0, value); }

        private void Awake()
        {
            currentBossHealth = bossMaxHealth;
            ResolveTilemaps();
            ResolveKing();
        }

        private void OnEnable()
        {
            EventBus<EntityDiedEvent>.Subscribe(OnEntityDied);
        }

        private void OnDisable()
        {
            EventBus<EntityDiedEvent>.Unsubscribe(OnEntityDied);
        }

        private void ResolveTilemaps()
        {
            if (floorTilemap != null && wallTilemap != null) return;

            var tilemaps = GetComponentsInChildren<Tilemap>(true);
            for (int i = 0; i < tilemaps.Length; i++)
            {
                var tm = tilemaps[i];
                if (tm == null) continue;
                string lower = tm.name.ToLower();
                if (floorTilemap == null && lower.Contains("floor"))
                {
                    floorTilemap = tm;
                }
                else if (wallTilemap == null && lower.Contains("wall"))
                {
                    wallTilemap = tm;
                }
            }
        }

        private void ResolveKing()
        {
            if (kingEnemy == null)
            {
                var enemies = GetComponentsInChildren<EnemyBase>(true);
                for (int i = 0; i < enemies.Length; i++)
                {
                    if (enemies[i] != null && enemies[i].name.ToLower().Contains("king"))
                    {
                        kingEnemy = enemies[i];
                        break;
                    }
                }
            }

            if (kingEnemy != null && kingAnimator == null)
            {
                kingAnimator = kingEnemy.GetComponentInChildren<Animator>();
            }

            if (kingEnemy != null && kingEnemy.Data != null)
            {
                if (kingDamageSound.IsNull) kingDamageSound = kingEnemy.Data.DamageSound;
                if (kingDeathSound.IsNull) kingDeathSound = kingEnemy.Data.DeathSound;
            }
        }

        public void CopyTilesTo(
            Vector2Int worldOriginTile,
            Tilemap targetFloor,
            Tilemap targetWall)
        {
            ResolveTilemaps();

            if (floorTilemap != null && targetFloor != null)
            {
                CopyLayer(floorTilemap, targetFloor, worldOriginTile);
            }

            if (wallTilemap != null && targetWall != null)
            {
                CopyLayer(wallTilemap, targetWall, worldOriginTile);
            }
        }

        private void CopyLayer(Tilemap source, Tilemap target, Vector2Int worldOriginTile)
        {
            if (source == null || target == null) return;

            BoundsInt bounds = source.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int srcPos = new Vector3Int(x, y, 0);
                    TileBase tile = source.GetTile(srcPos);
                    if (tile != null)
                    {
                        target.SetTile(new Vector3Int(worldOriginTile.x + x, worldOriginTile.y + y, 0), tile);
                    }
                }
            }
        }

        public void DisableLocalTilemapRenderers()
        {
            var renderers = GetComponentsInChildren<TilemapRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = false;
                }
            }
        }

        public Vector2Int GetWorldDoorTile(Vector2Int worldOriginTile)
        {
            Vector2Int local = doorMarker != null
                ? new Vector2Int(Mathf.RoundToInt(transform.InverseTransformPoint(doorMarker.position).x), Mathf.RoundToInt(transform.InverseTransformPoint(doorMarker.position).y))
                : new Vector2Int(roomSize.x / 2, 1);

            return worldOriginTile + local;
        }

        public void ScanAndRegisterBossEntities(
            Vector2Int worldOriginTile,
            GameBoard board,
            EffectsQueueRunner effectsRunner,
            int roomIndex)
        {
            if (isRegistered || board == null) return;
            isRegistered = true;
            currentRoomIndex = roomIndex;
            currentBossHealth = bossMaxHealth;

            ResolveKing();
            if (kingEnemy != null)
            {
                kingEnemy.CurrentRoomIndex = roomIndex;
                if (!kingEnemy.gameObject.activeSelf)
                {
                    kingEnemy.gameObject.SetActive(true);
                }
            }

            registeredMinions.Clear();
            var enemies = GetComponentsInChildren<EnemyBase>(true);
            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || enemy == kingEnemy || enemy.name.ToLower().Contains("king"))
                {
                    continue;
                }

                if (!enemy.gameObject.activeSelf)
                {
                    enemy.gameObject.SetActive(true);
                }

                enemy.CurrentRoomIndex = roomIndex;
                registeredMinions.Add(enemy.gameObject);

                Vector2Int tile = BoardCoordinate.WorldToGrid(enemy.transform.position);

                EnemyArchetype archetype = EnemyArchetype.Pawn;
                if (enemy.Data != null)
                {
                    archetype = enemy.Data.Archetype;
                }
                else
                {
                    string lower = enemy.name.ToLower();
                    if (lower.Contains("knight")) archetype = EnemyArchetype.Knight;
                    else if (lower.Contains("bishop")) archetype = EnemyArchetype.Bishop;
                    else if (lower.Contains("rook")) archetype = EnemyArchetype.Rook;
                    else if (lower.Contains("queen")) archetype = EnemyArchetype.Queen;
                }

                BoardEntityFactory.CreateEnemy(enemy.gameObject, tile, board, archetype, enemy.Data, effectsRunner);

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RegisterEnemy(enemy);
                }
                EventBus<EnemyRegisteredEvent>.Raise(new EnemyRegisteredEvent(enemy));
            }

            var pillars = GetComponentsInChildren<PillarTileObject>(true);
            for (int i = 0; i < pillars.Length; i++)
            {
                var pillar = pillars[i];
                if (pillar == null) continue;

                Vector2Int pos = BoardCoordinate.WorldToGrid(pillar.transform.position);
                Vector2Int pSize = pillar.Size;
                string pName = pillar.name.ToLower();
                if (pName.Contains("4x2")) pSize = new Vector2Int(4, 2);
                else if (pName.Contains("2x2")) pSize = new Vector2Int(2, 2);
                else if (pName.Contains("1x2")) pSize = new Vector2Int(1, 2);

                BoardEntityFactory.CreatePillar(pillar.gameObject, pos, pSize, board, effectsRunner);
            }

            RefreshBannerVisuals();
        }

        public int GetDamageForEnemy(EnemyData data)
        {
            if (data == null) return 1;

            switch (data.Archetype)
            {
                case EnemyArchetype.Pawn:
                    return pawnBossDamage;
                case EnemyArchetype.Knight:
                    return knightBossDamage;
                case EnemyArchetype.Bishop:
                    return bishopBossDamage;
                case EnemyArchetype.Rook:
                    return rookBossDamage;
                case EnemyArchetype.Queen:
                    return queenBossDamage;
                default:
                    return 1;
            }
        }

        private void OnEntityDied(EntityDiedEvent evt)
        {
            if (isDefeated || evt.Entity == null) return;

            bool isMyMinion = registeredMinions.Contains(evt.Entity);
            if (!isMyMinion && evt.Entity.TryGetComponent<EnemyBase>(out var eb))
            {
                if (eb.CurrentRoomIndex == currentRoomIndex && currentRoomIndex >= 0)
                {
                    isMyMinion = true;
                }
            }

            if (!isMyMinion) return;

            registeredMinions.Remove(evt.Entity);

            EnemyData data = null;
            if (evt.Entity.TryGetComponent<EnemyBase>(out var enemyComp))
            {
                data = enemyComp.Data;
            }

            int damage = GetDamageForEnemy(data);
            ApplyBossDamage(damage);
        }

        public void ApplyBossDamage(int damage)
        {
            if (isDefeated) return;

            currentBossHealth = Mathf.Max(0, currentBossHealth - damage);

            if (kingAnimator != null)
            {
                kingAnimator.SetTrigger("TakeDamage");
            }

            PlaySound(kingDamageSound);

            RefreshBannerVisuals();

            if (currentBossHealth <= 0)
            {
                isDefeated = true;
                StartCoroutine(KingDefeatSequence());
            }
        }

        public void RefreshBannerVisuals()
        {
            if (bannerSlots == null || bannerSlots.Count == 0) return;

            int lostHealth = Mathf.Max(0, bossMaxHealth - currentBossHealth);
            int accumulatedLoss = 0;

            for (int b = 0; b < bannerSlots.Count; b++)
            {
                var slot = bannerSlots[b];
                if (slot == null) continue;

                int slotCapacity = slot.heartFills != null ? slot.heartFills.Count : 0;
                int slotLoss = Mathf.Clamp(lostHealth - accumulatedLoss, 0, slotCapacity);
                accumulatedLoss += slotCapacity;

                if (slot.heartFills != null)
                {
                    for (int h = 0; h < slot.heartFills.Count; h++)
                    {
                        var heartObj = slot.heartFills[h];
                        if (heartObj != null)
                        {
                            bool isActive = h >= slotLoss;
                            heartObj.SetActive(isActive);
                        }
                    }
                }

                if (slot.bannerRenderer != null)
                {
                    if (slotCapacity > 0 && slotLoss >= slotCapacity && slot.emptyBannerSprite != null)
                    {
                        slot.bannerRenderer.sprite = slot.emptyBannerSprite;
                    }
                    else if (slot.fullBannerSprite != null)
                    {
                        slot.bannerRenderer.sprite = slot.fullBannerSprite;
                    }
                }
            }
        }

        private IEnumerator KingDefeatSequence()
        {
            if (kingAnimator != null)
            {
                kingAnimator.ResetTrigger("TakeDamage");
                kingAnimator.SetTrigger("Die");
                kingAnimator.SetTrigger("Decapitate");
            }

            PlaySound(kingDeathSound);

            yield return new WaitForSeconds(1.0f);

            string maskId = "Default";
            if (CharacterSelectData.SelectedCharacter != null)
            {
                maskId = !string.IsNullOrEmpty(CharacterSelectData.SelectedCharacter.CharacterName)
                    ? CharacterSelectData.SelectedCharacter.CharacterName
                    : CharacterSelectData.SelectedCharacter.name;
            }

            string defeatKey = $"first_king_defeat_{maskId}";
            bool isFirstDefeat = PlayerPrefs.GetInt(defeatKey, 0) == 0;
            if (isFirstDefeat)
            {
                PlayerPrefs.SetInt(defeatKey, 1);
                int currentKeys = PlayerPrefs.GetInt("MaskKeys", 0) + 1;
                PlayerPrefs.SetInt("MaskKeys", currentKeys);
                PlayerPrefs.Save();
            }

            float runTime = UIManager.Instance != null ? UIManager.Instance.ElapsedTime : 0f;
            EventBus<GameWonEvent>.Raise(new GameWonEvent(runTime, maskId, isFirstDefeat));
            EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent(GameState.Gameplay, GameState.GameOver));

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowWinScreen();
            }
        }

        private void PlaySound(EventReference sound)
        {
            if (sound.IsNull) return;
            try
            {
                if (Application.isPlaying)
                {
                    RuntimeManager.PlayOneShot(sound);
                }
            }
            catch { }
        }
    }
}
