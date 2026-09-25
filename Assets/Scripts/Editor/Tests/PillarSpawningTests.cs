using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Core.Board;
using Core.Occupants;
using Dungeon;
using Dungeon.Spawning;
using Infrastructure;

namespace Dungeon.EditorTests
{
    [TestFixture]
    public class PillarSpawningTests
    {
        [Test]
        public void Test_PillarSpawnData_DefaultsAndOverrides()
        {
            var data = ScriptableObject.CreateInstance<PillarSpawnData>();
            Assert.IsFalse(data.AllowInOrNearHallways);
            Assert.AreEqual(1, data.MinDistanceToWalls);
            Assert.AreEqual(2, data.MinDistanceToDoors);

            data.SetAllowInOrNearHallways(true);
            data.SetMinDistanceToWalls(3);
            data.SetMinDistanceToDoors(4);

            Assert.IsTrue(data.AllowInOrNearHallways);
            Assert.AreEqual(3, data.MinDistanceToWalls);
            Assert.AreEqual(4, data.MinDistanceToDoors);

            data.SetMinDistanceToWalls(-5);
            Assert.AreEqual(0, data.MinDistanceToWalls);
        }

        [Test]
        public void Test_RoomTileQuery_IsHallwayTile_TShape()
        {
            var room = new GameManager.RoomData
            {
                Shape = RoomShape.TShape,
                Size = new Vector2Int(15, 12),
                WorldOriginTile = Vector2Int.zero
            };

            int halfHeight = room.Size.y / 2;
            int stemCenterX = room.Size.x / 2;

            Assert.IsFalse(RoomTileQuery.IsHallwayTile(room, stemCenterX, 2));
            Assert.IsTrue(RoomTileQuery.IsHallwayTile(room, stemCenterX, halfHeight + 1));
            Assert.IsTrue(RoomTileQuery.IsNearHallwayTile(room, stemCenterX, halfHeight - 1, 1));
        }

        [Test]
        public void Test_RoomTileQuery_IsHallwayTile_UShape()
        {
            var room = new GameManager.RoomData
            {
                Shape = RoomShape.UShape,
                Size = new Vector2Int(16, 14),
                WorldOriginTile = Vector2Int.zero
            };

            Assert.IsFalse(RoomTileQuery.IsHallwayTile(room, 8, 2));
            Assert.IsTrue(RoomTileQuery.IsHallwayTile(room, 2, 7));
            Assert.IsTrue(RoomTileQuery.IsHallwayTile(room, 13, 7));
            Assert.IsTrue(RoomTileQuery.IsNearHallwayTile(room, 5, 7, 1));
        }

        [Test]
        public void Test_RoomTileQuery_IsDistanceValidFromWalls()
        {
            var room = new GameManager.RoomData
            {
                Shape = RoomShape.Rectangle,
                Size = new Vector2Int(10, 10),
                WorldOriginTile = Vector2Int.zero
            };

            Assert.IsFalse(RoomTileQuery.IsDistanceValidFromWalls(room, new Vector2Int(1, 1), Vector2Int.one, 1));
            Assert.IsTrue(RoomTileQuery.IsDistanceValidFromWalls(room, new Vector2Int(2, 2), Vector2Int.one, 1));
            Assert.IsFalse(RoomTileQuery.IsDistanceValidFromWalls(room, new Vector2Int(2, 2), new Vector2Int(2, 2), 2));
            Assert.IsTrue(RoomTileQuery.IsDistanceValidFromWalls(room, new Vector2Int(3, 3), new Vector2Int(2, 2), 2));
        }

        [Test]
        public void Test_PillarOccupant_InheritsFromObstacle()
        {
            var pillar = new PillarOccupant(
                gridPosition: new Vector2Int(3, 4),
                footprintOrigin: new Vector2Int(3, 4),
                footprintSize: new Vector2Int(2, 2),
                id: 42
            );

            Assert.IsTrue(pillar is ObstacleOccupant);
            Assert.AreEqual(ObstacleType.Pillar, pillar.ObstacleType);
            Assert.IsFalse(pillar.IsPushable);
            Assert.IsFalse(pillar.IsDead);

            var effects = pillar.TakeDamage(10);
            Assert.AreEqual(0, effects.Count);
        }

