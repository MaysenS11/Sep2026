using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Effects;
using Presentation.Board;
using Presentation.Entities;

namespace Presentation.Effects
{
    /// <summary>
    /// Dedicated presenter executing visual attack effects (particles, projectiles, slashes, arcs)
    /// for all 4 weapon archetypes:
    /// - Degen (Rapier): High-speed single-target thrust stopping at first occupant.
    /// - Halberd: Sweeping cleave arc stopping at first obstacle.
    /// - Pistol: Cardinal piercing projectile passing through aligned targets.
    /// - Flask (Raven Mask): Parabolic thrown chemical vial impacting target tile with toxic splash.
    /// </summary>
    public static class WeaponAttackVFX
    {
        private static Sprite s_SparkSprite;
        private static Sprite s_FlaskSprite;
        private static Sprite s_BulletSprite;

        public static IEnumerator PlayAttackRoutine(WeaponAttackEffect effect, float duration)
        {
            if (effect == null) yield break;

            float actualDuration = Mathf.Max(0.2f, duration);

            switch (effect.Weapon)
            {
                case WeaponType.Degen:
                    yield return PlayDegenRoutine(effect, actualDuration);
                    break;

                case WeaponType.Halberd:
                    yield return PlayHalberdRoutine(effect, actualDuration);
                    break;

                case WeaponType.Pistol:
                    yield return PlayPistolRoutine(effect, actualDuration);
                    break;

                case WeaponType.Flask:
                    yield return PlayFlaskRoutine(effect, actualDuration);
                    break;
            }
        }

        #region Degen (Rapier) VFX

        private static IEnumerator PlayDegenRoutine(WeaponAttackEffect effect, float duration)
        {
            Vector3 originWorld = TileObject.GridToWorld(effect.OriginPos);
            Vector2Int strikeEndGrid = effect.HitTiles.Count > 0 ? effect.HitTiles[0] : effect.PrimaryTarget;
            Vector3 targetWorld = TileObject.GridToWorld(strikeEndGrid);

            GameObject vfxRoot = new GameObject("VFX_DegenThrust");
            vfxRoot.transform.position = originWorld;

            LineRenderer line = vfxRoot.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.startWidth = 0.12f;
            line.endWidth = 0.04f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(1f, 1f, 1f, 0.95f);
            line.endColor = new Color(0.8f, 0.9f, 1f, 0.8f);
            line.positionCount = 2;
            line.sortingOrder = 120;

            float thrustOut = duration * 0.35f;
            float retract = duration * 0.65f;

            // Phase 1: Rapid thrust forward
            float elapsed = 0f;
            while (elapsed < thrustOut)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Sin(Mathf.Clamp01(elapsed / thrustOut) * Mathf.PI * 0.5f);
                Vector3 currentTip = Vector3.Lerp(originWorld, targetWorld, t);
                line.SetPosition(0, originWorld);
                line.SetPosition(1, currentTip);
                yield return null;
            }

            // Impact flash at tip
            SpawnImpactSparks(targetWorld, new Color(1f, 0.9f, 0.6f), 6);

