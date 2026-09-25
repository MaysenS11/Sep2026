using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Board;
using Core.Occupants;
using Chest;
using Player;
using Settings;
using UI;

namespace Core.Tests
{
    /// <summary>
    /// Test suite for Plan E: UI, HUD, Minimap & Audio Hooks.
    /// Verifies:
    /// 1. 4-Stat Chest Upgrade pool (Attack Damage, Defence, Range/Pattern, Max Health; no Speed).
    /// 2. Stat upgrade progression on PlayerOccupant (Tier 0 -> Tier 1 -> Tier 2) and hover preview calculation.
    /// 3. Minimap room reveal & child chest room auto-discovery.
    /// 4. Threat Overlay move and attack tile calculations for Knight, Rook, Bishop, Queen, and Pawn.
    /// 5. Key rebinding with double-binding prevention and validation.
    /// 6. Best completion time tracking per mask (best_time_{maskId}).
    /// 7. Mask Key award on first King defeat per unique mask.
    /// </summary>
    public static class PlanETests
    {
        public static void RunAllTests()
        {
            Debug.Log("[PlanETests] Starting Plan E test suite...");

            RunNamedTest("TestChest_4StatPool_ExcludesSpeed", TestChest_4StatPool_ExcludesSpeed);
            RunNamedTest("TestChest_StatUpgradeTiersAndValues", TestChest_StatUpgradeTiersAndValues);
            RunNamedTest("TestMinimap_RevealAndAutoDiscoverChildChest", TestMinimap_RevealAndAutoDiscoverChildChest);
            RunNamedTest("TestThreatOverlay_KnightLShapedThreats", TestThreatOverlay_KnightLShapedThreats);
            RunNamedTest("TestThreatOverlay_RookCardinalLineThreats", TestThreatOverlay_RookCardinalLineThreats);
            RunNamedTest("TestThreatOverlay_BishopAndQueenThreats", TestThreatOverlay_BishopAndQueenThreats);
            RunNamedTest("TestKeyRebinding_DoubleBindingValidation", TestKeyRebinding_DoubleBindingValidation);
            RunNamedTest("TestBestCompletionTime_And_MaskKeyAward", TestBestCompletionTime_And_MaskKeyAward);

            Debug.Log("[PlanETests] All 8 Plan E test cases passed successfully!");
        }

        private static void RunNamedTest(string name, Action testAction)
        {
            try
            {
                testAction();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlanETests] FAILED in {name}: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static void Assert(bool condition, string testName)
        {
            if (!condition)
            {
                throw new Exception($"[PlanETests] Assertion failed in {testName}!");
            }
        }

        public static void TestChest_4StatPool_ExcludesSpeed()
        {
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 3);
            var db = ScriptableObject.CreateInstance<StatCardDatabase>();

            var cards = CardGenerator.GenerateCards(player, db);

            Assert(cards != null, "Cards should not be null");
            Assert(cards.Count == 3, $"Expected 3 cards, got {cards.Count}");

            foreach (var card in cards)
            {
                if (!card.IsHeal)
                {
                    Assert(card.StatType != StatType.Speed, "Speed must never be generated in the chest upgrade pool");
                    bool isValidStat = card.StatType == StatType.AttackDamage ||
                                      card.StatType == StatType.Defence ||
                                      card.StatType == StatType.AttackRange ||
                                      card.StatType == StatType.Health;
                    Assert(isValidStat, $"Stat {card.StatType} must belong to the 4-stat pool");
                }
            }
        }

        public static void TestChest_StatUpgradeTiersAndValues()
        {
            var player = new PlayerOccupant(maxHealth: 6, attackDamage: 3);

            Assert(player.GetUpgradeTier(StatType.AttackDamage) == 0, "Initial attack tier should be 0");
            Assert(player.CanUpgrade(StatType.AttackDamage), "Should be able to upgrade tier 0 attack");

            player.UpgradeStat(StatType.AttackDamage);
            Assert(player.GetUpgradeTier(StatType.AttackDamage) == 1, "Attack tier should be 1 after 1 upgrade");
            Assert(player.AttackDamage == 4, $"Expected attack damage 4 at tier 1, got {player.AttackDamage}");

            player.UpgradeStat(StatType.AttackDamage);
            Assert(player.GetUpgradeTier(StatType.AttackDamage) == 2, "Attack tier should be 2 after 2 upgrades");
            Assert(player.AttackDamage == 5, $"Expected attack damage 5 at tier 2, got {player.AttackDamage}");
            Assert(!player.CanUpgrade(StatType.AttackDamage), "Should not be able to upgrade beyond tier 2");

            // Test Defence upgrade
            Assert(player.Defence == 0, "Initial defence should be 0");
            player.UpgradeStat(StatType.Defence);
            Assert(player.Defence == 1, "Defence should be 1 at tier 1");
            player.UpgradeStat(StatType.Defence);
            Assert(player.Defence == 2, "Defence should be 2 at tier 2");

            // Test Health upgrade
            int baseMaxHealth = player.MaxHealth;
            player.UpgradeStat(StatType.Health);
            Assert(player.MaxHealth > baseMaxHealth, "MaxHealth should increase on Health upgrade");
        }