        [Test]
        public void Test_MultiTile_PillarOccupants_RegisteredOnBoard()
        {
            var board = new GameBoard(10, 10, Vector2Int.zero);
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    board.SetCell(new Vector2Int(x, y), TerrainType.Floor);
                }
            }

            var go = new GameObject("Pillar_2x2");
            var origin = new Vector2Int(2, 2);
            var size = new Vector2Int(2, 2);

            var occupants = BoardEntityFactory.CreatePillar(go, origin, size, board);

            Assert.AreEqual(4, occupants.Count);
            for (int x = 2; x <= 3; x++)
            {
                for (int y = 2; y <= 3; y++)
                {
                    var occ = board.GetOccupant(new Vector2Int(x, y));
                    Assert.IsNotNull(occ);
                    Assert.IsTrue(occ is PillarOccupant);
                    var pillar = (PillarOccupant)occ;
                    Assert.AreEqual(origin, pillar.FootprintOrigin);
                    Assert.AreEqual(size, pillar.FootprintSize);
                }
            }

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_PillarConfig_MultiPillar_Configuration()
        {
            var data = ScriptableObject.CreateInstance<PillarSpawnData>();
            var p1 = new GameObject("P1");
            var p2 = new GameObject("P2");

            data.AddPillar(new PillarConfig(p1, new Vector2Int(2, 2), false));
            data.AddPillar(new PillarConfig(p2, new Vector2Int(3, 4), true));

            Assert.AreEqual(2, data.Pillars.Count);
            Assert.AreEqual(new Vector2Int(2, 2), data.Pillars[0].Size);
            Assert.IsFalse(data.Pillars[0].AllowInOrNearHallways);
            Assert.AreEqual(new Vector2Int(3, 4), data.Pillars[1].Size);
            Assert.IsTrue(data.Pillars[1].AllowInOrNearHallways);

            var list = data.GetConfiguredPillars();
            Assert.AreEqual(2, list.Count);

            Object.DestroyImmediate(p1);
            Object.DestroyImmediate(p2);
        }

        [Test]
        public void Test_PillarSpawner_FallbackWhenOversized()
        {
            var board = new GameBoard(10, 10, Vector2Int.zero);
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    board.SetCell(new Vector2Int(x, y), TerrainType.Floor);
                }
            }

            var goOversized = new GameObject("Pillar_8x8");
            var goSmall = new GameObject("Pillar_2x2");

            var data = ScriptableObject.CreateInstance<PillarSpawnData>();
            data.AddPillar(new PillarConfig(goOversized, new Vector2Int(8, 8), false));
            data.AddPillar(new PillarConfig(goSmall, new Vector2Int(2, 2), false));
            data.SetMinDistanceToWalls(1);
            data.SetDensity(1f);

            var spawner = new PropSpawner();
            spawner.PillarSpawnData = data;
            spawner.PropDensity = 1f;

            var room = new GameManager.RoomData
            {
                Shape = RoomShape.Rectangle,
                Size = new Vector2Int(6, 6),
                WorldOriginTile = Vector2Int.zero,
                Type = RoomType.Normal
            };

            var tileQuery = new RoomTileQuery();
            var floorGo = new GameObject("FloorMap");
            var tilemap = floorGo.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            var parent = new GameObject("PropsContainer").transform;

            spawner.SpawnContent(room, tileQuery, tilemap, parent);

            Assert.IsTrue(parent.childCount > 0);
            for (int i = 0; i < parent.childCount; i++)
            {
                Assert.IsTrue(parent.GetChild(i).name.Contains("Pillar_2x2"));
            }

            spawner.ClearSpawnedContent();
            Object.DestroyImmediate(goOversized);
            Object.DestroyImmediate(goSmall);
            Object.DestroyImmediate(floorGo);
            Object.DestroyImmediate(parent.gameObject);
        }
    }
}
