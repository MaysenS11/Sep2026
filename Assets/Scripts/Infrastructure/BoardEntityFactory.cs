using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Board;
using Core.Occupants;
using Presentation.Board;
using Presentation.Entities;

namespace Infrastructure
{
    /// Factory bridging room and spawner instantiation to authoritative simulation TileOccupants
    /// and visual TileObject presenters. Strips legacy physics components to guarantee objects
    /// are never pushed off tiles by Unity physics.
    public static class BoardEntityFactory
    {
        private static int _nextOccupantId = 1;

        public static int GetNextOccupantId() => _nextOccupantId++;

        public static void ResetIdCounter(int startId = 1)
        {
            _nextOccupantId = startId;
        }

        #region Physics Stripping

        /// Disables and removes all Rigidbody2D and Collider2D components from the GameObject
        /// to ensure Unity physics never pushes units between tiles or causes stacking.
        public static void StripPhysicsComponents(GameObject go)
        {
            if (go == null) return;

            var rigidbodies = go.GetComponentsInChildren<Rigidbody2D>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                var rb = rigidbodies[i];
                if (rb != null)
                {
                    rb.simulated = false;
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(rb);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(rb);
                    }
                }
            }

            var colliders = go.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (col != null)
                {
                    col.enabled = false;
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(col);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(col);
                    }
                }
            }
        }

        #endregion

        #region Factory Creation Methods

        /// Instantiates and binds a PlayerOccupant to a Player GameObject and PlayerTileObject presenter.
        public static PlayerOccupant CreatePlayer(
            GameObject go,
            Vector2Int gridPos,
            GameBoard board,
            int maxHealth = 6,
            int attackDamage = 2,
            EffectsQueueRunner runner = null)
        {
            if (go == null) throw new ArgumentNullException(nameof(go));
            if (board == null) throw new ArgumentNullException(nameof(board));

            StripPhysicsComponents(go);

            PlayerTileObject presenter = go.GetComponent<PlayerTileObject>();
            if (presenter == null)
            {
                presenter = go.AddComponent<PlayerTileObject>();
            }

            int id = GetNextOccupantId();
            var occupant = new PlayerOccupant(
                maxHealth: maxHealth,
                attackDamage: attackDamage,
                initialPosition: gridPos,
                id: id,
                name: "Player"
            );

            presenter.OccupantId = id;
            presenter.SnapToGrid(gridPos);

            if (go.TryGetComponent<PlayerMovement>(out var pMove) && pMove.CharacterDefinition != null)
            {
                occupant.InitializeFromCharacter(pMove.CharacterDefinition, maxHealth);
            }

            board.ForcePlace(occupant, gridPos);

            var activeRunner = runner != null ? runner : EffectsQueueRunner.Instance;
            if (activeRunner != null)
            {
                activeRunner.RegisterTileObject(id, presenter);
            }

            return occupant;
        }

        /// Instantiates and binds an EnemyOccupant to an Enemy GameObject and EnemyTileObject presenter.
        public static EnemyOccupant CreateEnemy(
            GameObject go,
            Vector2Int gridPos,
            GameBoard board,
            EnemyArchetype archetype,
            EnemyData data = null,
            EffectsQueueRunner runner = null)
        {
            if (go == null) throw new ArgumentNullException(nameof(go));
            if (board == null) throw new ArgumentNullException(nameof(board));

            StripPhysicsComponents(go);

            EnemyTileObject presenter = go.GetComponent<EnemyTileObject>();
            if (presenter == null)
            {
                presenter = go.AddComponent<EnemyTileObject>();
            }

            int id = GetNextOccupantId();

            if (data != null)
            {
                archetype = data.Archetype;
            }

            bool isBoss = data != null && data.IsBoss;
            int maxHealth = data != null ? data.MaxHealth : (isBoss ? 20 : 4);
            int attackDamage = data != null ? data.AttackDamage : (isBoss ? 0 : 1);
            int movePriority = data != null ? data.MovePriority : 0;
            int maxLineSteps = data != null ? data.MaxLineSteps : (archetype == EnemyArchetype.Pawn ? 1 : 3);
            EnemySmartness smartness = data != null ? data.Smartness : EnemySmartness.Mid;

            var occupant = new EnemyOccupant(
                archetype: archetype,
                maxHealth: maxHealth,
                attackDamage: attackDamage,
                movePriority: movePriority,
                maxLineSteps: maxLineSteps,
                initialPosition: gridPos,
                id: id,
                name: archetype.ToString(),
                intelligenceLevel: smartness,
                isBoss: isBoss
            );

            presenter.OccupantId = id;
            presenter.SnapToGrid(gridPos);

            board.ForcePlace(occupant, gridPos);

            var activeRunner = runner != null ? runner : EffectsQueueRunner.Instance;
            if (activeRunner != null)
            {
                activeRunner.RegisterTileObject(id, presenter);
            }

            return occupant;
        }

        /// Instantiates and binds a ChestOccupant to a Chest GameObject and ChestTileObject presenter.
        public static ChestOccupant CreateChest(
            GameObject go,
            Vector2Int gridPos,
            GameBoard board,
            bool isChestRoom = false,
            EffectsQueueRunner runner = null)
        {
            if (go == null) throw new ArgumentNullException(nameof(go));
            if (board == null) throw new ArgumentNullException(nameof(board));

            StripPhysicsComponents(go);

            ChestTileObject presenter = go.GetComponent<ChestTileObject>();
            if (presenter == null)
            {
                presenter = go.AddComponent<ChestTileObject>();
            }

            int id = GetNextOccupantId();
            var occupant = new ChestOccupant(
                initialPosition: gridPos,
                isChestRoom: isChestRoom,
                id: id,
                name: "Chest"
            );

            presenter.OccupantId = id;
            presenter.SnapToGrid(gridPos);

            board.ForcePlace(occupant, gridPos);

            var activeRunner = runner != null ? runner : EffectsQueueRunner.Instance;
            if (activeRunner != null)
            {
                activeRunner.RegisterTileObject(id, presenter);
            }

            return occupant;
        }

        /// Instantiates and binds a DestructiblePropOccupant to a Prop GameObject and PropTileObject presenter.
        public static DestructiblePropOccupant CreateProp(
            GameObject go,
            Vector2Int gridPos,
            GameBoard board,
            DestructiblePropType propType = DestructiblePropType.Barrel,
            int health = 1,
            EffectsQueueRunner runner = null)
        {
            if (go == null) throw new ArgumentNullException(nameof(go));
            if (board == null) throw new ArgumentNullException(nameof(board));

            StripPhysicsComponents(go);

            PropTileObject presenter = go.GetComponent<PropTileObject>();
            if (presenter == null)
            {
                presenter = go.AddComponent<PropTileObject>();
            }

            int id = GetNextOccupantId();
            var occupant = new DestructiblePropOccupant(
                propType: propType,
                maxHealth: health,
                initialPosition: gridPos,
                id: id,
                name: propType.ToString()
            );

            presenter.OccupantId = id;
            presenter.SnapToGrid(gridPos);

            board.ForcePlace(occupant, gridPos);

            var activeRunner = runner != null ? runner : EffectsQueueRunner.Instance;
            if (activeRunner != null)
            {
                activeRunner.RegisterTileObject(id, presenter);
            }

            return occupant;
        }

        /// Instantiates and binds a multi-tile Pillar to a GameObject and PillarTileObject presenter.
        /// Registers each individual occupied grid cell as a separate blocking PillarOccupant on the GameBoard
        /// while maintaining a single unified visual GameObject.
        public static List<PillarOccupant> CreatePillar(
            GameObject go,
            Vector2Int origin,
            Vector2Int size,
            GameBoard board,
            EffectsQueueRunner runner = null)
        {
            if (go == null) throw new ArgumentNullException(nameof(go));
            if (board == null) throw new ArgumentNullException(nameof(board));

            StripPhysicsComponents(go);

            PillarTileObject presenter = go.GetComponent<PillarTileObject>();
            if (presenter == null)
            {
                presenter = go.AddComponent<PillarTileObject>();
            }

            Vector2Int footprintSize = new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
            presenter.Initialize(origin, footprintSize, board);

            var activeRunner = runner != null ? runner : EffectsQueueRunner.Instance;
            presenter.ClearOccupantIds();

            var occupants = new List<PillarOccupant>();

            for (int dx = 0; dx < footprintSize.x; dx++)
            {
                for (int dy = 0; dy < footprintSize.y; dy++)
                {
                    Vector2Int cellPos = new Vector2Int(origin.x + dx, origin.y + dy);
                    int id = GetNextOccupantId();

                    var occ = new PillarOccupant(
                        gridPosition: cellPos,
                        footprintOrigin: origin,
                        footprintSize: footprintSize,
                        id: id,
                        name: $"Pillar_{footprintSize.x}x{footprintSize.y}_({cellPos.x},{cellPos.y})",
                        visualRoot: go
                    );

                    board.ForcePlace(occ, cellPos);
                    presenter.RegisterOccupantId(id);

                    if (activeRunner != null)
                    {
                        activeRunner.RegisterTileObject(id, presenter);
                    }

                    occupants.Add(occ);
                }
            }

            return occupants;
        }

        /// Instantiates and binds an ObstacleOccupant to an Obstacle GameObject (e.g. Pillar).
        public static ObstacleOccupant CreateObstacle(
            GameObject go,
            Vector2Int gridPos,
            GameBoard board,
            ObstacleType obstacleType = ObstacleType.Pillar,
            EffectsQueueRunner runner = null)
        {
            if (go == null) throw new ArgumentNullException(nameof(go));
            if (board == null) throw new ArgumentNullException(nameof(board));

            if (obstacleType == ObstacleType.Pillar)
            {
                Vector2Int pillarSize = Vector2Int.one;
                if (go.TryGetComponent<PillarTileObject>(out var pObj))
                {
                    pillarSize = pObj.Size;
                }
                var pillars = CreatePillar(go, gridPos, pillarSize, board, runner);
                return pillars.Count > 0 ? pillars[0] : null;
            }

            StripPhysicsComponents(go);

            TileObject presenter = go.GetComponent<TileObject>();
            if (presenter == null)
            {
                presenter = go.AddComponent<TileObject>();
            }

            int id = GetNextOccupantId();
            var occupant = new ObstacleOccupant(
                obstacleType: obstacleType,
                initialPosition: gridPos,
                id: id,
                name: obstacleType.ToString()
            );

            presenter.OccupantId = id;
            presenter.SnapToGrid(gridPos);

            board.ForcePlace(occupant, gridPos);

            var activeRunner = runner != null ? runner : EffectsQueueRunner.Instance;
            if (activeRunner != null)
            {
                activeRunner.RegisterTileObject(id, presenter);
            }

            return occupant;
        }

        #endregion

        #region Scene Auto-Discovery & Registration

        /// Automatically discovers and registers all active entities (player, enemies, chests, props)
        /// currently present in the Unity scene onto the authoritative GameBoard.
        public static void RegisterSceneEntities(GameBoard board, EffectsQueueRunner runner = null)
        {
            if (board == null) return;

            var activeRunner = runner != null ? runner : EffectsQueueRunner.Instance;

            // 1. Ensure Player is registered
            var playerMovement = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                Vector2Int pGrid = BoardCoordinate.WorldToGrid(playerMovement.transform.position);
                PlayerOccupant existingPlayer = board.FindPlayer();
                if (existingPlayer == null)
                {
                    var charDef = playerMovement.CharacterDefinition;
                    int maxHp = (charDef != null && charDef.HealthTierValues != null && charDef.HealthTierValues.Length > 0) ? charDef.HealthTierValues[0] : 6;
                    int atk = (charDef != null && charDef.AttackTierValues != null && charDef.AttackTierValues.Length > 0) ? charDef.AttackTierValues[0] : 2;
                    CreatePlayer(playerMovement.gameObject, pGrid, board, maxHp, atk, activeRunner);
                }
                else
                {
                    if (existingPlayer.GridPosition != pGrid && board.IsInBounds(pGrid))
                    {
                        board.Move(existingPlayer, pGrid);
                    }
                }
            }

            // 2. Discover all other objects in the active scene
            var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < allTransforms.Length; i++)
            {
                var t = allTransforms[i];
                if (t == null) continue;
                string lower = t.gameObject.name.ToLowerInvariant();

                // Skip player, cameras, managers, UI, lighting, tilemaps
                if (t.GetComponent<PlayerMovement>() != null || lower.Contains("player") ||
                    lower.Contains("manager") || lower.Contains("camera") || lower.Contains("grid") ||
                    lower.Contains("canvas") || lower.Contains("system") || lower.Contains("transition") ||
                    lower.Contains("light") || lower.Contains("tilemap") || lower.Contains("spawner") ||
                    lower.Contains("audio") || lower.Contains("event") || lower.Contains("input"))
                {
                    continue;
                }

                Vector2Int pos = BoardCoordinate.WorldToGrid(t.position);

                // Multi-Tile Pillars
                if (lower.Contains("pillar") || t.GetComponent<PillarTileObject>() != null)
                {
                    bool isAlreadyRegistered = t.TryGetComponent<PillarTileObject>(out var presenter) &&
                                               presenter.OccupantId != 0 &&
                                               board.GetOccupantById(presenter.OccupantId) != null;
                    if (!isAlreadyRegistered)
                    {
                        Vector2Int pSize = presenter != null ? presenter.Size : Vector2Int.one;
                        if (presenter == null)
                        {
                            if (lower.Contains("2x2")) pSize = new Vector2Int(2, 2);
                            else if (lower.Contains("1x2")) pSize = new Vector2Int(1, 2);
                        }
                        CreatePillar(t.gameObject, pos, pSize, board, activeRunner);
                    }
                }
                // Enemies
                else if (lower.Contains("pawn") || lower.Contains("knight") || lower.Contains("bishop") ||
                    lower.Contains("rook") || lower.Contains("queen") ||
                    lower.Contains("enemy") || t.GetComponent<EnemyBase>() != null || t.GetComponent<EnemyTileObject>() != null)
                {
                    if (lower.Contains("king"))
                    {
                        continue;
                    }

                    bool isAlreadyRegistered = t.TryGetComponent<EnemyTileObject>(out var presenter) &&
                                               presenter.OccupantId != 0 &&
                                               board.GetOccupantById(presenter.OccupantId) != null;
                    if (!isAlreadyRegistered)
                    {
                        EnemyData data = t.TryGetComponent<EnemyBase>(out var eb) ? eb.Data : null;
                        EnemyArchetype archetype = data != null ? data.Archetype : EnemyArchetype.Pawn;
                        if (data == null)
                        {
                            if (lower.Contains("knight")) archetype = EnemyArchetype.Knight;
                            else if (lower.Contains("bishop")) archetype = EnemyArchetype.Bishop;
                            else if (lower.Contains("rook")) archetype = EnemyArchetype.Rook;
                            else if (lower.Contains("queen")) archetype = EnemyArchetype.Queen;
                        }

                        CreateEnemy(t.gameObject, pos, board, archetype, data, activeRunner);
                    }
                }
                // Chests
                else if (lower.Contains("chest") || t.GetComponent<ChestTileObject>() != null)
                {
                    bool isAlreadyRegistered = t.TryGetComponent<ChestTileObject>(out var presenter) &&
                                               presenter.OccupantId != 0 &&
                                               board.GetOccupantById(presenter.OccupantId) != null;
                    if (!isAlreadyRegistered)
                    {
                        CreateChest(t.gameObject, pos, board, false, activeRunner);
                    }
                }
                // Barrels / Props
                else if (lower.Contains("barrel") || lower.Contains("crate") || lower.Contains("prop") ||
                         lower.Contains("urn") || t.GetComponent<PropTileObject>() != null)
                {
                    bool isAlreadyRegistered = t.TryGetComponent<PropTileObject>(out var presenter) &&
                                               presenter.OccupantId != 0 &&
                                               board.GetOccupantById(presenter.OccupantId) != null;
                    if (!isAlreadyRegistered)
                    {
                        CreateProp(t.gameObject, pos, board, DestructiblePropType.Barrel, 1, activeRunner);
                    }
                }
            }
        }

        #endregion
    }
}
