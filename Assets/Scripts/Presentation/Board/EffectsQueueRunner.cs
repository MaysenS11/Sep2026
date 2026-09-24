using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Effects;
using Core.Scheduling;
using Presentation.Entities;

namespace Presentation.Board
{
    /// Presentation layer manager executing sequential playback of animation batches.
    /// Maintains an active registry of TileObject presenters mapped by OccupantId.
    /// Drives concurrent effect dispatching across allocated time slices.
    public class EffectsQueueRunner : MonoBehaviour
    {
        public static EffectsQueueRunner Instance { get; private set; }

        private readonly Dictionary<int, TileObject> _registry = new Dictionary<int, TileObject>();

        public int RegisteredCount => _registry.Count;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            _registry.Clear();
        }

        #region TileObject Registry

        /// Registers a visual TileObject associated with a specific simulation Occupant ID.
        public void RegisterTileObject(int occupantId, TileObject tileObj)
        {
            if (tileObj == null) return;
            tileObj.OccupantId = occupantId;
            _registry[occupantId] = tileObj;
        }

        /// Unregisters a TileObject by its simulation Occupant ID.
        public void UnregisterTileObject(int occupantId)
        {
            if (_registry.ContainsKey(occupantId))
            {
                _registry.Remove(occupantId);
            }
        }

        /// Retrieves the registered visual TileObject for a given simulation Occupant ID.
        public TileObject GetTileObject(int occupantId)
        {
            _registry.TryGetValue(occupantId, out var tileObj);
            return tileObj;
        }

        /// Clears all registered presenters.
        public void ClearRegistry()
        {
            _registry.Clear();
        }

        #endregion

        #region Batch & Turn Execution

        /// Sequentially iterates through a list of AnimationBatches, yielding on each batch until fully played.
        public IEnumerator PlayTurnBatches(List<AnimationBatch> batches)
        {
            if (batches == null || batches.Count == 0)
            {
                yield break;
            }

            for (int i = 0; i < batches.Count; i++)
            {
                if (batches[i] != null)
                {
                    yield return StartCoroutine(PlayBatch(batches[i]));
                }
            }
        }

        /// Launches animations for all effects in the batch concurrently,
        /// waiting until the batch duration elapses and all batch coroutines complete.
        public IEnumerator PlayBatch(AnimationBatch batch)
        {
            if (batch == null)
            {
                yield break;
            }

            float duration = Mathf.Max(0.01f, batch.Duration);
            var effects = batch.AllEffects;

            if (effects == null || effects.Count == 0)
            {
                if (duration > 0f)
                {
                    yield return new WaitForSeconds(duration);
                }
                yield break;
            }

            // Group effects by occupant ID so that for any single occupant, effects execute sequentially
            // (e.g. Move/Recoil -> Damage -> Death), while distinct occupants animate concurrently.
            var effectsByOccupant = new Dictionary<int, List<BoardEffect>>();
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null) continue;

