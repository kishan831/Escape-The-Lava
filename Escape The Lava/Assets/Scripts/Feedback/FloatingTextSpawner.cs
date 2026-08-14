using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeTheLava
{
    /// <summary>
    /// Pooled score popups. The brief asks for the popup to appear "at the exact position where the
    /// player clicks a diamond", so the spawner takes a world position, converts it through the
    /// camera and places the label in canvas space - it lands on the pixel that was tapped even
    /// while the camera is shaking.
    /// </summary>
    public class FloatingTextSpawner : MonoBehaviour
    {
        [Header("Wired by the scene builder")]
        public GameAssets assets;
        public RectTransform canvasRect;
        public Camera worldCamera;

        [Tooltip("Simultaneous popups. Fast combos can put several on screen at once.")]
        public int poolSize = 16;

        Item[] _items;
        int _cursor;

        class Item
        {
            public RectTransform rect;
            public Text text;
            public Outline outline;
            public Coroutine routine;
            public bool busy;
        }

        void Awake()
        {
            _items = new Item[Mathf.Max(4, poolSize)];
            for (int i = 0; i < _items.Length; i++) _items[i] = Create(i);
        }

        Item Create(int index)
        {
            var go = new GameObject($"popup{index}", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 90f);

            Text text = go.AddComponent<Text>();
            text.font = assets ? assets.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 46;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            go.SetActive(false);
            return new Item { rect = rect, text = text, outline = outline };
        }

        /// <summary>Spawns a popup anchored to a world-space point.</summary>
        public void SpawnAtWorld(Vector3 worldPosition, string label, Color color,
            int fontSize = 46, float rise = 110f, float duration = 0.95f)
        {
            Camera cam = worldCamera ? worldCamera : Camera.main;
            if (!cam) return;
            SpawnAtScreen(cam.WorldToScreenPoint(worldPosition), label, color, fontSize, rise, duration);
        }

        /// <summary>Spawns a popup at a screen-space point.</summary>
        public void SpawnAtScreen(Vector2 screenPosition, string label, Color color,
            int fontSize = 46, float rise = 110f, float duration = 0.95f)
        {
            if (_items == null || !canvasRect) return;

            Item item = Take();
            if (item == null) return;

            // Overlay canvases can be scaled by the CanvasScaler, so go through the utility instead
            // of assuming 1 canvas unit == 1 screen pixel.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 local);

            item.rect.anchoredPosition = local;
            item.text.text = label;
            item.text.fontSize = fontSize;
            item.text.color = color;
            item.rect.gameObject.SetActive(true);

            if (item.routine != null) StopCoroutine(item.routine);
            item.busy = true;
            item.routine = StartCoroutine(Animate(item, local, rise, duration, color));
        }

        IEnumerator Animate(Item item, Vector2 origin, float rise, float duration, Color color)
        {
            float drift = Random.Range(-24f, 24f);
            Vector3 baseScale = Vector3.one;

            // Overshoot pop so the number reads instantly.
            item.rect.localScale = baseScale * 0.45f;
            yield return Tween.Value(duration * 0.22f, Easing.Type.BackOut, t =>
            {
                item.rect.localScale = Vector3.LerpUnclamped(baseScale * 0.45f, baseScale * 1.12f, t);
                item.rect.anchoredPosition = origin + new Vector2(drift * t * 0.25f, rise * 0.28f * t);
            }, unscaled: true);

            // Rise and fade.
            yield return Tween.Value(duration * 0.78f, Easing.Type.QuadOut, t =>
            {
                item.rect.localScale = Vector3.LerpUnclamped(baseScale * 1.12f, baseScale * 0.92f, t);
                item.rect.anchoredPosition = origin + new Vector2(drift, rise * (0.28f + 0.72f * t));

                float alpha = 1f - Mathf.Pow(t, 2.2f);
                item.text.color = new Color(color.r, color.g, color.b, alpha);
                item.outline.effectColor = new Color(0f, 0f, 0f, 0.75f * alpha);
            }, unscaled: true);

            item.rect.gameObject.SetActive(false);
            item.busy = false;
            item.routine = null;
        }

        Item Take()
        {
            for (int i = 0; i < _items.Length; i++)
            {
                _cursor = (_cursor + 1) % _items.Length;
                if (!_items[_cursor].busy) return _items[_cursor];
            }
            _cursor = (_cursor + 1) % _items.Length;
            return _items[_cursor];
        }

        /// <summary>Hides every popup. Called on restart.</summary>
        public void Clear()
        {
            if (_items == null) return;
            foreach (Item item in _items)
            {
                if (item.routine != null) StopCoroutine(item.routine);
                item.routine = null;
                item.busy = false;
                if (item.rect) item.rect.gameObject.SetActive(false);
            }
        }
    }
}