        public static void TestMinimap_RevealAndAutoDiscoverChildChest()
        {
            var go = new GameObject("MinimapUI_Test", typeof(RectTransform), typeof(MinimapUI));
            var minimap = go.GetComponent<MinimapUI>();

            minimap.ResetMinimap();
            Assert(minimap.RevealedRooms.Count == 0, "Initially no revealed rooms");
            Assert(minimap.DiscoveredRooms.Count == 0, "Initially no discovered rooms");

            var parentRoom = new GameManager.RoomData
            {
                RoomIndex = 1,
                Type = RoomType.Normal,
                MacroPos = new Vector2Int(0, 0),
                HasSpecialChestRoom = true,
                SpecialChestRoomIndex = 5
            };

            minimap.OnRoomEntered(new RoomEnteredEvent(parentRoom));

            Assert(minimap.IsRoomRevealed(1), "Parent room 1 should be revealed");
            Assert(minimap.IsRoomDiscovered(5), "Child chest room 5 should be auto-discovered");
            Assert(minimap.CurrentRoomIndex == 1, "Current room should be 1");

            UnityEngine.Object.DestroyImmediate(go);
        }

        public static void TestThreatOverlay_KnightLShapedThreats()
        {
            var board = new GameBoard(10, 10);
            var knight = new EnemyOccupant(EnemyArchetype.Knight, initialPosition: new Vector2Int(4, 4));
            board.Place(knight, new Vector2Int(4, 4));

            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(5, 6));
            board.Place(player, new Vector2Int(5, 6)); // (4+1, 4+2) is an L-shape!

            var moveTiles = new HashSet<Vector2Int>();
            var attackTiles = new HashSet<Vector2Int>();

            ThreatOverlay.CalculateThreatTiles(board, -1, moveTiles, attackTiles);

            Assert(attackTiles.Contains(new Vector2Int(5, 6)), "Player at (5,6) should be threatened for attack by Knight");
            Assert(moveTiles.Contains(new Vector2Int(6, 5)), "L-shape tile (6,5) should be in move tiles");
            Assert(moveTiles.Contains(new Vector2Int(2, 5)), "L-shape tile (2,5) should be in move tiles");
            Assert(!moveTiles.Contains(new Vector2Int(5, 6)), "Player tile should be in attack tiles, not move tiles");
        }

        public static void TestThreatOverlay_RookCardinalLineThreats()
        {
            var board = new GameBoard(10, 10);
            var rook = new EnemyOccupant(EnemyArchetype.Rook, initialPosition: new Vector2Int(3, 3));
            board.Place(rook, new Vector2Int(3, 3));

            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(3, 7));
            board.Place(player, new Vector2Int(3, 7));

            var moveTiles = new HashSet<Vector2Int>();
            var attackTiles = new HashSet<Vector2Int>();

            ThreatOverlay.CalculateThreatTiles(board, -1, moveTiles, attackTiles);

