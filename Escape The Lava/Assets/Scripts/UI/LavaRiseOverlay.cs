using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeTheLava
{
    /// <summary>
    /// Loss transition: the lava floods up the screen with a wobbling surface line and a heat glow.
    /// It gives the game-over sequence a beat of anticipation before the panel appears, and it is the
    /// theme delivering the bad news rather than a plain fade.
    /// </summary>
    public class LavaRiseOverlay : MonoBehaviour
    {
        [Header("Wired by the scene builder")]
        public RectTransform root;
        public Image body;
        public RectTransform surface;
        public Image surfaceImage;
        public Image heatGlow;
        public GameConfig config;
        public AudioManager audioManager;

        bool _active;
        float _wobbleTime;

        void Awake() => Reset();

        /// <summary>
        /// Y offset that parks the slab entirely below the screen. The slab is anchored to the bottom
        /// of the canvas with a bottom pivot, so pushing it down by its own height puts its top edge
        /// exactly on the bottom of the screen.
        /// </summary>
        float HiddenY => -(root ? root.rect.height : 2000f) - 8f;

        public void Reset()
        {
            _active = false;
            if (root) root.anchoredPosition = new Vector2(0f, HiddenY);
            if (heatGlow) heatGlow.color = new Color(1f, 0.4f, 0.1f, 0f);
        }

        /// <summary>Raises the lava until it covers the screen.</summary>
        public IEnumerator PlayRise(float duration)
        {
            if (!root) yield break;

            _active = true;
            _wobbleTime = 0f;
            if (audioManager) audioManager.PlayWhoosh(0.55f);

            float from = HiddenY;
            const float to = 0f;          // slab bottom on the screen bottom: fully flooded
            root.anchoredPosition = new Vector2(0f, from);

            yield return Tween.Value(duration, Easing.Type.CubicOut, t =>
            {
                root.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(from, to, t));
                if (heatGlow) heatGlow.color = new Color(1f, 0.42f, 0.12f, t * 0.5f);
            }, unscaled: true);
        }

        void Update()
        {
            if (!_active || !surface) return;

            // Two sine waves at different rates so the surface never looks like a metronome.
            _wobbleTime += Time.unscaledDeltaTime;
            float wobble = Mathf.Sin(_wobbleTime * 3.1f) * 6f + Mathf.Sin(_wobbleTime * 5.7f) * 3f;
            surface.anchoredPosition = new Vector2(Mathf.Sin(_wobbleTime * 1.3f) * 14f, wobble);

            if (surfaceImage && config)
            {
                float heat = 0.65f + Mathf.Sin(_wobbleTime * 4.2f) * 0.2f;
                surfaceImage.color = Color.Lerp(config.lavaDeep, config.lavaHot, heat);
            }
        }
    }
}
