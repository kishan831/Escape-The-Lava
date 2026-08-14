using System.Collections;
using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// Owns the board: builds it, indexes it, resolves taps to cells and ticks every tile's idle
    /// animation from one loop.
    ///
    /// Tap resolution is arithmetic (world point -> cell index) rather than 128 <c>Collider2D</c>
    /// hits, so there is no physics setup, no allocation per tap and no ambiguity about which cell
    /// was pressed.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Wired by the scene builder")]
        public GameConfig config;
        public GameAssets assets;
        public SpriteParticles particles;

        /// <summary>Number of uncollected diamonds still on the board.</summary>
        public int DiamondsRemaining { get; private set; }

        public int DiamondsTotal { get; private set; }
        public int CurrentSeed { get; private set; }

        Tile[,] _tiles;
        Transform _board;
        float _boardTime;
        bool _ticking;
        float _moteTimer;

        /// <summary>Discards the current board and generates a fresh layout.</summary>
        public void Build(int seedOverride = 0)
        {
            Clear();

            if (_board == null)
            {
                _board = new GameObject("Board").transform;
                _board.SetParent(transform, false);
            }

            LevelGenerator.Result level = LevelGenerator.Generate(config, seedOverride);
            CurrentSeed = level.seed;

            _tiles = new Tile[config.columns, config.rows];
            DiamondsTotal = level.diamondCount;
            DiamondsRemaining = level.diamondCount;

            for (int y = 0; y < config.rows; y++)
            {
                for (int x = 0; x < config.columns; x++)
                {
                    _tiles[x, y] = CreateTile(level.cells[x, y], x, y);
                }
            }

            _boardTime = 0f;
            _ticking = false;
        }

        Tile CreateTile(TileType type, int x, int y)
        {
            var go = new GameObject($"Tile_{x}_{y}");
            go.transform.SetParent(_board, false);

            Tile tile;
            switch (type)
            {
                case TileType.Lava:
                    var lava = go.AddComponent<LavaTile>();
                    lava.Particles = particles;
                    tile = lava;
                    break;
                case TileType.Diamond:
                    tile = go.AddComponent<DiamondTile>();
                    break;
                default:
                    tile = go.AddComponent<IslandTile>();
                    break;
            }

            tile.Initialize(config, assets, x, y, CellToWorld(x, y));
            return tile;
        }

        public void Clear()
        {
            if (_board != null)
            {
                for (int i = _board.childCount - 1; i >= 0; i--) Destroy(_board.GetChild(i).gameObject);
            }
            _tiles = null;
            DiamondsRemaining = 0;
            DiamondsTotal = 0;
            _ticking = false;
        }

        /// <summary>World-space centre of a cell. The board is centred on the origin.</summary>
        public Vector3 CellToWorld(int x, int y)
        {
            float pitch = config.CellPitch;
            return new Vector3(
                (x - (config.columns - 1) * 0.5f) * pitch,
                (y - (config.rows - 1) * 0.5f) * pitch,
                0f);
        }

        /// <summary>
        /// Resolves a world point to a tile. Returns false for points that fall outside the board or
        /// into the gap between tiles (widened by <see cref="GameConfig.tapForgiveness"/>).
        /// </summary>
        public bool TryGetTile(Vector3 worldPoint, out Tile tile)
        {
            tile = null;
            if (_tiles == null) return false;

            float pitch = config.CellPitch;
            float fx = worldPoint.x / pitch + (config.columns - 1) * 0.5f;
            float fy = worldPoint.y / pitch + (config.rows - 1) * 0.5f;

            int x = Mathf.RoundToInt(fx);
            int y = Mathf.RoundToInt(fy);
            if (x < 0 || y < 0 || x >= config.columns || y >= config.rows) return false;

            // Reject presses that land in the gap, so a tap next to lava is never mistaken for a hit.
            float half = 0.5f * config.tileSize * config.tapForgiveness / pitch;
            if (Mathf.Abs(fx - x) > half || Mathf.Abs(fy - y) > half) return false;

            tile = _tiles[x, y];
            return tile != null;
        }

        /// <summary>Called by <see cref="GameManager"/> once a diamond has actually been collected.</summary>
        public void NotifyDiamondCollected()
        {
            DiamondsRemaining = Mathf.Max(0, DiamondsRemaining - 1);
        }

        /// <summary>Staggered entrance sweep: tiles scale in diagonally across the board.</summary>
        public IEnumerator PlayIntro()
        {
            if (_tiles == null) yield break;

            int columns = config.columns;
            int rows = config.rows;
            float stagger = config.introStagger;
            float tileDuration = Mathf.Max(0.05f, config.introTileDuration);

            // Diagonal wave: delay grows with (x + y), so the board unfolds from the bottom-left.
            float lastDelay = (columns - 1 + rows - 1) * stagger;
            float total = lastDelay + tileDuration;
            float elapsed = 0f;

            while (elapsed < total)
            {
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        float delay = (x + y) * stagger;
                        float progress = Mathf.Clamp01((elapsed - delay) / tileDuration);
                        _tiles[x, y].SetIntroProgress(progress);
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                    _tiles[x, y].SetIntroProgress(1f);

            _ticking = true;
        }

        /// <summary>Idle animations keep playing on the end screen, so this is not gated on state.</summary>
        void Update()
        {
            if (!_ticking || _tiles == null) return;

            float dt = Time.deltaTime;
            _boardTime += dt;

            for (int y = 0; y < config.rows; y++)
                for (int x = 0; x < config.columns; x++)
                    _tiles[x, y].Tick(_boardTime, dt);

            TickAmbientMotes(dt);
        }

        /// <summary>Sparse embers drifting up across the whole board.</summary>
        void TickAmbientMotes(float dt)
        {
            if (!particles) return;

            _moteTimer -= dt;
            if (_moteTimer > 0f) return;
            _moteTimer = Random.Range(0.06f, 0.16f);

            Vector2 size = config.BoardSize;
            var position = new Vector3(
                Random.Range(-size.x * 0.55f, size.x * 0.55f),
                Random.Range(-size.y * 0.6f, size.y * 0.6f),
                0f);

            particles.EmitSingle(position, FxPresets.AmbientMote(assets, config));
        }

        /// <summary>Top edge of the board in world space. Used to launch the win confetti.</summary>
        public Vector3 TopCenter => new Vector3(0f, config.BoardSize.y * 0.5f + 0.6f, 0f);
    }
}
