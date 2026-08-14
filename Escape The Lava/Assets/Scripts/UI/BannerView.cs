using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeTheLava
{
    /// <summary>
    /// Round-start banner. Two beats - the objective, then GO! - so the player knows what to do
    /// before the 30 second clock starts.
    /// </summary>
    public class BannerView : MonoBehaviour
    {
        [Header("Wired by the scene builder")]
        public CanvasGroup group;
        public Text headline;
        public Text subline;
        public AudioManager audioManager;

        void Awake() => HideImmediate();

        public void HideImmediate()
        {
            if (group) group.alpha = 0f;
        }

        public IEnumerator PlayCountIn(float duration)
        {
            if (!group || !headline) yield break;

            float readyTime = duration * 0.6f;
            float goTime = Mathf.Max(0.25f, duration - readyTime);

            // Beat 1 - the objective.
            headline.text = "TAP THE DIAMONDS";
            if (subline) subline.text = "AVOID THE LAVA";
            yield return Beat(readyTime, 0.82f, 1.0f);

            // Beat 2 - the start cue.
            headline.text = "GO!";
            if (subline) subline.text = string.Empty;
            if (audioManager) audioManager.PlayWhoosh(1.35f);
            yield return Beat(goTime, 0.6f, 1.35f);

            group.alpha = 0f;
        }

        /// <summary>Scale-in overshoot, hold, then scale up and fade out.</summary>
        IEnumerator Beat(float duration, float fromScale, float toScale)
        {
            var rect = (RectTransform)headline.transform.parent;
            float inTime = duration * 0.28f;
            float holdTime = duration * 0.44f;
            float outTime = duration - inTime - holdTime;

            yield return Tween.Value(inTime, Easing.Type.BackOut, t =>
            {
                group.alpha = t;
                rect.localScale = Vector3.one * Mathf.LerpUnclamped(fromScale, toScale, t);
            }, unscaled: true);

            yield return new WaitForSecondsRealtime(holdTime);

            yield return Tween.Value(outTime, Easing.Type.QuadIn, t =>
            {
                group.alpha = 1f - t;
                rect.localScale = Vector3.one * Mathf.LerpUnclamped(toScale, toScale * 1.18f, t);
            }, unscaled: true);
        }
    }
}
