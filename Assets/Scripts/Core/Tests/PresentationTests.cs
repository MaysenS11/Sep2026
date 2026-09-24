using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Effects;
using Core.Scheduling;
using Presentation.Board;
using Presentation.Entities;

namespace Core.Tests
{
    /// Verification test suite for Phase 5: Presentation Layer.
    /// Tests coordinate conversions, snapping, presenter components, registry, and effect dispatching.
    public static class PresentationTests
    {
        public static void RunAllTests()
        {
            Debug.Log("[PresentationTests] Starting Phase 5 Presentation test suite...");

            RunNamedTest("TestCoordinateConversionsAndSnapping", TestCoordinateConversionsAndSnapping);
            RunNamedTest("TestTileObjectHasNoPhysicsComponents", TestTileObjectHasNoPhysicsComponents);
            RunNamedTest("TestEnemyTileObjectSpriteFlipping", TestEnemyTileObjectSpriteFlipping);
            RunNamedTest("TestPlayerTileObjectFacingDirection", TestPlayerTileObjectFacingDirection);
            RunNamedTest("TestChestTileObjectInitialState", TestChestTileObjectInitialState);
            RunNamedTest("TestPropTileObjectProperties", TestPropTileObjectProperties);
            RunNamedTest("TestEffectsQueueRunnerRegistry", TestEffectsQueueRunnerRegistry);
            RunNamedTest("TestEffectsQueueRunnerNullAndEmptySafety", TestEffectsQueueRunnerNullAndEmptySafety);
            RunNamedTest("TestAnimationRoutinesReturnValidEnumerators", TestAnimationRoutinesReturnValidEnumerators);

            Debug.Log("[PresentationTests] All 9 Phase 5 test cases passed successfully!");
        }

