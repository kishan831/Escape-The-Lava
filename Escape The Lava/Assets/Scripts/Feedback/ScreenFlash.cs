using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeTheLava
{
    /// <summary>
    /// Full-screen damage feedback: a hard colour flash plus a red vignette that pulses in from the
    /// edges. Both images are wired by the scene builder and sit above gameplay but below the HUD.
    /// </summary>
    public class ScreenFlash : MonoBehaviour
    {
        [Header("Wired by the scene builder")]
        public Image flashImage;
        public Image vignetteImage;

        Coroutine _flashRoutine;
        Coroutine _vignetteRoutine;

        void Awake() => Clear();

        /// <summary>Hard cut to <paramref name="color"/>, then fade out.</summary>
        public void Flash(Color color, float peakAlpha = 0.45f, float duration = 0.35f)
        {
            if (!flashImage) return;
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine(color, peakAlpha, duration));
        }

        /// <summary>Pulses the edge vignette. Used on damage and while time is running out.</summary>
        public void Vignette(Color color, float peakAlpha = 0.7f, float duration = 0.6f)
        {
            if (!vignetteImage) return;
            if (_vignetteRoutine != null) StopCoroutine(_vignetteRoutine);
            _vignetteRoutine = StartCoroutine(VignetteRoutine(color, peakAlpha, duration));
        }

        IEnumerator FlashRoutine(Color color, float peakAlpha, float duration)
        {
            flashImage.color = new Color(color.r, color.g, color.b, peakAlpha);
            yield return Tween.FadeGraphic(flashImage, peakAlpha, 0f, duration, Easing.Type.QuadOut);
            _flashRoutine = null;
        }

        IEnumerator VignetteRoutine(Color color, float peakAlpha, float duration)
        {
            vignetteImage.color = new Color(color.r, color.g, color.b, 0f);
            yield return Tween.FadeGraphic(vignetteImage, 0f, peakAlpha, duration * 0.18f, Easing.Type.QuadOut);
            yield return Tween.FadeGraphic(vignetteImage, peakAlpha, 0f, duration * 0.82f, Easing.Type.QuadIn);
            _vignetteRoutine = null;
        }

        /// <summary>Cancels anything in flight and hides both overlays.</summary>
        public void Clear()
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            if (_vignetteRoutine != null) StopCoroutine(_vignetteRoutine);
            _flashRoutine = null;
            _vignetteRoutine = null;

            if (flashImage) flashImage.color = new Color(1f, 1f, 1f, 0f);
            if (vignetteImage) vignetteImage.color = new Color(1f, 0f, 0f, 0f);
        }
    }
}
