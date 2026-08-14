using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeTheLava
{
    /// <summary>
    /// Minimal coroutine tween helpers. Deliberately dependency free: the project ships without
    /// DOTween so the repository clones and plays with nothing to install.
    ///
    /// Every method returns an <see cref="IEnumerator"/>, so it can be yielded inside a bigger
    /// sequence, or fired and forgotten through <see cref="Run"/>.
    /// </summary>
    public static class Tween
    {
        static TweenRunner s_runner;

        /// <summary>Shared hidden MonoBehaviour used to drive fire-and-forget tweens.</summary>
        static TweenRunner Runner
        {
            get
            {
                // The null check also covers a destroyed runner after a scene change.
                if (s_runner == null)
                {
                    var go = new GameObject("~TweenRunner");
                    s_runner = go.AddComponent<TweenRunner>();
                }
                return s_runner;
            }
        }

        /// <summary>Starts a routine without needing a MonoBehaviour at the call site.</summary>
        public static Coroutine Run(IEnumerator routine) => Runner.StartCoroutine(routine);

        public static void Stop(Coroutine routine)
        {
            if (routine != null && s_runner != null) s_runner.StopCoroutine(routine);
        }

        /// <summary>Ticks <paramref name="onStep"/> with an eased 0..1 value, then guarantees a final 1.</summary>
        public static IEnumerator Value(float duration, Easing.Type ease, Action<float> onStep, bool unscaled = false)
        {
            if (duration <= 0f)
            {
                onStep(1f);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                onStep(Easing.Evaluate(ease, elapsed / duration));
                elapsed += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
            onStep(1f);
        }

        public static IEnumerator Delay(float seconds, bool unscaled = false)
        {
            if (unscaled) yield return new WaitForSecondsRealtime(seconds);
            else yield return new WaitForSeconds(seconds);
        }

        public static IEnumerator Scale(Transform target, Vector3 from, Vector3 to, float duration,
            Easing.Type ease = Easing.Type.QuadOut, bool unscaled = false)
        {
            return Value(duration, ease, t =>
            {
                if (target) target.localScale = Vector3.LerpUnclamped(from, to, t);
            }, unscaled);
        }

        public static IEnumerator Move(Transform target, Vector3 from, Vector3 to, float duration,
            Easing.Type ease = Easing.Type.QuadOut, bool unscaled = false)
        {
            return Value(duration, ease, t =>
            {
                if (target) target.position = Vector3.LerpUnclamped(from, to, t);
            }, unscaled);
        }

        public static IEnumerator MoveLocal(Transform target, Vector3 from, Vector3 to, float duration,
            Easing.Type ease = Easing.Type.QuadOut, bool unscaled = false)
        {
            return Value(duration, ease, t =>
            {
                if (target) target.localPosition = Vector3.LerpUnclamped(from, to, t);
            }, unscaled);
        }

        /// <summary>Quadratic bezier move, used for the collected-diamond arc towards the HUD.</summary>
        public static IEnumerator Arc(Transform target, Vector3 from, Vector3 control, Vector3 to,
            float duration, Easing.Type ease = Easing.Type.QuadInOut, bool unscaled = false)
        {
            return Value(duration, ease, t =>
            {
                if (!target) return;
                float u = 1f - t;
                target.position = u * u * from + 2f * u * t * control + t * t * to;
            }, unscaled);
        }

        public static IEnumerator Fade(SpriteRenderer target, float from, float to, float duration,
            Easing.Type ease = Easing.Type.Linear, bool unscaled = false)
        {
            return Value(duration, ease, t =>
            {
                if (!target) return;
                Color c = target.color;
                c.a = Mathf.LerpUnclamped(from, to, t);
                target.color = c;
            }, unscaled);
        }

        public static IEnumerator Tint(SpriteRenderer target, Color from, Color to, float duration,
            Easing.Type ease = Easing.Type.Linear, bool unscaled = false)
        {
            return Value(duration, ease, t =>
            {
                if (target) target.color = Color.LerpUnclamped(from, to, t);
            }, unscaled);
        }

        public static IEnumerator FadeGraphic(Graphic target, float from, float to, float duration,
            Easing.Type ease = Easing.Type.Linear, bool unscaled = true)
        {
            return Value(duration, ease, t =>
            {
                if (!target) return;
                Color c = target.color;
                c.a = Mathf.LerpUnclamped(from, to, t);
                target.color = c;
            }, unscaled);
        }

        public static IEnumerator TintGraphic(Graphic target, Color from, Color to, float duration,
            Easing.Type ease = Easing.Type.Linear, bool unscaled = true)
        {
            return Value(duration, ease, t =>
            {
                if (target) target.color = Color.LerpUnclamped(from, to, t);
            }, unscaled);
        }

        public static IEnumerator FadeCanvas(CanvasGroup target, float from, float to, float duration,
            Easing.Type ease = Easing.Type.Linear, bool unscaled = true)
        {
            return Value(duration, ease, t =>
            {
                if (target) target.alpha = Mathf.LerpUnclamped(from, to, t);
            }, unscaled);
        }

        /// <summary>Scale kick that settles back to <paramref name="baseScale"/>.</summary>
        public static IEnumerator Punch(Transform target, Vector3 baseScale, float amount, float duration,
            bool unscaled = false)
        {
            Vector3 peak = baseScale * (1f + amount);
            yield return Scale(target, baseScale, peak, duration * 0.32f, Easing.Type.QuadOut, unscaled);
            yield return Scale(target, peak, baseScale, duration * 0.68f, Easing.Type.ElasticOut, unscaled);
        }

        /// <summary>Random positional jitter that decays to zero, applied around a fixed anchor.</summary>
        public static IEnumerator Shake(Transform target, Vector3 anchor, float strength, float duration,
            bool unscaled = true)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float falloff = 1f - elapsed / duration;
                Vector2 offset = UnityEngine.Random.insideUnitCircle * strength * falloff * falloff;
                if (target) target.position = anchor + (Vector3)offset;
                elapsed += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
            if (target) target.position = anchor;
        }

        /// <summary>Hidden coroutine host. Nothing to configure, hence the empty body.</summary>
        class TweenRunner : MonoBehaviour
        {
            void Awake() => gameObject.hideFlags = HideFlags.DontSaveInEditor;
        }
    }
}
