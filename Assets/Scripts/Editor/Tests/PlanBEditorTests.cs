using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Core.Board;
using Core.Effects;
using Core.Occupants;
using Infrastructure;

namespace Core.EditorTests
{
    [TestFixture]
    public class PlanBEditorTests
    {
        [Test]
        public void Test_PlanB_AllSimulationTests_Pass()
        {
            Core.Tests.PlanBTests.RunAllTests();
            Assert.Pass();
        }

        [Test]
        public void Test_PlanB_Degen_StopsAtFirstOccupant()
        {
            var board = new GameBoard(10, 10);
            var coordinator = new BoardTurnCoordinator(board);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 2, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));
            player.SetFacingDirection(BoardCoordinate.East);

            var charDef = ScriptableObject.CreateInstance<CharacterDefinition>();
            charDef.SetWeaponType(WeaponType.Degen);
            player.InitializeFromCharacter(charDef, 6);

            var pawn1 = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(3, 2));
            var pawn2 = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 4, initialPosition: new Vector2Int(4, 2));
            board.Place(pawn1, new Vector2Int(3, 2));
            board.Place(pawn2, new Vector2Int(4, 2));

            var targetTiles = new List<Vector2Int> { new Vector2Int(3, 2), new Vector2Int(4, 2) };
            coordinator.SimPlayerAttack(targetTiles, out var effects);

            Assert.AreEqual(2, pawn1.CurrentHealth);
            Assert.AreEqual(4, pawn2.CurrentHealth);
        }

        [Test]
        public void Test_PlanB_Pistol_PiercingWithDamageDegradation()
        {
            var board = new GameBoard(10, 10);
            var coordinator = new BoardTurnCoordinator(board);
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 3, initialPosition: new Vector2Int(2, 2));
            board.Place(player, new Vector2Int(2, 2));
            player.SetFacingDirection(BoardCoordinate.East);

            var charDef = ScriptableObject.CreateInstance<CharacterDefinition>();
            charDef.SetWeaponType(WeaponType.Pistol);
            player.InitializeFromCharacter(charDef, 6);

            var p1 = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 6, initialPosition: new Vector2Int(3, 2));
            var p2 = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 6, initialPosition: new Vector2Int(4, 2));
            var p3 = new EnemyOccupant(EnemyArchetype.Pawn, maxHealth: 6, initialPosition: new Vector2Int(5, 2));
            board.Place(p1, new Vector2Int(3, 2));
            board.Place(p2, new Vector2Int(4, 2));
            board.Place(p3, new Vector2Int(5, 2));

            var targetTiles = new List<Vector2Int> { new Vector2Int(3, 2), new Vector2Int(4, 2), new Vector2Int(5, 2) };
            coordinator.SimPlayerAttack(targetTiles, out var effects);

            Assert.AreEqual(3, p1.CurrentHealth); // 6 - 3
            Assert.AreEqual(4, p2.CurrentHealth); // 6 - 2
            Assert.AreEqual(5, p3.CurrentHealth); // 6 - 1
        }
    }

    internal static class CoordExt
    {
        public static bool SimPlayerAttack(this BoardTurnCoordinator coordinator, List<Vector2Int> targets, out List<BoardEffect> effects)
        {
            return coordinator.SimulatePlayerAttack(targets, out effects);
        }
    }
}
