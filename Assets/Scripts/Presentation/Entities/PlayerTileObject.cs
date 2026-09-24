using System.Collections;
using UnityEngine;

namespace Presentation.Entities
{
    /// Presenter view for the Player on the game board.
    /// Manages 4-directional facing vectors (MoveX/MoveY), attack triggers, and damage feedback.
    public class PlayerTileObject : TileObject
    {
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
        private static readonly int TakeDamageHash = Animator.StringToHash("TakeDamage");
        private static readonly int DieHash = Animator.StringToHash("Die");

        [Header("Player Facing Direction")]
        [SerializeField] private Vector2 facingDirection = Vector2.down;

        public Vector2 FacingDirection => facingDirection;

        /// Updates directional Animator parameters (MoveX, MoveY) and sprite horizontal flip.
        public virtual void UpdateFacingDirection(Vector2 direction)
        {
            EnsureComponents();
            if (direction.sqrMagnitude < 0.001f) return;

            direction.Normalize();
            facingDirection = direction;

            if (animator != null)
            {
                if (HasParameter(animator, MoveXHash))
                {
                    animator.SetFloat(MoveXHash, direction.x);
                }
                if (HasParameter(animator, MoveYHash))
                {
                    animator.SetFloat(MoveYHash, direction.y);
                }
            }

            // Directional animations (left, right, up, down) are driven by MoveX and MoveY blend trees / clips
        }

        public override IEnumerator AnimateMoveRoutine(Vector2Int[] path, float duration)
        {
            if (path == null || path.Length == 0)
            {
                yield break;
            }

            if (path.Length > 1)
            {
                Vector2 initialDir = path[1] - path[0];
                UpdateFacingDirection(initialDir);
            }

            SetAnimatorBool(IsMovingHash, true);

            int segmentCount = path.Length - 1;
            if (segmentCount > 0 && duration > 0f)
            {
                float segDuration = duration / segmentCount;

                for (int i = 0; i < segmentCount; i++)
                {
                    if (this == null || transform == null) yield break;
                    Vector2 segDir = path[i + 1] - path[i];
                    UpdateFacingDirection(segDir);

                    Vector3 segStart = (i == 0) ? transform.position : GridToWorldCenter(path[i]);
                    Vector3 segEnd = GridToWorldCenter(path[i + 1]);

                    float elapsed = 0f;
                    while (elapsed < segDuration)
                    {
                        if (this == null || transform == null) yield break;
                        elapsed += Time.deltaTime;
                        float progress = Mathf.Clamp01(elapsed / segDuration);
                        float t = (i == segmentCount - 1)
                            ? Mathf.Sin(progress * Mathf.PI * 0.5f)
                            : progress;

                        transform.position = Vector3.Lerp(segStart, segEnd, t);
                        yield return null;
                    }

                    CurrentGridPosition = path[i + 1];
                }

                if (this != null && transform != null)
                {
                    SnapToGrid(path[path.Length - 1]);
                }
            }
            else
            {
                yield return base.AnimateMoveRoutine(path, duration);
            }

            if (this != null)
            {
                SetAnimatorBool(IsMovingHash, false);
            }
        }

        public override IEnumerator AnimateAttackLungeRoutine(Vector2Int targetPos, float duration)
        {
            Vector2 attackDir = targetPos - CurrentGridPosition;
            UpdateFacingDirection(attackDir);

            SetAnimatorTrigger(IsAttackingHash);

            yield return base.AnimateAttackLungeRoutine(targetPos, duration);
        }

        public override IEnumerator AnimateDamageRoutine(int damage)
        {
            SetAnimatorTrigger(TakeDamageHash);

            var playerOcc = GameManager.Instance?.Board?.FindPlayer();
            var stats = GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(damage, null);
                if (playerOcc != null)
                {
                    EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(playerOcc.CurrentHealth, playerOcc.MaxHealth));
                }
            }
            else if (playerOcc != null)
            {
                EventBus<PlayerHealthChangedEvent>.Raise(new PlayerHealthChangedEvent(playerOcc.CurrentHealth, playerOcc.MaxHealth));
            }

            yield return base.AnimateDamageRoutine(damage);
        }

        public override IEnumerator AnimateDeathRoutine()
        {
            SetAnimatorTrigger(DieHash);
            yield return base.AnimateDeathRoutine();
        }

        private void SetAnimatorBool(int hash, bool value)
        {
            if (animator != null && HasParameter(animator, hash))
            {
                animator.SetBool(hash, value);
            }
        }

        private void SetAnimatorTrigger(int hash)
        {
            if (animator != null && HasParameter(animator, hash))
            {
                animator.SetTrigger(hash);
            }
        }
    }
}
