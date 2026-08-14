using System;
using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// Builds a board layout.
    ///
    /// Random scatter looks like static, so the lava is shaped with value noise: that produces
    /// connected rivers and pools with clean island shorelines. Diamonds are then placed with a
    /// weighted draw that favours cells touching lava, which is what makes precision tapping feel
    /// risky instead of trivial.
    /// </summary>
    public static class LevelGenerator
    {
        public struct Result
        {
            public TileType[,] cells;
            public int diamondCount;
            public int lavaCount;
            public int seed;
        }

        public static Result Generate(GameConfig config, int seedOverride = 0)
        {
            int seed = seedOverride != 0 ? seedOverride
                     : config.seed != 0 ? config.seed
                     : UnityEngine.Random.Range(1, int.MaxValue);

            var rng = new System.Random(seed);
            int columns = config.columns;
            int rows = config.rows;

            var cells = new TileType[columns, rows];
            var noise = new float[columns * rows];

            // Two octaves of Perlin noise, sampled from a seeded offset so every seed is a new map.
            float offsetX = (float)rng.NextDouble() * 500f;
            float offsetY = (float)rng.NextDouble() * 500f;
            float scale = Mathf.Max(0.05f, config.lavaNoiseScale);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    float nx = offsetX + x * scale;
                    float ny = offsetY + y * scale;
                    float value = Mathf.PerlinNoise(nx, ny) * 0.68f
                                + Mathf.PerlinNoise(nx * 2.7f, ny * 2.7f) * 0.32f;
                    noise[y * columns + x] = value;
                }
            }

            // Pick the threshold from the sorted values so the lava share matches lavaCoverage exactly,
            // regardless of how the noise happened to fall.
            var sorted = (float[])noise.Clone();
            Array.Sort(sorted);
            int lavaTarget = Mathf.Clamp(
                Mathf.RoundToInt(sorted.Length * config.lavaCoverage), 1, sorted.Length - config.diamondCount - 1);
            float threshold = sorted[sorted.Length - lavaTarget];

            int lavaCount = 0;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    bool isLava = noise[y * columns + x] >= threshold;
                    cells[x, y] = isLava ? TileType.Lava : TileType.Island;
                    if (isLava) lavaCount++;
                }
            }

            int placed = PlaceDiamonds(config, cells, rng);

            return new Result
            {
                cells = cells,
                diamondCount = placed,
                lavaCount = lavaCount,
                seed = seed
            };
        }

        /// <summary>Weighted draw without replacement over every island cell.</summary>
        static int PlaceDiamonds(GameConfig config, TileType[,] cells, System.Random rng)
        {
            int columns = config.columns;
            int rows = config.rows;

            int candidateCount = 0;
            var candidateX = new int[columns * rows];
            var candidateY = new int[columns * rows];
            var weights = new float[columns * rows];

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (cells[x, y] != TileType.Island) continue;

                    int adjacentLava = CountAdjacentLava(cells, columns, rows, x, y);

                    // riskBias 0 -> every island cell is equally likely.
                    // riskBias 1 -> a cell with 4+ lava neighbours is ~5x more likely than an inland cell.
                    float weight = 1f + config.diamondRiskBias * adjacentLava;

                    candidateX[candidateCount] = x;
                    candidateY[candidateCount] = y;
                    weights[candidateCount] = weight;
                    candidateCount++;
                }
            }

            int target = Mathf.Min(config.diamondCount, candidateCount);
            int placed = 0;

            for (int i = 0; i < target; i++)
            {
                float total = 0f;
                for (int c = 0; c < candidateCount; c++) total += weights[c];
                if (total <= 0f) break;

                float roll = (float)rng.NextDouble() * total;
                int chosen = candidateCount - 1;
                for (int c = 0; c < candidateCount; c++)
                {
                    roll -= weights[c];
                    if (roll > 0f) continue;
                    chosen = c;
                    break;
                }

                cells[candidateX[chosen], candidateY[chosen]] = TileType.Diamond;
                weights[chosen] = 0f;                      // drawn, cannot be picked again
                DampenNeighbours(candidateX, candidateY, weights, candidateCount, chosen);
                placed++;
            }

            return placed;
        }

        /// <summary>
        /// Halves the weight of cells next to a freshly placed diamond so the gems spread out over
        /// the board instead of clumping into one corner.
        /// </summary>
        static void DampenNeighbours(int[] xs, int[] ys, float[] weights, int count, int chosen)
        {
            int cx = xs[chosen];
            int cy = ys[chosen];

            for (int c = 0; c < count; c++)
            {
                if (weights[c] <= 0f) continue;
                int dx = Mathf.Abs(xs[c] - cx);
                int dy = Mathf.Abs(ys[c] - cy);
                if (dx <= 1 && dy <= 1) weights[c] *= 0.25f;
                else if (dx <= 2 && dy <= 2) weights[c] *= 0.6f;
            }
        }

        static int CountAdjacentLava(TileType[,] cells, int columns, int rows, int x, int y)
        {
            int count = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= columns || ny >= rows) continue;
                    if (cells[nx, ny] == TileType.Lava) count++;
                }
            }
            return count;
        }
    }
}
