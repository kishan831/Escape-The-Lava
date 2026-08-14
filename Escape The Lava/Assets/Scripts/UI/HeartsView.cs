using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeTheLava
{
    /// <summary>
    /// The five heart icons from the brief. Full hearts beat gently; a lost heart bursts, drains to a
    /// dark husk and the remaining row flinches.
    /// </summary>
    public class HeartsView : MonoBehaviour
    {
        [Header("Wired by the scene builder")]
        public Image[] hearts;
        public GameConfig config;

        int _shown = -1;
        Coroutine[] _routines;

        public Color FullColor => config ? config.uiDanger : new Color(1f, 0.29f, 0.26f);
        static readonly Color EmptyColor = new Color(0.22f, 0.16f, 0.19f, 0.85f);

        void Awake()
        {
            if (hearts != null) _routines = new Coroutine[hearts.Length];
        }

        void Update()
        {
            if (hearts == null) return;

            // Gentle heartbeat on the hearts the player still has, each offset so the row ripples.
            for (int i = 0; i < hearts.Length; i++)
            {
                if (!hearts[i] || i >= _shown) continue;
                if (_routines != null && _routines[i] != null) continue;

                float beat = 1f + Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Time.time * 2.2f - i * 0.35f)), 6f) * 0.12f;
                hearts[i].transform.localScale = Vector3.one * beat;
            }
        }

        public void SetLives(int lives, bool animate)
        {
            if (hearts == null) return;
            if (_routines == null || _routines.Length != hearts.Length) _routines = new Coroutine[hearts.Length];

            int previous = _shown;
            _shown = Mathf.Clamp(lives, 0, hearts.Length);

            for (int i = 0; i < hearts.Length; i++)
            {
                if (!hearts[i]) continue;

                bool full = i < _shown;
                bool justLost = animate && previous > _shown && i >= _shown && i < previous;

                if (justLost)
                {
                    if (_routines[i] != null) StopCoroutine(_routines[i]);
                    _routines[i] = StartCoroutine(BreakRoutine(i));
                }
                else if (!animate || i >= previous || full)
                {
                    hearts[i].color = full ? FullColor : EmptyColor;
                    hearts[i].transform.localScale = Vector3.one;
                }
            }

            if (animate && previous > _shown) StartCoroutine(FlinchRow());
        }

        IEnumerator BreakRoutine(int index)
        {
            Image heart = hearts[index];
            Transform t = heart.transform;

            // Blow up white, then collapse into the empty husk.
            yield return Tween.Value(0.12f, Easing.Type.QuadOut, p =>
            {
                heart.color = Color.Lerp(FullColor, Color.white, p);
                t.localScale = Vector3.one * Mathf.Lerp(1f, 1.55f, p);
                t.localRotation = Quaternion.Euler(0f, 0f, p * 18f);
            }, unscaled: true);

            yield return Tween.Value(0.32f, Easing.Type.BackIn, p =>
            {
                heart.color = Color.Lerp(Color.white, EmptyColor, p);
                t.localScale = Vector3.one * Mathf.Lerp(1.55f, 0.86f, p);
                t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(18f, -8f, p));
            }, unscaled: true);

            yield return Tween.Value(0.2f, Easing.Type.ElasticOut, p =>
            {
                t.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, p);
                t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-8f, 0f, p));
            }, unscaled: true);

            heart.color = EmptyColor;
            t.localScale = Vector3.one;
            t.localRotation = Quaternion.identity;
            _routines[index] = null;
        }

        /// <summary>Whole row jolts sideways, so losing a life registers even in peripheral vision.</summary>
        IEnumerator FlinchRow()
        {
            var rect = (RectTransform)transform;
            Vector2 rest = rect.anchoredPosition;

            yield return Tween.Value(0.28f, Easing.Type.ElasticOut, p =>
            {
                float offset = Mathf.Sin(p * Mathf.PI * 3f) * (1f - p) * 12f;
                rect.anchoredPosition = rest + new Vector2(offset, 0f);
            }, unscaled: true);

            rect.anchoredPosition = rest;
        }
    }
}
