using System.Collections;
using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// Very short global slow-down used as impact punctuation ("hit-stop").
    /// Requests are ignored while a stronger one is already running so damage taken in quick
    /// succession cannot stack into a long, mushy slow-motion.
    /// </summary>
    public static class TimeFx
    {
        static Coroutine s_active;
        static float s_activeScale = 1f;

        public static void HitStop(float scale, float duration)
        {
            scale = Mathf.Clamp(scale, 0.02f, 1f);
            if (s_active != null && scale >= s_activeScale) return;

            if (s_active != null) Tween.Stop(s_active);
            s_active = Tween.Run(Routine(scale, duration));
        }

        static IEnumerator Routine(float scale, float duration)
        {
            s_activeScale = scale;
            Time.timeScale = scale;
            yield return new WaitForSecondsRealtime(duration);

            // Ease back so the return to full speed does not pop.
            yield return Tween.Value(0.12f, Easing.Type.QuadOut,
                t => Time.timeScale = Mathf.Lerp(scale, 1f, t), unscaled: true);

            Time.timeScale = 1f;
            s_activeScale = 1f;
            s_active = null;
        }

        /// <summary>Hard reset. Called when a round restarts so a pending hit-stop cannot leak.</summary>
        public static void Reset()
        {
            if (s_active != null) Tween.Stop(s_active);
            s_active = null;
            s_activeScale = 1f;
            Time.timeScale = 1f;
        }
    }
}
