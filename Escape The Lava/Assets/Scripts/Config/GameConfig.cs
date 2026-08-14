using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// Every tunable number for a round lives here so designers can balance the game
    /// without touching code. The asset is created by the one-click builder at
    /// Assets/Generated/GameConfig.asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Escape The Lava/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Grid")]
        [Tooltip("Brief requirement: 16 columns x 8 rows. Swap the two values for a portrait board.")]
        public int columns = 16;
        public int rows = 8;

        [Tooltip("World size of a single tile.")]
        public float tileSize = 1f;

        [Tooltip("Gap between tiles, as a fraction of tileSize.")]
        [Range(0f, 0.4f)] public float tileGap = 0.08f;

        [Tooltip("1 = a tap has to land on the visible tile and the gap between tiles is a dead zone. " +
                 "Above 1 the hit area grows into the gap; at 1 + tileGap the whole board is live.")]
        [Range(1f, 1.5f)] public float tapForgiveness = 1f;

        [Header("Rules")]
        [Tooltip("Brief requirement: 30 seconds per round.")]
        public float roundDuration = 30f;

        [Tooltip("Brief requirement: the player starts with 5 lives.")]
        public int startingLives = 5;

        [Tooltip("Number of diamonds to place. All of them must be collected to win.")]
        public int diamondCount = 18;

        [Tooltip("Seconds of tap immunity after touching lava, so one clumsy double-tap cannot cost two lives.")]
        public float damageLockout = 0.35f;

        [Header("Level generation")]
        [Tooltip("0 = a new random layout every round. Anything else is a fixed, reproducible layout.")]
        public int seed = 0;

        [Tooltip("Share of the board covered by lava before diamonds are placed.")]
        [Range(0.15f, 0.6f)] public float lavaCoverage = 0.34f;

        [Tooltip("Scale of the value noise that shapes the lava rivers. Lower = larger, smoother blobs.")]
        [Range(0.1f, 1f)] public float lavaNoiseScale = 0.32f;

        [Tooltip("How strongly diamonds prefer cells that touch lava. 0 = uniform, 1 = always the riskiest cells.")]
        [Range(0f, 1f)] public float diamondRiskBias = 0.7f;

        [Header("Scoring")]
        public int diamondScore = 100;

        [Tooltip("Seconds allowed between two collects to keep a combo alive.")]
        public float comboWindow = 1.2f;

        [Tooltip("Combo multiplier ceiling.")]
        public int maxComboMultiplier = 5;

        [Tooltip("Bonus score for each whole second left on the clock when the round is won.")]
        public int timeBonusPerSecond = 25;

        [Header("Timing / feel")]
        [Tooltip("Delay between neighbouring tiles during the board intro sweep.")]
        public float introStagger = 0.018f;

        public float introTileDuration = 0.42f;

        [Tooltip("Seconds the GO! banner stays on screen before the timer starts.")]
        public float countInDuration = 1.1f;

        [Tooltip("Seconds below which the timer starts flashing and beeping.")]
        public float lowTimeThreshold = 10f;

        [Header("Palette")]
        public Color backgroundTop = new Color(0.055f, 0.043f, 0.098f, 1f);
        public Color backgroundBottom = new Color(0.145f, 0.055f, 0.075f, 1f);

        public Color lavaDeep = new Color(0.42f, 0.055f, 0.035f, 1f);
        public Color lavaHot = new Color(1f, 0.35f, 0.06f, 1f);
        public Color lavaGlow = new Color(1f, 0.42f, 0.09f, 1f);

        public Color islandDeep = new Color(0.09f, 0.28f, 0.16f, 1f);
        public Color islandTop = new Color(0.28f, 0.72f, 0.34f, 1f);

        public Color diamondCore = new Color(0.42f, 0.85f, 1f, 1f);
        public Color diamondGlow = new Color(0.29f, 0.72f, 1f, 1f);

        public Color uiAccent = new Color(1f, 0.72f, 0.22f, 1f);
        public Color uiDanger = new Color(1f, 0.29f, 0.26f, 1f);

        /// <summary>Distance between two neighbouring tile centres.</summary>
        public float CellPitch => tileSize * (1f + tileGap);

        public int CellCount => columns * rows;

        /// <summary>Total board size in world units, including the gaps between tiles.</summary>
        public Vector2 BoardSize => new Vector2(
            columns * CellPitch - tileSize * tileGap,
            rows * CellPitch - tileSize * tileGap);

        void OnValidate()
        {
            columns = Mathf.Max(2, columns);
            rows = Mathf.Max(2, rows);
            // Leave at least a third of the board free so a layout is always solvable.
            diamondCount = Mathf.Clamp(diamondCount, 1, Mathf.Max(1, Mathf.RoundToInt(CellCount * 0.4f)));
            startingLives = Mathf.Max(1, startingLives);
            roundDuration = Mathf.Max(5f, roundDuration);
        }
    }
}
