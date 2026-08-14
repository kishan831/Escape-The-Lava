using System.Collections;
using UnityEngine;

namespace EscapeTheLava
{
    /// <summary>
    /// Owns the rules from the brief: a 30 second round, 5 lives, collect every diamond to win, lose
    /// on the clock or on lives. Tiles animate, the HUD displays, this class decides.
    ///
    /// All wiring fields are public because the one-click scene builder assigns them from an editor
    /// script; nothing here expects to be hooked up by hand.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Data")]
        public GameConfig config;
        public GameAssets assets;

        [Header("Scene references (wired by the scene builder)")]
        public Camera worldCamera;
        public GridManager grid;
        public PointerInput pointer;
        public SpriteParticles particles;
        public AudioManager audioManager;
        public CameraShake cameraShake;
        public ScreenFlash screenFlash;
        public FloatingTextSpawner popups;
        public HudController hud;
        public BannerView banner;
        public EndScreen endScreen;
        public LavaRiseOverlay lavaRise;

        public GameState State { get; private set; } = GameState.Boot;
        public EndReason Reason { get; private set; } = EndReason.None;

        public int Score { get; private set; }
        public int Lives { get; private set; }
        public float TimeLeft { get; private set; }
        public int DiamondsCollected { get; private set; }
        public int ComboMultiplier { get; private set; } = 1;

        float _comboTimer;
        float _damageLockout;
        int _lastTickSecond;
        Coroutine _roundRoutine;

        void Awake()
        {
            // A leftover hit-stop from a previous play session must never survive into a new one.
            TimeFx.Reset();
        }

        void OnEnable()
        {
            if (pointer) pointer.Pressed += OnPressed;
            if (endScreen) endScreen.RestartRequested += Restart;
        }

        void OnDisable()
        {
            if (pointer) pointer.Pressed -= OnPressed;
            if (endScreen) endScreen.RestartRequested -= Restart;
        }

        void Start() => StartRound();

        // ------------------------------------------------------------------ round lifecycle

        public void StartRound()
        {
            if (_roundRoutine != null) StopCoroutine(_roundRoutine);
            _roundRoutine = StartCoroutine(RoundRoutine());
        }

        IEnumerator RoundRoutine()
        {
            TimeFx.Reset();
            State = GameState.Intro;
            Reason = EndReason.None;

            Score = 0;
            Lives = config.startingLives;
            TimeLeft = config.roundDuration;
            DiamondsCollected = 0;
            ComboMultiplier = 1;
            _comboTimer = 0f;
            _damageLockout = 0f;
            _lastTickSecond = Mathf.CeilToInt(TimeLeft);

            if (particles) particles.Clear();
            if (popups) popups.Clear();
            if (screenFlash) screenFlash.Clear();
            if (lavaRise) lavaRise.Reset();
            if (endScreen) endScreen.HideImmediate();

            grid.Build();

            if (hud)
            {
                hud.Bind(config);
                hud.SetScore(0, animate: false);
                hud.SetLives(Lives, animate: false);
                hud.SetDiamonds(0, grid.DiamondsTotal, animate: false);
                hud.SetTime(TimeLeft, false);
                hud.Show();
            }

            if (audioManager) audioManager.PlayWhoosh(0.85f);

            yield return grid.PlayIntro();

            if (banner) yield return banner.PlayCountIn(config.countInDuration);

            State = GameState.Playing;
        }

        void Update()
        {
            if (_damageLockout > 0f) _damageLockout -= Time.deltaTime;

            if (State != GameState.Playing) return;

            TickTimer();
            if (State != GameState.Playing) return;   // the timer may have ended the round
            TickCombo();
        }

        void TickTimer()
        {
            TimeLeft -= Time.deltaTime;

            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                if (hud) hud.SetTime(0f, true);
                Lose(EndReason.TimeUp);
                return;
            }

            bool low = TimeLeft <= config.lowTimeThreshold;
            if (hud) hud.SetTime(TimeLeft, low);

