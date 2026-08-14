using System.Collections.Generic;
using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// Base class for every board cell.
    ///
    /// Tiles intentionally have no <c>Update</c>. <see cref="GridManager"/> ticks all 128 of them
    /// from a single loop, which keeps the per-frame managed/native transitions down to one.
    /// Tiles also never touch game rules: they animate and report, and
    /// <see cref="GameManager"/> owns score, lives and win/lose.
    /// </summary>
    public abstract class Tile : MonoBehaviour
    {
        public TileType Type { get; protected set; }
        public int Column { get; private set; }
        public int Row { get; private set; }

        /// <summary>World-space centre of the tile.</summary>
        public Vector3 Center => transform.position;

        protected GameConfig Config;
        protected GameAssets Art;

        /// <summary>Per-tile animation offset so 128 tiles never pulse in lockstep.</summary>
        protected float Phase;

        protected SpriteRenderer Body;
        protected SpriteRenderer Face;

        readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>(6);
        readonly List<Color> _baseColors = new List<Color>(6);

        Vector3 _restScale;
        Coroutine _reaction;

        protected const int OrderBody = 10;
        protected const int OrderFace = 11;
        protected const int OrderDetail = 12;
        protected const int OrderGlow = 14;
        protected const int OrderContent = 16;
        protected const int OrderShine = 18;

        /// <summary>Creates the visuals and caches everything the idle animation needs.</summary>
        public void Initialize(GameConfig config, GameAssets art, int column, int row, Vector3 worldPosition)
        {
            Config = config;
            Art = art;
            Column = column;
            Row = row;

            transform.position = worldPosition;
            _restScale = Vector3.one * config.tileSize;
            transform.localScale = _restScale;

            // A hash of the cell keeps the animation offset stable across rebuilds of the same layout.
            Phase = Mathf.Repeat((column * 7.13f + row * 3.71f), 10f);

            Body = CreateRenderer("Body", art.tile, Color.white, OrderBody);
            Face = CreateRenderer("Face", art.tileFace, Color.white, OrderFace);
            Face.transform.localScale = Vector3.one * 0.84f;

            BuildVisuals();
            CacheBaseColors();
            SetIntroProgress(0f);
        }

        /// <summary>Subclass hook: add the type-specific decoration on top of body and face.</summary>
        protected abstract void BuildVisuals();

        /// <summary>Idle animation. <paramref name="time"/> is the shared board clock.</summary>
        public abstract void Tick(float time, float deltaTime);

        protected SpriteRenderer CreateRenderer(string name, Sprite sprite, Color color, int order,
            bool additive = false, Vector2 offset = default, float scale = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
            go.transform.localScale = Vector3.one * scale;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            // Explicit materials so tiles render identically with or without 2D lights in the scene.
            sr.sharedMaterial = additive ? Art.spriteAdditive : Art.spriteUnlit;
            return sr;
        }

        void CacheBaseColors()
        {
            _renderers.Clear();
            _baseColors.Clear();
            GetComponentsInChildren(true, _renderers);
            foreach (SpriteRenderer sr in _renderers) _baseColors.Add(sr.color);
        }

        /// <summary>
        /// Drives the board intro sweep. 0 = fully hidden, 1 = at rest. Called from a single
        /// coroutine in <see cref="GridManager"/> rather than one coroutine per tile.
        /// </summary>
        public void SetIntroProgress(float progress)
        {
            float eased = Easing.Evaluate(Easing.Type.BackOut, progress);
            transform.localScale = _restScale * eased;

            for (int i = 0; i < _renderers.Count; i++)
            {
                Color c = _baseColors[i];
                c.a *= Mathf.Clamp01(progress * 1.6f);
                _renderers[i].color = c;
            }
        }

        /// <summary>Squash-and-stretch reaction used by every tap, whatever the tile type.</summary>
        protected void PlayReaction(float squash, float duration)
        {
            if (_reaction != null) StopCoroutine(_reaction);
            _reaction = StartCoroutine(ReactionRoutine(squash, duration));
        }

        System.Collections.IEnumerator ReactionRoutine(float squash, float duration)
        {
            var squashed = new Vector3(_restScale.x * (1f + squash), _restScale.y * (1f - squash), _restScale.z);
            yield return Tween.Scale(transform, _restScale, squashed, duration * 0.25f, Easing.Type.QuadOut);
            yield return Tween.Scale(transform, squashed, _restScale, duration * 0.75f, Easing.Type.ElasticOut);
            transform.localScale = _restScale;
            _reaction = null;
        }

        /// <summary>Stops any reaction in flight and returns the tile to its rest pose.</summary>
        public virtual void ResetVisualState()
        {
            if (_reaction != null) StopCoroutine(_reaction);
            _reaction = null;
            transform.localScale = _restScale;
        }

        protected Vector3 RestScale => _restScale;

        /// <summary>Re-reads the current colours as the new baseline, e.g. after a tile changes type.</summary>
        protected void RefreshBaseColors() => CacheBaseColors();
    }
}