                if (!effectsByOccupant.TryGetValue(effect.OccupantId, out var list))
                {
                    list = new List<BoardEffect>();
                    effectsByOccupant[effect.OccupantId] = list;
                }
                list.Add(effect);
            }

            float startTime = Time.time;
            int activeCoroutines = 0;

            foreach (var kvp in effectsByOccupant)
            {
                var occupantEffects = kvp.Value;
                activeCoroutines++;
                StartCoroutine(RunOccupantEffectsSequential(occupantEffects, duration, () => activeCoroutines--));
            }

            while (activeCoroutines > 0)
            {
                yield return null;
            }

            float elapsed = Time.time - startTime;
            if (elapsed < duration)
            {
                yield return new WaitForSeconds(duration - elapsed);
            }
        }

        private IEnumerator RunOccupantEffectsSequential(List<BoardEffect> occupantEffects, float totalDuration, Action onComplete)
        {
            try
            {
                if (occupantEffects == null || occupantEffects.Count == 0) yield break;

                // Sort: Movement/Recoil first (0), Damage second (1), Chest opened (2), Death last (3)
                occupantEffects.Sort((a, b) => GetEffectPriority(a).CompareTo(GetEffectPriority(b)));

                float perEffectDuration = occupantEffects.Count > 1
                    ? totalDuration / occupantEffects.Count
                    : totalDuration;

                for (int i = 0; i < occupantEffects.Count; i++)
                {
                    var effect = occupantEffects[i];
                    if (effect == null) continue;

                    TileObject targetObj = GetTileObject(effect.OccupantId);
                    if (targetObj == null) break;

                    yield return StartCoroutine(PlaySingleEffect(effect, perEffectDuration));
                }
            }
            finally
            {
                onComplete?.Invoke();
            }
        }

        private static int GetEffectPriority(BoardEffect effect)
        {
            switch (effect)
            {
                case MoveEffect _:
                case JumpEffect _:
                case PushDisplacementEffect _:
                case BlockedPushRecoilEffect _:
                case AttackLungeEffect _:
                    return 0; // Movement / displacement / recoil first
                case DamageTakenEffect _:
                    return 1; // Damage feedback second
                case ChestOpenedEffect _:
                    return 2; // Chest opened
                case OccupantDestroyedEffect _:
                    return 3; // Death & destruction strictly last
                default:
                    return 0;
            }
        }

        private IEnumerator RunEffectWithCounter(BoardEffect effect, float duration, Action onComplete)
        {
            try
            {
                yield return StartCoroutine(PlaySingleEffect(effect, duration));
            }
            finally
            {
                onComplete?.Invoke();
            }
        }

        /// Dispatches a single BoardEffect to its corresponding target TileObject presenter.
        public IEnumerator PlaySingleEffect(BoardEffect effect, float duration)
        {
            if (effect == null)
            {
                yield break;
            }

            TileObject targetObj = GetTileObject(effect.OccupantId);

            switch (effect)
            {
                case MoveEffect move:
                    if (targetObj != null)
                    {
                        yield return StartCoroutine(targetObj.AnimateMoveRoutine(move.Path, duration));
                    }
                    break;

                case JumpEffect jump:
                    if (targetObj != null)
                    {
                        yield return StartCoroutine(targetObj.AnimateJumpRoutine(jump.StartPos, jump.EndPos, duration, jump.ArcHeight));
                    }
                    break;

                case AttackLungeEffect attack:
                    if (targetObj != null)
                    {
                        yield return StartCoroutine(targetObj.AnimateAttackLungeRoutine(attack.TargetPos, duration));
                    }
                    break;

                case PushDisplacementEffect push:
                    if (targetObj != null)
                    {
                        var path = new[] { push.StartPos, push.EndPos };
                        yield return StartCoroutine(targetObj.AnimateMoveRoutine(path, duration));
                    }
                    break;

                case BlockedPushRecoilEffect recoil:
                    if (targetObj != null)
                    {
                        yield return StartCoroutine(targetObj.AnimateBlockedRecoilRoutine(recoil.PlayerTile, recoil.RecoilEndTile, duration));
                    }
                    break;

                case DamageTakenEffect damage:
                    if (targetObj != null)
                    {
                        yield return StartCoroutine(targetObj.AnimateDamageRoutine(damage.DamageAmount));
                    }
                    break;

                case OccupantDestroyedEffect destroyed:
                    if (targetObj != null)
                    {
                        yield return StartCoroutine(targetObj.AnimateDeathRoutine());
                        UnregisterTileObject(destroyed.OccupantId);
                    }
                    break;

                case ChestOpenedEffect chest:
                    TileObject chestObj = GetTileObject(chest.ChestOccupantId);
                    if (chestObj != null)
                    {
                        yield return StartCoroutine(chestObj.AnimateChestOpenRoutine(chest.LootInfo));
                    }
                    break;

                default:
                    yield break;
            }
        }

        #endregion
    }
}
