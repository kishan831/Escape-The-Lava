using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeTheLava
{
    /// <summary>
    /// The dedicated win / game-over screen the brief asks for.
    ///
    /// It is one panel driven by an animated sequence: backdrop fade, elastic panel entrance, headline
    /// punch, then the stat lines staggering in one by one, and finally the restart button. Copy and
    /// colour change with the reason the round ended.
    /// </summary>
    public class EndScreen : MonoBehaviour
    {
        /// <summary>Raised by the restart button. <see cref="GameManager"/> subscribes.</summary>
        public event Action RestartRequested;

        [Header("Wired by the scene builder")]
        public CanvasGroup group;
        public Image backdrop;
        public RectTransform panel;
        public Image panelImage;
        public Text headline;
        public Text subline;
        public Text[] statLines;
        public Button restartButton;
        public Text restartLabel;
        public GameConfig config;
        public AudioManager audioManager;
        public SpriteParticles particles;
        public GameAssets assets;

        Coroutine _sequence;

        void Awake()
        {
            if (restartButton) restartButton.onClick.AddListener(OnRestartClicked);
            HideImmediate();
        }

        void OnDestroy()
        {
            if (restartButton) restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        /// <summary>
        /// True once the reveal sequence has finished. <see cref="GameManager"/> also accepts a tap
        /// anywhere as a restart, so the round can always be replayed even if the pointer never lands
        /// on the button.
        /// </summary>
        public bool IsInteractive => group && group.blocksRaycasts && _sequence == null;

        void OnRestartClicked() => RestartRequested?.Invoke();

        public void HideImmediate()
        {
            if (_sequence != null) StopCoroutine(_sequence);
            _sequence = null;

            if (group)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            if (panel) panel.localScale = Vector3.one * 0.7f;
        }

        public void ShowWin(int score, int collected, int total, int livesLeft, float timeLeft, int timeBonus)
        {
            Color accent = config ? config.diamondCore : Color.cyan;

            string[] stats =
            {
                $"SCORE   {score}",
                $"DIAMONDS   {collected} / {total}",
                $"TIME LEFT   {timeLeft:0.0}s   (+{timeBonus})",
                $"LIVES LEFT   {livesLeft}"
            };

            Show("ESCAPED!", "Every diamond collected", accent, stats, true);
        }

        public void ShowLose(EndReason reason, int score, int collected, int total)
        {
            Color accent = config ? config.uiDanger : Color.red;

            string subtitle = reason == EndReason.TimeUp
                ? "The clock ran out"
                : "The lava took your last life";

            string[] stats =
            {
                $"SCORE   {score}",
                $"DIAMONDS   {collected} / {total}",
                $"MISSED   {Mathf.Max(0, total - collected)}",
                reason == EndReason.TimeUp ? "OUT OF TIME" : "OUT OF LIVES"
            };

            Show(reason == EndReason.TimeUp ? "TIME'S UP" : "GAME OVER", subtitle, accent, stats, false);
        }

        void Show(string title, string subtitle, Color accent, string[] stats, bool celebrate)
        {
            if (!group) return;

            if (headline)
            {
                headline.text = title;
                headline.color = accent;
            }
            if (subline) subline.text = subtitle;
            if (panelImage) panelImage.color = new Color(0.07f, 0.06f, 0.1f, 0.96f);
            if (restartLabel) restartLabel.text = celebrate ? "PLAY AGAIN" : "TRY AGAIN";

            if (statLines != null)
            {
                for (int i = 0; i < statLines.Length; i++)
                {
                    if (!statLines[i]) continue;
                    statLines[i].text = i < stats.Length ? stats[i] : string.Empty;
                    statLines[i].color = new Color(1f, 1f, 1f, 0f);
                }
            }

            if (_sequence != null) StopCoroutine(_sequence);
            _sequence = StartCoroutine(ShowSequence(accent, celebrate));
        }

        IEnumerator ShowSequence(Color accent, bool celebrate)
        {
            group.blocksRaycasts = true;
            group.interactable = true;

            if (restartButton) restartButton.gameObject.SetActive(false);
            if (backdrop) backdrop.color = new Color(0.02f, 0.01f, 0.04f, 0f);
            group.alpha = 0f;

            // Backdrop dim and panel entrance share one tween, so nothing can outlive the sequence and
            // fight HideImmediate on a restart. Elastic overshoots past 1, hence the clamp on the fades.
            if (panel)
            {
                Vector2 rest = panel.anchoredPosition;
                panel.localScale = Vector3.one * 0.62f;

                yield return Tween.Value(0.5f, Easing.Type.ElasticOut, t =>
                {
                    panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.62f, 1f, t);
                    panel.anchoredPosition = rest + new Vector2(0f, Mathf.LerpUnclamped(120f, 0f, t));

                    float fade = Mathf.Clamp01(t * 2.5f);
                    group.alpha = fade;
                    if (backdrop) backdrop.color = new Color(0.02f, 0.01f, 0.04f, fade * 0.72f);
                }, unscaled: true);

                panel.anchoredPosition = rest;
            }

            group.alpha = 1f;

            // Headline punch.
            if (headline)
            {
                Transform t = headline.transform;
                yield return Tween.Value(0.34f, Easing.Type.BackOut, p =>
                {
                    t.localScale = Vector3.one * Mathf.LerpUnclamped(0.55f, 1f, p);
                    Color c = headline.color;
                    headline.color = new Color(c.r, c.g, c.b, p);
                }, unscaled: true);
            }

            if (subline) yield return Tween.FadeGraphic(subline, 0f, 0.8f, 0.22f);

            // Stats stagger in, each one a small slide plus fade.
            if (statLines != null)
            {
                foreach (Text line in statLines)
                {
                    if (!line || string.IsNullOrEmpty(line.text)) continue;

                    var rect = (RectTransform)line.transform;
                    Vector2 rest = rect.anchoredPosition;

                    if (audioManager) audioManager.PlayUi();

                    yield return Tween.Value(0.16f, Easing.Type.QuadOut, p =>
                    {
                        line.color = new Color(1f, 1f, 1f, p * 0.92f);
                        rect.anchoredPosition = rest + new Vector2(Mathf.LerpUnclamped(26f, 0f, p), 0f);
                    }, unscaled: true);

                    rect.anchoredPosition = rest;
                }
            }

            if (celebrate) SpawnCelebration();

            // Button last, so nobody restarts before the sequence has read out.
            if (restartButton)
            {
                restartButton.gameObject.SetActive(true);
                Transform bt = restartButton.transform;
                yield return Tween.Value(0.32f, Easing.Type.BackOut,
                    p => bt.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, p), unscaled: true);
            }

            _sequence = null;
        }

        /// <summary>Extra confetti behind the win panel.</summary>
        void SpawnCelebration()
        {
            if (!particles || !assets || !config) return;

            for (int i = 0; i < 3; i++)
            {
                var position = new Vector3(UnityEngine.Random.Range(-4f, 4f), UnityEngine.Random.Range(0f, 3f), 0f);
                particles.Emit(position, FxPresets.WinConfetti(assets, config));
            }
        }
    }
}