        private static void RunNamedTest(string name, Action testAction)
        {
            try
            {
                testAction();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PresentationTests] FAILED in {name}: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static void Assert(bool condition, string testName)
        {
            if (!condition)
            {
                throw new Exception($"[PresentationTests] Assertion failed in {testName}!");
            }
        }

        private static void TestCoordinateConversionsAndSnapping()
        {
            Vector2Int gridPos = new Vector2Int(4, -2);
            Vector3 worldCenter = TileObject.GridToWorld(gridPos, 0f);
            Assert(Mathf.Approximately(worldCenter.x, 4.5f) && Mathf.Approximately(worldCenter.y, -1.5f), "GridToWorld center calculation");

            Vector2Int backToGrid = TileObject.WorldToGrid(new Vector3(4.9f, -1.1f, 0f));
            Assert(backToGrid == gridPos, "WorldToGrid floor conversion");

            GameObject go = new GameObject("TestTileObject");
            try
            {
                TileObject tileObj = go.AddComponent<TileObject>();
                tileObj.SnapToGrid(new Vector2Int(2, 3));

                Assert(tileObj.CurrentGridPosition == new Vector2Int(2, 3), "SnapToGrid updates CurrentGridPosition");
                Assert(Mathf.Approximately(go.transform.position.x, 2.5f) && Mathf.Approximately(go.transform.position.y, 3.5f), "SnapToGrid sets exact world center");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void TestTileObjectHasNoPhysicsComponents()
        {
            GameObject go = new GameObject("TestTileObject");
            try
            {
                TileObject tileObj = go.AddComponent<TileObject>();
                Assert(go.GetComponent<Rigidbody2D>() == null, "TileObject must have no Rigidbody2D");
                Assert(go.GetComponent<Collider2D>() == null, "TileObject must have no Collider2D");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void TestEnemyTileObjectSpriteFlipping()
        {
            GameObject go = new GameObject("TestEnemyTileObject");
            try
            {
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                EnemyTileObject enemy = go.AddComponent<EnemyTileObject>();

                enemy.UpdateFacingDirection(Vector2.right);
                Assert(sr.flipX == false, "Enemy facing right should not flipX");

                enemy.UpdateFacingDirection(Vector2.left);
                Assert(sr.flipX == true, "Enemy facing left should flipX");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void TestPlayerTileObjectFacingDirection()
        {
            GameObject go = new GameObject("TestPlayerTileObject");
            try
            {
                PlayerTileObject player = go.AddComponent<PlayerTileObject>();
                player.UpdateFacingDirection(Vector2.up);
                Assert(player.FacingDirection == Vector2.up, "Player facing direction updated to up");

                player.UpdateFacingDirection(new Vector2(-1, 0));
                Assert(player.FacingDirection == Vector2.left, "Player facing direction updated to left");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void TestChestTileObjectInitialState()
        {
            GameObject go = new GameObject("TestChestTileObject");
            try
            {
                ChestTileObject chest = go.AddComponent<ChestTileObject>();
                Assert(!chest.IsOpen, "Chest initial open state is false");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void TestPropTileObjectProperties()
        {
            GameObject go = new GameObject("TestPropTileObject");
            try
            {
                PropTileObject prop = go.AddComponent<PropTileObject>();
                Assert(prop != null, "PropTileObject successfully instantiated");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static void TestEffectsQueueRunnerRegistry()
        {
            GameObject runnerGo = new GameObject("TestRunner");
            GameObject obj1Go = new GameObject("TestObj1");
            GameObject obj2Go = new GameObject("TestObj2");

            try
            {
                EffectsQueueRunner runner = runnerGo.AddComponent<EffectsQueueRunner>();
                TileObject obj1 = obj1Go.AddComponent<TileObject>();
                TileObject obj2 = obj2Go.AddComponent<TileObject>();

                runner.RegisterTileObject(101, obj1);
                runner.RegisterTileObject(102, obj2);

                Assert(runner.RegisteredCount == 2, "Registered count is 2");
                Assert(runner.GetTileObject(101) == obj1, "GetTileObject(101) returns obj1");
                Assert(runner.GetTileObject(102) == obj2, "GetTileObject(102) returns obj2");

                runner.UnregisterTileObject(101);
                Assert(runner.RegisteredCount == 1, "Registered count is 1 after unregister");
                Assert(runner.GetTileObject(101) == null, "GetTileObject(101) is null after unregister");

                runner.ClearRegistry();
                Assert(runner.RegisteredCount == 0, "Registered count is 0 after ClearRegistry");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runnerGo);
                UnityEngine.Object.DestroyImmediate(obj1Go);
                UnityEngine.Object.DestroyImmediate(obj2Go);
            }
        }

        private static void TestEffectsQueueRunnerNullAndEmptySafety()
        {
            GameObject runnerGo = new GameObject("TestRunner");
            try
            {
                EffectsQueueRunner runner = runnerGo.AddComponent<EffectsQueueRunner>();

                IEnumerator batchesRoutine = runner.PlayTurnBatches(null);
                Assert(!batchesRoutine.MoveNext(), "PlayTurnBatches(null) completes immediately");

                IEnumerator emptyBatchesRoutine = runner.PlayTurnBatches(new List<AnimationBatch>());
                Assert(!emptyBatchesRoutine.MoveNext(), "PlayTurnBatches(empty) completes immediately");

                IEnumerator batchRoutine = runner.PlayBatch(null);
                Assert(!batchRoutine.MoveNext(), "PlayBatch(null) completes immediately");

                IEnumerator effectRoutine = runner.PlaySingleEffect(null, 0.5f);
                Assert(!effectRoutine.MoveNext(), "PlaySingleEffect(null) completes immediately");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runnerGo);
            }
        }

        private static void TestAnimationRoutinesReturnValidEnumerators()
        {
            GameObject go = new GameObject("TestTileObject");
            try
            {
                TileObject tileObj = go.AddComponent<TileObject>();

                Assert(tileObj.AnimateMoveRoutine(new[] { Vector2Int.zero, Vector2Int.up }, 0.5f) != null, "AnimateMoveRoutine returns valid enumerator");
                Assert(tileObj.AnimateJumpRoutine(Vector2Int.zero, new Vector2Int(1, 2), 0.5f) != null, "AnimateJumpRoutine returns valid enumerator");
                Assert(tileObj.AnimateAttackLungeRoutine(Vector2Int.up, 0.5f) != null, "AnimateAttackLungeRoutine returns valid enumerator");
                Assert(tileObj.AnimateBlockedRecoilRoutine(Vector2Int.up, Vector2Int.zero, 0.5f) != null, "AnimateBlockedRecoilRoutine returns valid enumerator");
                Assert(tileObj.AnimateDamageRoutine(1) != null, "AnimateDamageRoutine returns valid enumerator");
                Assert(tileObj.AnimateDeathRoutine() != null, "AnimateDeathRoutine returns valid enumerator");
                Assert(tileObj.AnimateChestOpenRoutine("Gold") != null, "AnimateChestOpenRoutine returns valid enumerator");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
