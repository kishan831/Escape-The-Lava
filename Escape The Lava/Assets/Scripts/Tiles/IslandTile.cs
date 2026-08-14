using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// Green Island. Per the brief this is a purely safe visual tile and tapping it does nothing to
    /// the score, the timer or the lives. It still answers with a small squash so a tap never feels
    /// like the game dropped the input.
    /// </summary>
    public class IslandTile : Tile
    {
        SpriteRenderer _highlight;
        SpriteRenderer[] _pebbles;

        protected override void BuildVisuals()
        {
            Type = TileType.Island;

            Body.color = Config.islandDeep;
            Face.color = Config.islandTop;

            // Thin lighter band along the top edge, reads as grass catching the light.
            _highlight = CreateRenderer("Highlight", Art.tileFace,
                new Color(1f, 1f, 1f, 0.14f), OrderDetail, false, new Vector2(0f, 0.16f), 0.7f);
            _highlight.transform.localScale = new Vector3(0.7f, 0.28f, 1f);

            _pebbles = new SpriteRenderer[2];
            for (int i = 0; i < _pebbles.Length; i++)
            {
                var offset = new Vector2(
                    Mathf.Lerp(-0.2f, 0.2f, Mathf.PerlinNoise(Phase + i * 3.1f, 0.5f)),
                    Mathf.Lerp(-0.2f, 0.05f, Mathf.PerlinNoise(0.5f, Phase + i * 1.7f)));

                _pebbles[i] = CreateRenderer($"Pebble{i}", Art.glow,
                    new Color(0.06f, 0.2f, 0.12f, 0.5f), OrderDetail, false, offset, 0.16f);
            }
        }

        public override void Tick(float time, float deltaTime)
        {
            // A slow breath keeps the safe tiles alive without pulling focus from lava and diamonds.
            float breathe = 1f + Mathf.Sin(time * 1.1f + Phase) * 0.008f;
            Face.transform.localScale = Vector3.one * (0.84f * breathe);

            if (_highlight)
            {
                float shimmer = 0.1f + Mathf.PerlinNoise(Phase, time * 0.35f) * 0.1f;
                Color c = _highlight.color;
                c.a = shimmer;
                _highlight.color = c;
            }
        }

        /// <summary>Feedback-only response to a tap. Deliberately changes no game state.</summary>
        public virtual void PlayHarmlessTap()
        {
            PlayReaction(0.07f, 0.26f);
        }
    }
}