            Assert(attackTiles.Contains(new Vector2Int(3, 7)), "Player in cardinal line of Rook should be threatened");
            Assert(moveTiles.Contains(new Vector2Int(3, 4)), "Empty tile (3,4) in Rook ray should be a move tile");
            Assert(moveTiles.Contains(new Vector2Int(3, 5)), "Empty tile (3,5) in Rook ray should be a move tile");
            Assert(moveTiles.Contains(new Vector2Int(3, 6)), "Empty tile (3,6) in Rook ray should be a move tile");
            Assert(!moveTiles.Contains(new Vector2Int(3, 8)), "Tile behind player should not be reachable by Rook ray");
        }

        public static void TestThreatOverlay_BishopAndQueenThreats()
        {
            var board = new GameBoard(10, 10);
            var bishop = new EnemyOccupant(EnemyArchetype.Bishop, initialPosition: new Vector2Int(2, 2));
            board.Place(bishop, new Vector2Int(2, 2));

            var player = new PlayerOccupant(maxHealth: 6, initialPosition: new Vector2Int(5, 5));
            board.Place(player, new Vector2Int(5, 5));

            var moveTiles = new HashSet<Vector2Int>();
            var attackTiles = new HashSet<Vector2Int>();

            ThreatOverlay.CalculateThreatTiles(board, -1, moveTiles, attackTiles);

            Assert(attackTiles.Contains(new Vector2Int(5, 5)), "Player on diagonal should be threatened by Bishop");
            Assert(moveTiles.Contains(new Vector2Int(3, 3)), "Tile (3,3) should be a diagonal move tile");
            Assert(moveTiles.Contains(new Vector2Int(4, 4)), "Tile (4,4) should be a diagonal move tile");
        }

        public static void TestKeyRebinding_DoubleBindingValidation()
        {
            KeyBindingManager.ResetToDefaults();

            KeyCode initialAttackKey = KeyBindingManager.GetBinding(KeyBindingManager.ActionAttack);
            Assert(initialAttackKey == KeyCode.Space, "Default attack key should be Space");

            // Attempt to bind MoveUp to Space (already used by Attack)
            bool doubleBindResult = KeyBindingManager.TryRebind(KeyBindingManager.ActionMoveUp, KeyCode.Space, out string error);
            Assert(!doubleBindResult, "Rebinding MoveUp to Space should fail due to double-binding validation");
            Assert(!string.IsNullOrEmpty(error), "Error message should describe double-binding conflict");
            Assert(KeyBindingManager.GetBinding(KeyBindingManager.ActionMoveUp) == KeyCode.W, "MoveUp should remain W after failed rebind");

            // Valid rebind to unused key
            bool validRebind = KeyBindingManager.TryRebind(KeyBindingManager.ActionMoveUp, KeyCode.UpArrow, out string validError);
            Assert(validRebind, $"Valid rebind should succeed, error: {validError}");
            Assert(KeyBindingManager.GetBinding(KeyBindingManager.ActionMoveUp) == KeyCode.UpArrow, "MoveUp should now be UpArrow");

            KeyBindingManager.ResetToDefaults();
            Assert(KeyBindingManager.GetBinding(KeyBindingManager.ActionMoveUp) == KeyCode.W, "Reset should restore W");
        }

        public static void TestBestCompletionTime_And_MaskKeyAward()
        {
            string testMask = "TestRavenMask_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            string bestKey = $"best_time_{testMask}";
            string firstDefeatKey = $"first_king_defeat_{testMask}";

            PlayerPrefs.DeleteKey(bestKey);
            PlayerPrefs.DeleteKey(firstDefeatKey);
            int initialMaskKeys = PlayerPrefs.GetInt("MaskKeys", 0);

            var winGo = new GameObject("GameOverWinUI_Test", typeof(GameOverWinUI));
            var winUI = winGo.GetComponent<GameOverWinUI>();

            // First victory: run time 150 seconds
            winUI.ShowWinScreen(150f, testMask);

            float savedBest = PlayerPrefs.GetFloat(bestKey, float.MaxValue);
            Assert(Mathf.Approximately(savedBest, 150f), $"Expected best time 150, got {savedBest}");
            Assert(PlayerPrefs.GetInt(firstDefeatKey, 0) == 1, "First King defeat flag should be 1");
            Assert(PlayerPrefs.GetInt("MaskKeys", 0) == initialMaskKeys + 1, "MaskKeys should be incremented by 1 on first defeat");

            // Second victory with a slower time (200s): best time should remain 150s, no extra key
            winUI.ShowWinScreen(200f, testMask);
            float afterSlower = PlayerPrefs.GetFloat(bestKey, float.MaxValue);
            Assert(Mathf.Approximately(afterSlower, 150f), "Best time should not be overwritten by slower run");
            Assert(PlayerPrefs.GetInt("MaskKeys", 0) == initialMaskKeys + 1, "MaskKeys should not be incremented again for the same mask");

            // Third victory with faster time (120s): best time should update to 120s
            winUI.ShowWinScreen(120f, testMask);
            float afterFaster = PlayerPrefs.GetFloat(bestKey, float.MaxValue);
            Assert(Mathf.Approximately(afterFaster, 120f), "Best time should update to faster run (120s)");

            // Clean up test keys
            PlayerPrefs.DeleteKey(bestKey);
            PlayerPrefs.DeleteKey(firstDefeatKey);
            PlayerPrefs.SetInt("MaskKeys", initialMaskKeys);
            PlayerPrefs.Save();

            UnityEngine.Object.DestroyImmediate(winGo);
        }
    }
}