            // Phase 2: Retract thrust
            elapsed = 0f;
            while (elapsed < retract)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / retract);
                float t = Mathf.SmoothStep(0f, 1f, progress);
                Vector3 currentTip = Vector3.Lerp(targetWorld, originWorld, t);
                line.SetPosition(0, originWorld);
                line.SetPosition(1, currentTip);
                line.startColor = new Color(1f, 1f, 1f, (1f - progress) * 0.95f);
                yield return null;
            }

            UnityEngine.Object.Destroy(vfxRoot);
        }

        #endregion

        #region Halberd Sweep Cleave VFX

        private static IEnumerator PlayHalberdRoutine(WeaponAttackEffect effect, float duration)
        {
            Vector3 originWorld = TileObject.GridToWorld(effect.OriginPos);

            GameObject vfxRoot = new GameObject("VFX_HalberdSweep");
            vfxRoot.transform.position = originWorld;

            int targetCount = effect.TargetTiles.Count;
            var slashLines = new List<LineRenderer>();

            for (int i = 0; i < targetCount; i++)
            {
                Vector3 targetWorld = TileObject.GridToWorld(effect.TargetTiles[i]);
                GameObject seg = new GameObject($"SweepSeg_{i}");
                seg.transform.SetParent(vfxRoot.transform, false);

                LineRenderer line = seg.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.startWidth = 0.16f;
                line.endWidth = 0.08f;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = new Color(1f, 0.85f, 0.3f, 0.9f);
                line.endColor = new Color(1f, 0.5f, 0.1f, 0.8f);
                line.positionCount = 2;
                line.sortingOrder = 120;
                line.SetPosition(0, originWorld);
                line.SetPosition(1, originWorld);
                slashLines.Add(line);
            }

            float sweepDuration = duration * 0.7f;
            float fadeDuration = duration * 0.3f;

            // Sequential sweep across targeted arc
            float elapsed = 0f;
            int lastSparkIdx = -1;
            while (elapsed < sweepDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / sweepDuration);
                int activeIndex = Mathf.Clamp(Mathf.FloorToInt(t * targetCount), 0, targetCount - 1);

                for (int i = 0; i < targetCount; i++)
                {
                    if (i <= activeIndex)
                    {
                        Vector3 targetWorld = TileObject.GridToWorld(effect.TargetTiles[i]);
                        slashLines[i].SetPosition(0, Vector3.Lerp(originWorld, targetWorld, 0.25f));
                        slashLines[i].SetPosition(1, targetWorld);

                        if (i != lastSparkIdx && effect.HitTiles.Contains(effect.TargetTiles[i]))
                        {
                            lastSparkIdx = i;
                            SpawnImpactSparks(targetWorld, new Color(1f, 0.6f, 0.2f), 5);
                        }
                    }
                }
                yield return null;
            }

            // Fade out sweep trails
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                for (int i = 0; i < slashLines.Count; i++)
                {
                    slashLines[i].startColor = new Color(1f, 0.85f, 0.3f, alpha * 0.9f);
                    slashLines[i].endColor = new Color(1f, 0.5f, 0.1f, alpha * 0.8f);
                }
                yield return null;
            }

            UnityEngine.Object.Destroy(vfxRoot);
        }

        #endregion

        #region Pistol Piercing Projectile VFX

        private static IEnumerator PlayPistolRoutine(WeaponAttackEffect effect, float duration)
        {
            Vector3 originWorld = TileObject.GridToWorld(effect.OriginPos);
            Vector3 dir = new Vector3(effect.Direction.x, effect.Direction.y, 0f).normalized;

            Vector3 endWorld = effect.TargetTiles.Count > 0
                ? TileObject.GridToWorld(effect.TargetTiles[effect.TargetTiles.Count - 1])
                : originWorld + dir * 3f;

            // Muzzle flash at player border
            Vector3 muzzlePos = originWorld + dir * 0.4f;
            SpawnMuzzleFlash(muzzlePos);

            GameObject bullet = new GameObject("VFX_PistolBullet");
            bullet.transform.position = muzzlePos;

            var sr = bullet.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateBulletSprite();
            sr.color = new Color(1f, 0.95f, 0.5f, 1f);
            sr.sortingOrder = 130;
            bullet.transform.localScale = new Vector3(0.5f, 0.25f, 1f);

            // Rotate bullet towards direction
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

            float bulletFlightTime = duration * 0.65f;
            float elapsed = 0f;

            var sparkedTiles = new HashSet<Vector2Int>();

            while (elapsed < bulletFlightTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / bulletFlightTime);
                Vector3 currentPos = Vector3.Lerp(muzzlePos, endWorld, progress);
                bullet.transform.position = currentPos;

                Vector2Int currentGrid = TileObject.WorldToGrid(currentPos);
                if (effect.HitTiles.Contains(currentGrid) && sparkedTiles.Add(currentGrid))
                {
                    SpawnImpactSparks(TileObject.GridToWorld(currentGrid), new Color(1f, 0.8f, 0.2f), 4);
                }

                yield return null;
            }

            UnityEngine.Object.Destroy(bullet);

            // Small linger for impact dust
            float linger = duration - bulletFlightTime;
            if (linger > 0f)
            {
                yield return new WaitForSeconds(linger);
            }
        }

        #endregion

        #region Flask (Raven Mask) Thrown Arc VFX

        private static IEnumerator PlayFlaskRoutine(WeaponAttackEffect effect, float duration)
        {
            Vector3 originWorld = TileObject.GridToWorld(effect.OriginPos);
            Vector3 targetWorld = TileObject.GridToWorld(effect.PrimaryTarget);

            GameObject flask = new GameObject("VFX_ThrownFlask");
            flask.transform.position = originWorld;

            var sr = flask.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateFlaskSprite();
            sr.color = new Color(0.3f, 0.95f, 0.4f, 1f); // Toxic emerald
            sr.sortingOrder = 130;
            flask.transform.localScale = Vector3.one * 0.7f;

            float flightDuration = duration * 0.75f;
            float arcPeak = 1.25f;
            float elapsed = 0f;

            // Phase 1: Thrown parabolic flight arc
            while (elapsed < flightDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / flightDuration);
                float t = Mathf.SmoothStep(0f, 1f, progress);

                Vector3 basePos = Vector3.Lerp(originWorld, targetWorld, t);
                float arc = 4f * arcPeak * progress * (1f - progress);
                flask.transform.position = new Vector3(basePos.x, basePos.y + arc, basePos.z);

                flask.transform.Rotate(0, 0, 720f * Time.deltaTime);
                yield return null;
            }

            UnityEngine.Object.Destroy(flask);

            // Phase 2: Toxic splash explosion on landing
            SpawnToxicSplash(targetWorld, duration - flightDuration);

            float remainder = duration - flightDuration;
            if (remainder > 0f)
            {
                yield return new WaitForSeconds(remainder);
            }
        }

        private static void SpawnToxicSplash(Vector3 center, float duration)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (360f / 8) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * UnityEngine.Random.Range(0.2f, 0.45f);

                var go = new GameObject("VFX_SplashDrop");
                go.transform.position = center;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetOrCreateSparkSprite();
                sr.color = new Color(0.2f, 0.95f, 0.4f, 0.9f);
                sr.sortingOrder = 125;
                go.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.3f, 0.55f);

                EffectsQueueRunner.Instance?.StartCoroutine(FadeAndDestroy(go, center + offset, Mathf.Max(0.12f, duration)));
            }
        }

        #endregion

        #region Helpers & Procedural Sprites

        private static void SpawnMuzzleFlash(Vector3 position)
        {
            var go = new GameObject("VFX_MuzzleFlash");
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateSparkSprite();
            sr.color = new Color(1f, 1f, 0.7f, 1f);
            sr.sortingOrder = 135;
            go.transform.localScale = Vector3.one * 0.8f;
            EffectsQueueRunner.Instance?.StartCoroutine(FadeAndDestroy(go, position, 0.08f));
        }

        private static void SpawnImpactSparks(Vector3 position, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("VFX_Spark");
                go.transform.position = position;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetOrCreateSparkSprite();
                sr.color = color;
                sr.sortingOrder = 135;
                go.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.2f, 0.4f);

                Vector3 target = position + (Vector3)(UnityEngine.Random.insideUnitCircle * UnityEngine.Random.Range(0.25f, 0.55f));
                EffectsQueueRunner.Instance?.StartCoroutine(FadeAndDestroy(go, target, 0.15f));
            }
        }

        private static IEnumerator FadeAndDestroy(GameObject go, Vector3 targetPos, float duration)
        {
            Vector3 startPos = go.transform.position;
            var sr = go.GetComponent<SpriteRenderer>();
            Color startColor = sr != null ? sr.color : Color.white;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (go == null) yield break;
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                go.transform.position = Vector3.Lerp(startPos, targetPos, progress);
                if (sr != null)
                {
                    sr.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);
                }
                yield return null;
            }

            if (go != null)
            {
                UnityEngine.Object.Destroy(go);
            }
        }

        private static Sprite GetOrCreateSparkSprite()
        {
            if (s_SparkSprite != null) return s_SparkSprite;

            int size = 8;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            Color clear = new Color(0, 0, 0, 0);
            Color white = Color.white;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f));
                    tex.SetPixel(x, y, dist <= size * 0.45f ? white : clear);
                }
            }
            tex.Apply();
            s_SparkSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 8f);
            return s_SparkSprite;
        }

        private static Sprite GetOrCreateBulletSprite()
        {
            if (s_BulletSprite != null) return s_BulletSprite;

            int width = 12;
            int height = 6;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            Color gold = new Color(1f, 0.9f, 0.4f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, gold);
                }
            }
            tex.Apply();
            s_BulletSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 8f);
            return s_BulletSprite;
        }

        private static Sprite GetOrCreateFlaskSprite()
        {
            if (s_FlaskSprite != null) return s_FlaskSprite;

            int size = 12;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            Color clear = new Color(0, 0, 0, 0);
            Color green = new Color(0.2f, 0.95f, 0.4f, 1f);
            Color glass = new Color(0.8f, 1f, 0.9f, 0.9f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (y >= 8 && x >= 4 && x <= 7) // Neck
                    {
                        tex.SetPixel(x, y, glass);
                    }
                    else if (y < 8 && x >= 2 && x <= 9) // Body
                    {
                        tex.SetPixel(x, y, y <= 5 ? green : glass);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            s_FlaskSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 12f);
            return s_FlaskSprite;
        }

        #endregion
    }
}
