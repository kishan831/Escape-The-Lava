using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeTheLava
{
    /// <summary>
    /// The in-game HUD required by the brief: countdown timer, five hearts, and score / diamonds
    /// collected. The score counts up rather than snapping, the timer changes colour and pulses in
    /// the last ten seconds, and the diamond counter punches on every pickup.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [Header("Wired by the scene builder")]
        public CanvasGroup group;
        public Text timerText;
        public Image timerBar;
        public RectTransform timerRoot;
        public Text scoreText;
        public RectTransform scoreRoot;
        public RectTransform scoreAnchor;
        public Text diamondText;
        public RectTransform diamondRoot;
        public Text comboText;
        public HeartsView hearts;

        GameConfig _config;

        int _displayedScore;
        int _targetScore;
        Coroutine _scoreRoutine;
        Coroutine _timerPulse;
        Coroutine _diamondPunch;
        Coroutine _comboRoutine;

        Color _timerNormal = Color.white;

        public void Bind(GameConfig config)
        {
            _config = config;
            if (hearts) hearts.config = config;
            _timerNormal = Color.white;
            if (comboText) comboText.color = new Color(1f, 1f, 1f, 0f);
        }

        // ------------------------------------------------------------------ timer

        public void SetTime(float secondsLeft, bool low)
        {
            if (timerText)
            {
                // One decimal keeps the last seconds tense without turning into a stopwatch readout.
                timerText.text = secondsLeft >= 10f
                    ? Mathf.CeilToInt(secondsLeft).ToString()
                    : secondsLeft.ToString("0.0");

                Color danger = _config ? _config.uiDanger : Color.red;
                timerText.color = low
                    ? Color.Lerp(danger, Color.white, Mathf.PingPong(Time.time * 4f, 1f) * 0.5f)
                    : _timerNormal;
            }

            if (timerBar && _config)
            {
                timerBar.fillAmount = Mathf.Clamp01(secondsLeft / _config.roundDuration);
                timerBar.color = low
                    ? _config.uiDanger
                    : Color.Lerp(_config.uiDanger, _config.uiAccent, Mathf.Clamp01(secondsLeft / _config.roundDuration));
            }
        }

        /// <summary>One-second heartbeat on the clock. Harder when time is nearly out.</summary>
        public void PulseTimer(bool urgent)
        {
            if (!timerRoot) return;
            if (_timerPulse != null) StopCoroutine(_timerPulse);
            _timerPulse = StartCoroutine(PulseRoutine(timerRoot, urgent ? 0.22f : 0.09f, urgent ? 0.34f : 0.22f));
        }

        // ------------------------------------------------------------------ score

        public void SetScore(int score, bool animate)
        {
            _targetScore = score;

            if (!animate)
            {
                _displayedScore = score;
                if (scoreText) scoreText.text = score.ToString();
                return;
            }

            if (_scoreRoutine != null) StopCoroutine(_scoreRoutine);
            _scoreRoutine = StartCoroutine(CountUpRoutine());

            if (scoreRoot) StartCoroutine(PulseRoutine(scoreRoot, 0.18f, 0.3f));
        }

        IEnumerator CountUpRoutine()
        {
            int from = _displayedScore;
            int to = _targetScore;
            // Longer roll for bigger jumps, but never long enough to lag behind the next pickup.
            float duration = Mathf.Clamp(Mathf.Abs(to - from) / 900f, 0.15f, 0.5f);

            yield return Tween.Value(duration, Easing.Type.QuadOut, t =>
            {
                _displayedScore = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
                if (scoreText) scoreText.text = _displayedScore.ToString();
            }, unscaled: true);

            _displayedScore = to;
            if (scoreText) scoreText.text = to.ToString();
            _scoreRoutine = null;
        }

        /// <summary>Where collected diamonds should fly to, in world space.</summary>
        public Vector3 ScoreAnchorWorldPosition(Camera cam)
        {
            if (!scoreAnchor || !cam) return Vector3.zero;

            // Overlay canvas: pass a null camera to get the screen point, then unproject.
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, scoreAnchor.position);
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(cam.transform.position.z)));
            world.z = 0f;
            return world;
        }

        // ------------------------------------------------------------------ diamonds / lives / combo

        public void SetDiamonds(int collected, int total, bool animate)
        {
            if (diamondText) diamondText.text = $"{collected} / {total}";
            if (!animate || !diamondRoot) return;

            if (_diamondPunch != null) StopCoroutine(_diamondPunch);
            _diamondPunch = StartCoroutine(PulseRoutine(diamondRoot, 0.26f, 0.34f));
        }

        public void SetLives(int lives, bool animate)
        {
            if (hearts) hearts.SetLives(lives, animate);
        }

        public void SetCombo(int multiplier)
        {
            if (!comboText) return;

            if (multiplier <= 1)
            {
                if (_comboRoutine != null) StopCoroutine(_comboRoutine);
                _comboRoutine = StartCoroutine(Tween.FadeGraphic(comboText, comboText.color.a, 0f, 0.25f));
                return;
            }

            comboText.text = $"x{multiplier}";
            comboText.color = _config ? _config.uiAccent : Color.yellow;

            if (_comboRoutine != null) StopCoroutine(_comboRoutine);
            _comboRoutine = StartCoroutine(ComboPopRoutine());
        }

        IEnumerator ComboPopRoutine()
        {
            var rect = (RectTransform)comboText.transform;
            yield return Tween.Value(0.22f, Easing.Type.BackOut, t =>
            {
                rect.localScale = Vector3.one * Mathf.LerpUnclamped(0.5f, 1f, t);
                Color c = comboText.color;
                c.a = t;
                comboText.color = c;
            }, unscaled: true);
            _comboRoutine = null;
        }

        // ------------------------------------------------------------------ visibility

        public void Show()
        {
            if (!group) return;
            group.alpha = 1f;
            StartCoroutine(Tween.FadeCanvas(group, 0f, 1f, 0.35f));
        }

        public void Hide()
        {
            if (!group) return;
            StartCoroutine(Tween.FadeCanvas(group, group.alpha, 0f, 0.3f));
        }

        static IEnumerator PulseRoutine(RectTransform target, float amount, float duration)
        {
            Vector3 rest = Vector3.one;
            yield return Tween.Value(duration * 0.3f, Easing.Type.QuadOut,
                t => target.localScale = Vector3.LerpUnclamped(rest, rest * (1f + amount), t), unscaled: true);
            yield return Tween.Value(duration * 0.7f, Easing.Type.ElasticOut,
                t => target.localScale = Vector3.LerpUnclamped(rest * (1f + amount), rest, t), unscaled: true);
            target.localScale = rest;
        }
    }
}
