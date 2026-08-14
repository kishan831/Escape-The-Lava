using System.Collections;
using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// Red Lava. Tapping it costs one life.
    ///
    /// Idle animation (brief requirement): the surface bubbles and the glow breathes. Both are driven
    /// by Perlin noise offset per tile, so no two cells ever pulse together and the board looks like
    /// one continuous molten sheet instead of 40 copies of the same loop.
    /// </summary>
    public class LavaTile : Tile
    {
        SpriteRenderer _glow;
        SpriteRenderer[] _cracks;
        SpriteRenderer _scorch;

        float _bubbleTimer;

        /// <summary>Set by <see cref="GridManager"/> so the tile can emit its own bubbles.</summary>
        public SpriteParticles Particles;

        protected override void BuildVisuals()
        {
            Type = TileType.Lava;

            Body.color = Config.lavaDeep;
            Face.color = Config.lavaHot;

            // Additive halo. This is what the bloom in the volume profile picks up.
            _glow = CreateRenderer("Glow", Art.glow,
                new Color(Config.lavaGlow.r, Config.lavaGlow.g, Config.lavaGlow.b, 0.4f),
                OrderGlow, additive: true, scale: 1.3f);

            // Two hot spots inside the tile that drift and pulse: the "bubbling" read.
            _cracks = new SpriteRenderer[2];
            for (int i = 0; i < _cracks.Length; i++)
            {
                _cracks[i] = CreateRenderer($"Crack{i}", Art.glow,
                    new Color(1f, 0.8f, 0.35f, 0.5f), OrderDetail, additive: true, default, 0.3f);
            }

            // Darkened crust that briefly appears where the player was burned.
            _scorch = CreateRenderer("Scorch", Art.tileFace,
                new Color(0.1f, 0.05f, 0.05f, 0f), OrderShine, false, default, 0.84f);

            _bubbleTimer = Random.Range(0.2f, 2.2f);
        }

        public override void Tick(float time, float deltaTime)
        {
            // Surface heat: two noise samples at different speeds so the colour never cycles visibly.
            float heat = Mathf.PerlinNoise(Phase, time * 0.55f) * 0.7f
                       + Mathf.PerlinNoise(Phase * 2.3f + 11f, time * 1.35f) * 0.3f;

            Face.color = Color.Lerp(Config.lavaDeep, Config.lavaHot, 0.25f + heat * 0.75f);
            Face.transform.localScale = Vector3.one * (0.84f + heat * 0.02f);

            float glowPulse = 0.24f + heat * 0.42f;
            _glow.color = new Color(Config.lavaGlow.r, Config.lavaGlow.g, Config.lavaGlow.b, glowPulse);
            _glow.transform.localScale = Vector3.one * (1.22f + heat * 0.18f);

            TickCracks(time, heat);
            TickBubbles(deltaTime);
        }

        void TickCracks(float time, float heat)
        {
            for (int i = 0; i < _cracks.Length; i++)
            {
                float seed = Phase + i * 5.31f;
                float x = (Mathf.PerlinNoise(seed, time * 0.4f) - 0.5f) * 0.42f;
                float y = (Mathf.PerlinNoise(seed + 7.7f, time * 0.45f) - 0.5f) * 0.42f;

                // Each hot spot swells and dies on its own cycle.
                float life = Mathf.PerlinNoise(seed + 21f, time * 0.9f);
                float size = 0.14f + life * 0.3f;

                _cracks[i].transform.localPosition = new Vector3(x, y, 0f);
                _cracks[i].transform.localScale = Vector3.one * size;
                _cracks[i].color = new Color(1f, 0.72f + heat * 0.2f, 0.28f, 0.2f + life * 0.5f);
            }
        }

        void TickBubbles(float deltaTime)
        {
            if (!Particles) return;

            _bubbleTimer -= deltaTime;
            if (_bubbleTimer > 0f) return;

            // Sparse on purpose: 40-ish lava tiles each popping every ~2s already fills the board.
            _bubbleTimer = Random.Range(1.4f, 3.6f);
            Particles.EmitSingle(Center + new Vector3(0f, -0.15f, 0f), FxPresets.LavaBubble(Art, Config));
        }

        /// <summary>
        /// Damage animation (brief requirement): expanding shockwave ring, molten splash, rising
        /// smoke and a scorch mark that cools off.
        /// </summary>
        public IEnumerator PlayHit(Vector3 hitPoint, SpriteParticles fx)
        {
            PlayReaction(0.2f, 0.4f);

            if (fx)
            {
                fx.Emit(hitPoint, FxPresets.LavaSplash(Art, Config));
                fx.Emit(hitPoint, FxPresets.LavaSmoke(Art, Config));
            }

            // Owned by this tile so a board rebuild cancels them instead of leaving them to touch
            // renderers that no longer exist.
            StartCoroutine(Shockwave(hitPoint));
            StartCoroutine(Scorch());

            // Blow the glow out hard, then settle. Tick() takes the colour back over afterwards.
            yield return Tween.Value(0.5f, Easing.Type.ExpoOut, t =>
            {
                float burst = Mathf.Lerp(1.1f, 0.3f, t);
                _glow.color = new Color(1f, 0.62f, 0.24f, burst);
                _glow.transform.localScale = Vector3.one * Mathf.Lerp(2.1f, 1.3f, t);
            });
        }

        /// <summary>Expanding ring, spawned as a throwaway renderer so several can overlap.</summary>
        IEnumerator Shockwave(Vector3 position)
        {
            // Parented to the board rather than to this tile, so the tap squash cannot scale it,
            // and so a board rebuild still cleans it up.
            var go = new GameObject("Shockwave");
            go.transform.SetParent(transform.parent, true);
            go.transform.position = position;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.ring;
            sr.sharedMaterial = Art.spriteAdditive;
            sr.sortingOrder = 65;

            yield return Tween.Value(0.45f, Easing.Type.ExpoOut, t =>
            {
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 2.9f, t);
                sr.color = new Color(1f, 0.55f, 0.2f, (1f - t) * 0.9f);
            });

            Destroy(go);
        }

        IEnumerator Scorch()
        {
            yield return Tween.Value(0.08f, Easing.Type.QuadOut,
                t => _scorch.color = new Color(0.08f, 0.03f, 0.03f, t * 0.75f));
            yield return Tween.Value(0.9f, Easing.Type.QuadIn,
                t => _scorch.color = new Color(0.08f, 0.03f, 0.03f, (1f - t) * 0.75f));
            _scorch.color = new Color(0.08f, 0.03f, 0.03f, 0f);
        }
    }
}