            // One tick per whole second, louder and higher once the clock is in the danger band.
            int second = Mathf.CeilToInt(TimeLeft);
            if (second == _lastTickSecond) return;
            _lastTickSecond = second;

            if (audioManager) audioManager.PlayTick(low);
            if (hud) hud.PulseTimer(low);
            if (low && screenFlash) screenFlash.Vignette(config.uiDanger, 0.22f, 0.5f);
        }

        void TickCombo()
        {
            // The window has to run down even at x1, otherwise a single early pickup would leave the
            // window permanently open and the next collect - minutes later - would still start a combo.
            if (_comboTimer <= 0f) return;

            _comboTimer -= Time.deltaTime;
            if (_comboTimer > 0f) return;

            if (ComboMultiplier <= 1) return;
            ComboMultiplier = 1;
            if (hud) hud.SetCombo(1);
        }

        // ------------------------------------------------------------------ input

        void OnPressed(Vector2 screenPosition)
        {
            // On the end screen, a tap anywhere off the button replays the round.
            if (State == GameState.Won || State == GameState.Lost)
            {
                if (endScreen && endScreen.IsInteractive) Restart();
                return;
            }

            if (State != GameState.Playing) return;

            Camera cam = worldCamera ? worldCamera : Camera.main;
            if (!cam) return;

            Vector3 world = cam.ScreenToWorldPoint(screenPosition);
            world.z = 0f;

            if (!grid.TryGetTile(world, out Tile tile)) return;

            switch (tile.Type)
            {
                case TileType.Diamond:
                    CollectDiamond((DiamondTile)tile);
                    break;

                case TileType.Lava:
                    HitLava((LavaTile)tile, world);
                    break;

                default:
                    // Brief: "Green Islands are purely safe visual tiles (tapping them does nothing)."
                    if (tile is IslandTile island) island.PlayHarmlessTap();
                    break;
            }
        }

        // ------------------------------------------------------------------ scoring

        void CollectDiamond(DiamondTile diamond)
        {
            if (diamond.IsCollected) return;

            Vector3 gemPosition = diamond.GemPosition;

            // Chain collects inside the combo window to build the multiplier.
            ComboMultiplier = _comboTimer > 0f
                ? Mathf.Min(ComboMultiplier + 1, config.maxComboMultiplier)
                : 1;
            _comboTimer = config.comboWindow;

            int gained = config.diamondScore * ComboMultiplier;
            Score += gained;
            DiamondsCollected++;
            grid.NotifyDiamondCollected();

            if (hud)
            {
                hud.SetScore(Score, animate: true);
                hud.SetDiamonds(DiamondsCollected, grid.DiamondsTotal, animate: true);
                hud.SetCombo(ComboMultiplier);
            }

            if (popups)
            {
                popups.SpawnAtWorld(gemPosition, $"+{gained}", config.diamondCore, 48);
                if (ComboMultiplier > 1)
                {
                    popups.SpawnAtWorld(gemPosition + new Vector3(0f, 0.45f, 0f),
                        $"x{ComboMultiplier} COMBO", config.uiAccent, 32, 70f, 0.8f);
                }
            }

            if (audioManager) audioManager.PlayCollect(ComboMultiplier);
            if (cameraShake) cameraShake.Shake(0.045f + ComboMultiplier * 0.008f, 0.15f);

            // Run the animation on the tile, not on this manager: a restart destroys the board and the
            // coroutine must die with it rather than keep poking at destroyed renderers.
            diamond.StartCoroutine(diamond.PlayCollect(FlyTarget(), particles));

            if (grid.DiamondsRemaining <= 0) Win();
        }

        /// <summary>World position of the HUD score counter, so collected gems fly to the number.</summary>
        Vector3 FlyTarget()
        {
            Camera cam = worldCamera ? worldCamera : Camera.main;
            if (hud && cam) return hud.ScoreAnchorWorldPosition(cam);
            return grid.TopCenter;
        }

        void HitLava(LavaTile lava, Vector3 hitPoint)
        {
            // Short immunity so one clumsy double-tap cannot burn two lives.
            if (_damageLockout > 0f) return;
            _damageLockout = config.damageLockout;

            Lives = Mathf.Max(0, Lives - 1);
            ComboMultiplier = 1;
            _comboTimer = 0f;

            if (hud)
            {
                hud.SetLives(Lives, animate: true);
                hud.SetCombo(1);
            }

            if (popups) popups.SpawnAtWorld(hitPoint, "-1 LIFE", config.uiDanger, 40, 95f, 0.85f);
            if (audioManager) audioManager.PlayDamage();
            if (cameraShake) cameraShake.Shake(0.34f, 0.6f);
            if (screenFlash)
            {
                screenFlash.Flash(new Color(1f, 0.25f, 0.1f), 0.42f, 0.4f);
                screenFlash.Vignette(config.uiDanger, 0.8f, 0.75f);
            }

            TimeFx.HitStop(0.22f, 0.07f);
            lava.StartCoroutine(lava.PlayHit(hitPoint, particles));

            if (Lives <= 0) Lose(EndReason.OutOfLives);
        }

        // ------------------------------------------------------------------ endings

        void Win()
        {
            if (State != GameState.Playing) return;

            State = GameState.Won;
            Reason = EndReason.AllDiamondsCollected;

            int timeBonus = Mathf.FloorToInt(TimeLeft) * config.timeBonusPerSecond;
            Score += timeBonus;
            if (hud) hud.SetScore(Score, animate: true);

            StartCoroutine(WinSequence(timeBonus));
        }

        IEnumerator WinSequence(int timeBonus)
        {
            if (audioManager) audioManager.PlayWin();
            if (cameraShake) cameraShake.Shake(0.12f, 0.2f);

            // Confetti sweeps across the top of the board.
            if (particles)
            {
                for (int i = 0; i < 5; i++)
                {
                    float x = Mathf.Lerp(-config.BoardSize.x * 0.42f, config.BoardSize.x * 0.42f, i / 4f);
                    particles.Emit(new Vector3(x, grid.TopCenter.y, 0f), FxPresets.WinConfetti(assets, config));
                    yield return new WaitForSeconds(0.07f);
                }
            }

            if (timeBonus > 0 && popups)
            {
                popups.SpawnAtScreen(new Vector2(Screen.width * 0.5f, Screen.height * 0.62f),
                    $"TIME BONUS +{timeBonus}", config.uiAccent, 44, 90f, 1.2f);
            }

            yield return new WaitForSeconds(0.55f);

            if (hud) hud.Hide();
            if (endScreen) endScreen.ShowWin(Score, DiamondsCollected, grid.DiamondsTotal, Lives, TimeLeft, timeBonus);
        }

        void Lose(EndReason reason)
        {
            if (State != GameState.Playing) return;

            State = GameState.Lost;
            Reason = reason;
            StartCoroutine(LoseSequence(reason));
        }

        IEnumerator LoseSequence(EndReason reason)
        {
            if (audioManager) audioManager.PlayLose();
            if (cameraShake) cameraShake.Shake(0.42f, 0.8f);
            if (screenFlash) screenFlash.Vignette(config.uiDanger, 0.9f, 1.1f);
            TimeFx.HitStop(0.3f, 0.14f);

            // The lava floods the screen: the theme delivering the bad news.
            if (lavaRise) yield return lavaRise.PlayRise(1.15f);
            else yield return new WaitForSeconds(0.6f);

            if (hud) hud.Hide();
            if (endScreen) endScreen.ShowLose(reason, Score, DiamondsCollected, grid.DiamondsTotal);
        }

        // ------------------------------------------------------------------ restart

        public void Restart()
        {
            if (State != GameState.Won && State != GameState.Lost) return;
            if (audioManager) audioManager.PlayUi();
            StartRound();
        }
    }
}
