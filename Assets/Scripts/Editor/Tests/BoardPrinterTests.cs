using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Core.Board;
using Core.Occupants;

namespace Core.EditorTests
{
    [TestFixture]
    public class BoardPrinterTests
    {
        [Test]
        public void Test_BoardPrinter_RendersMapCharactersCorrectly()
        {
            var board = new GameBoard(5, 5, new Vector2Int(0, 0));
            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    board.SetWall(new Vector2Int(x, y), true);
                }
            }

            board.SetCell(new Vector2Int(1, 1), TerrainType.Floor);
            board.SetCell(new Vector2Int(2, 1), TerrainType.Floor);
            board.SetCell(new Vector2Int(3, 1), TerrainType.Floor);
            board.SetCell(new Vector2Int(1, 2), TerrainType.Floor);
            board.SetCell(new Vector2Int(2, 2), TerrainType.Floor);
            board.SetCell(new Vector2Int(3, 2), TerrainType.Floor);
            board.SetCell(new Vector2Int(1, 3), TerrainType.Floor);
            board.SetCell(new Vector2Int(2, 3), TerrainType.Floor);
            board.SetCell(new Vector2Int(3, 3), TerrainType.Floor);

            var player = new PlayerOccupant(6, 2, new Vector2Int(1, 1), 10, "TestPlayer");
            board.ForcePlace(player, new Vector2Int(1, 1));

            var enemy = new EnemyOccupant(EnemyArchetype.Knight, 4, 1, 0, 3, new Vector2Int(2, 1), 11, "TestEnemy");
            board.ForcePlace(enemy, new Vector2Int(2, 1));

            var barrel = new DestructiblePropOccupant(DestructiblePropType.Barrel, 1, new Vector2Int(3, 1), 12, "TestBarrel");
            board.ForcePlace(barrel, new Vector2Int(3, 1));

            var chest = new ChestOccupant(new Vector2Int(1, 2), false, 13, "TestChest");
            board.ForcePlace(chest, new Vector2Int(1, 2));

            var pillar = new PillarOccupant(new Vector2Int(2, 2), new Vector2Int(2, 2), new Vector2Int(1, 1), 14, "TestPillar");
            board.ForcePlace(pillar, new Vector2Int(2, 2));

            var doors = new Dictionary<Vector2Int, DoorInfo>
            {
                { new Vector2Int(3, 2), new DoorInfo(new Vector2Int(3, 2), "D_in", 0) },
                { new Vector2Int(1, 3), new DoorInfo(new Vector2Int(1, 3), "D_out", 0) },
                { new Vector2Int(2, 3), new DoorInfo(new Vector2Int(2, 3), "D_spec", 1) }
            };

            Assert.AreEqual('P', BoardPrinter.GetCellCharacter(board, new Vector2Int(1, 1), doors));
            Assert.AreEqual('E', BoardPrinter.GetCellCharacter(board, new Vector2Int(2, 1), doors));
            Assert.AreEqual('B', BoardPrinter.GetCellCharacter(board, new Vector2Int(3, 1), doors));
            Assert.AreEqual('C', BoardPrinter.GetCellCharacter(board, new Vector2Int(1, 2), doors));
            Assert.AreEqual('\u220F', BoardPrinter.GetCellCharacter(board, new Vector2Int(2, 2), doors));
            Assert.AreEqual('D', BoardPrinter.GetCellCharacter(board, new Vector2Int(3, 2), doors));
            Assert.AreEqual('D', BoardPrinter.GetCellCharacter(board, new Vector2Int(1, 3), doors));
            Assert.AreEqual('D', BoardPrinter.GetCellCharacter(board, new Vector2Int(2, 3), doors));
            Assert.AreEqual('.', BoardPrinter.GetCellCharacter(board, new Vector2Int(3, 3), doors));
            Assert.AreEqual('#', BoardPrinter.GetCellCharacter(board, new Vector2Int(0, 0), doors));

            string text = BoardPrinter.PrintToString(board, fullBoard: true, customDoors: doors);
            StringAssert.Contains("P", text);
            StringAssert.Contains("E", text);
            StringAssert.Contains("B", text);
            StringAssert.Contains("C", text);
            StringAssert.Contains("\u220F", text);
            StringAssert.Contains("D", text);
            StringAssert.Contains("D_in", text);
            StringAssert.Contains("D_out", text);
            StringAssert.Contains("D_spec", text);
            StringAssert.Contains("[ID:  10] PLAYER", text);
            StringAssert.Contains("[ID:  11] ENEMY", text);
            StringAssert.Contains("[ID:  12] PROP", text);
            StringAssert.Contains("[ID:  13] CHEST", text);
        }

        [Test]
        public void Test_BoardPrinter_MultiTilePillar_BoundaryVerification()
        {
            var board = new GameBoard(10, 10, Vector2Int.zero);
            Vector2Int origin = new Vector2Int(3, 3);
            Vector2Int size = new Vector2Int(2, 2);

            int baseId = 100;
            for (int dx = 0; dx < size.x; dx++)
            {
                for (int dy = 0; dy < size.y; dy++)
                {
                    Vector2Int pos = new Vector2Int(origin.x + dx, origin.y + dy);
                    var pillar = new PillarOccupant(pos, origin, size, baseId++, $"Pillar_{size.x}x{size.y}");
                    board.ForcePlace(pillar, pos);
                }
            }

            string text = BoardPrinter.PrintToString(board, fullBoard: true);
            StringAssert.Contains("Pillar Footprint: Origin=(3,3) to Max=(4,4) | Dim: 2x2 (4 cells registered)", text);
            StringAssert.Contains("[ID:100 at (3,3) offset (0,0)]", text);
            StringAssert.Contains("[ID:101 at (3,4) offset (0,1)]", text);
            StringAssert.Contains("[ID:102 at (4,3) offset (1,0)]", text);
            StringAssert.Contains("[ID:103 at (4,4) offset (1,1)]", text);
        }

        [Test]
        public void Test_BoardPrinter_DumpToFile_WritesSuccessfully()
        {
            var board = new GameBoard(4, 4, Vector2Int.zero);
            board.SetCell(new Vector2Int(1, 1), TerrainType.Floor);
            var player = new PlayerOccupant(6, 2, new Vector2Int(1, 1), 1, "Player");
            board.ForcePlace(player, new Vector2Int(1, 1));

            string tempFile = Path.Combine(Application.temporaryCachePath, "Test_BoardPrinter_Dump.txt");
            if (File.Exists(tempFile)) File.Delete(tempFile);

            BoardPrinter.DumpToFile(tempFile, board, fullBoard: true);

            Assert.IsTrue(File.Exists(tempFile));
            string content = File.ReadAllText(tempFile);
            StringAssert.Contains("=== GAMEBOARD ASCII DUMP ===", content);
            StringAssert.Contains("P", content);
            StringAssert.Contains("[ID:   1] PLAYER", content);

            File.Delete(tempFile);
        }
    }
}
