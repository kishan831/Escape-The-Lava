using System.Collections;
using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// Blue Diamond. Inherits the island body on purpose: a diamond sits on safe ground, so once it
    /// has been collected the very same tile simply becomes a Green Island with no object swap.
    ///
    /// Idle animation (brief requirement): the gem floats, breathes, counter-rotates and throws a
    /// periodic sparkle.
    /// </summary>
    public class DiamondTile : IslandTile
    {
        public bool IsCollected { get; private set; }

        /// <summary>World position of the gem itself, which is where the score popup belongs.</summary>
        public Vector3 GemPosition => _gem ? _gem.transform.position : Center;

        SpriteRenderer _gem;
        SpriteRenderer _gemGlow;
        SpriteRenderer _sparkle;

        float _sparkleTimer;
        float _sparkleDuration;
        Vector3 _gemRestLocalPosition;

        protected override void BuildVisuals()
        {
            base.BuildVisuals();       // island ground under the gem
            Type = TileType.Diamond;

            _gemGlow = CreateRenderer("GemGlow", Art.glow,
                new Color(Config.diamondGlow.r, Config.diamondGlow.g, Config.diamondGlow.b, 0.55f),
                OrderGlow, additive: true, scale: 1.15f);

            _gem = CreateRenderer("Gem", Art.diamond, Color.white, OrderContent, false, default, 0.62f);

            _sparkle = CreateRenderer("Sparkle", Art.sparkle,
                new Color(1f, 1f, 1f, 0f), OrderShine, additive: true,
                new Vector2(0.14f, 0.16f), 0.28f);

            _gemRestLocalPosition = _gem.transform.localPosition;
            _sparkleTimer = Random.Range(0.4f, 2.4f);
        }

        public override void Tick(float time, float deltaTime)
        {
            base.Tick(time, deltaTime);
            if (IsCollected) return;

            // Float, breathe and gently rock.
            float bob = Mathf.Sin(time * 2.1f + Phase) * 0.055f;
            float breathe = 1f + Mathf.Sin(time * 3.2f + Phase * 1.7f) * 0.05f;
            float tilt = Mathf.Sin(time * 1.4f + Phase) * 7f;

            _gem.transform.localPosition = _gemRestLocalPosition + new Vector3(0f, bob, 0f);
            _gem.transform.localScale = Vector3.one * (0.62f * breathe);
            _gem.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);

            // Glow pulses out of phase with the breath so the two never look like one animation.
            float glowPulse = 0.42f + Mathf.Sin(time * 2.6f + Phase * 2.3f) * 0.18f;
            _gemGlow.color = new Color(Config.diamondGlow.r, Config.diamondGlow.g, Config.diamondGlow.b, glowPulse);
            _gemGlow.transform.localPosition = new Vector3(0f, bob * 0.6f, 0f);
            _gemGlow.transform.localScale = Vector3.one * (1.15f + Mathf.Sin(time * 2.6f + Phase * 2.3f) * 0.12f);

            TickSparkle(deltaTime, bob);
        }

        /// <summary>Occasional shine: a four-point sparkle that scales up, spins and vanishes.</summary>
        void TickSparkle(float deltaTime, float bob)
        {
            _sparkleTimer -= deltaTime;

            if (_sparkleTimer <= 0f)
            {
                _sparkleTimer = Random.Range(1.6f, 3.4f);
                _sparkleDuration = 0.42f;
                _sparkle.transform.localPosition = new Vector3(
                    Random.Range(-0.16f, 0.16f), Random.Range(-0.1f, 0.2f), 0f);
            }

            if (_sparkleDuration <= 0f)
            {
                _sparkle.color = new Color(1f, 1f, 1f, 0f);
                return;
            }

            _sparkleDuration -= deltaTime;
            float t = Mathf.Clamp01(1f - _sparkleDuration / 0.42f);
            float envelope = Mathf.Sin(t * Mathf.PI);          // 0 -> 1 -> 0

            _sparkle.color = new Color(1f, 1f, 1f, envelope);
            _sparkle.transform.localScale = Vector3.one * (0.12f + envelope * 0.26f);
            _sparkle.transform.localRotation = Quaternion.Euler(0f, 0f, t * 90f);
            _sparkle.transform.localPosition += new Vector3(0f, bob * 0.02f, 0f);
        }

        /// <summary>
        /// Collect animation. The gem bursts, arcs towards <paramref name="flyTarget"/> (the score
        /// counter in the HUD) and pops on arrival, then the tile settles into a plain island.
        /// </summary>
        public IEnumerator PlayCollect(Vector3 flyTarget, SpriteParticles fx)
        {
            if (IsCollected) yield break;
            IsCollected = true;
            Type = TileType.Island;

            Vector3 start = _gem.transform.position;

            if (fx)
            {
                fx.Emit(start, FxPresets.DiamondHalo(Art, Config));
                fx.Emit(start, FxPresets.DiamondShards(Art, Config));
            }

            PlayReaction(0.16f, 0.34f);
            _sparkle.color = new Color(1f, 1f, 1f, 0f);

            // Flash the ground white, then fade back to the island palette.
            StartCoroutine(GroundFlash());

            // Detach from the tile so the parent squash does not drag the gem around mid-flight.
            _gem.transform.SetParent(transform.parent, true);
            _gemGlow.color = new Color(_gemGlow.color.r, _gemGlow.color.g, _gemGlow.color.b, 0f);

            Vector3 control = Vector3.Lerp(start, flyTarget, 0.4f) + new Vector3(0f, 1.6f, 0f);
            float duration = 0.42f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float eased = Easing.Evaluate(Easing.Type.QuadInOut, t);
                float u = 1f - eased;

                _gem.transform.position = u * u * start + 2f * u * eased * control + eased * eased * flyTarget;
                _gem.transform.localScale = Vector3.one * Mathf.Lerp(0.78f, 0.18f, eased);
                _gem.transform.localRotation = Quaternion.Euler(0f, 0f, eased * 420f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (fx)
            {
                var pop = FxPresets.DiamondShards(Art, Config);
                pop.count = 8;
                pop.speedMin = 1.2f; pop.speedMax = 3f;
                pop.sizeMin = 0.07f; pop.sizeMax = 0.14f;
                fx.Emit(flyTarget, pop);
            }

            _gem.gameObject.SetActive(false);
            _gemGlow.gameObject.SetActive(false);
        }

        IEnumerator GroundFlash()
        {
            Color flash = Color.Lerp(Config.islandTop, Color.white, 0.8f);
            yield return Tween.Tint(Face, Config.islandTop, flash, 0.07f, Easing.Type.QuadOut);
            yield return Tween.Tint(Face, flash, Config.islandTop, 0.32f, Easing.Type.QuadIn);
        }

        /// <summary>A collected diamond behaves exactly like an island from then on.</summary>
        public override void PlayHarmlessTap()
        {
            if (IsCollected) base.PlayHarmlessTap();
        }
    }
}
