# Escape The Lava

A 2D arcade board game built for the **FOG (Future of Gaming) Round 1 Unity Developer assignment**.
A 16x8 grid of lava, islands and diamonds: collect every diamond in 30 seconds without touching the
lava, and you keep your five lives.

Unity **6000.3.14f1**, URP 2D. Unity project root: `Escape The Lava/`.

---

## Run it

1. Open `Escape The Lava/` in Unity 6000.3.14f1 and wait for the initial compile.
2. Menu: **Tools > Escape The Lava > Build Everything (One Click)**.
3. Press **Play**.

That single command generates every sprite, every sound effect, the config assets, and the complete
playable scene at `Assets/Scenes/Game.unity`, then adds it to Build Settings. Nothing has to be
dragged into an inspector by hand.

Headless equivalent, for CI:

```bash
Unity -batchmode -quit -projectPath "Escape The Lava" \
      -executeMethod EscapeTheLava.EditorTools.EscapeTheLavaBuilder.BuildEverythingBatch
```

### Other menu commands

| Command | Use |
| --- | --- |
| `Rebuild Art Only` | Re-render the sprites after editing `ArtGenerator` |
| `Rebuild Audio Only` | Re-synthesise the sound effects |
| `Rebuild Scene Only` | Rebuild the scene after changing `GameConfig` |
| `Select Game Config` | Jump to the tuning asset |

---

## Controls

Click or tap a tile. Mouse, pen and touch all work through the same code path.

| Tile | Result |
| --- | --- |
| Blue Diamond | Collected, scores, builds the combo multiplier |
| Red Lava | Costs one life |
| Green Island | Nothing (safe tile) |

---

## Rules

| Rule | Value |
| --- | --- |
| Board | 16 columns x 8 rows |
| Round time | 30 seconds |
| Lives | 5 |
| Diamonds | 18 per round, all of them required to win |
| Win | Every diamond collected before the timer hits 0 |
| Game over | Timer reaches 0, or all 5 lives are gone |

Extras layered on top of the brief: a combo multiplier for chained pickups (up to x5), a time bonus
on a win, and a short immunity window after taking damage so one clumsy double-tap cannot cost two
lives. Every number lives in `Assets/Generated/Data/GameConfig.asset`.

---

## No binary assets

The repository contains no imported art or audio. `ArtGenerator` rasterises every sprite from
signed-distance fields and `AudioGenerator` synthesises every sound effect as a 16-bit WAV, both at
build time into `Assets/Generated/`. The project therefore clones and runs with zero missing
references, and there are no third-party assets to license.

## Animation and polish

- **Lava idle** — Perlin-driven surface heat, drifting hot spots, rising bubbles, additive halo
- **Diamond idle** — float, breathe, rock, periodic eight-point sparkle
- **Collect** — shard burst, ground flash, and the gem arcs to the score counter in the HUD
- **Damage** — molten splash, rising smoke, expanding shockwave, scorch mark, camera shake, red
  flash and vignette, plus a 70 ms hit-stop
- **Score popup** — floating label at the exact tapped pixel, with the combo multiplier above it
- **Board intro** — diagonal scale-in sweep, then a two-beat objective banner
- **Win** — confetti sweep across the top of the board, then an animated results panel
- **Loss** — the lava floods up the screen with a wobbling surface before the panel appears
- **Post-processing** — bloom, vignette and colour adjustments on a generated volume profile

---

## Layout

```
Escape The Lava/Assets/
  Art/Shaders/AdditiveSprite.shader   hand-written premultiplied additive sprite shader
  Doc/Round.pdf                       the assignment brief
  Editor/                             one-click builder: art, audio, scene
  Generated/                          produced by the builder (art, audio, materials, data)
  Scenes/Game.unity                   produced by the builder
  Scripts/
    Config/    GameConfig, GameAssets
    Core/      GameManager, GridManager, LevelGenerator, GameState
    Tiles/     Tile, IslandTile, DiamondTile, LavaTile, TileType
    Input/     PointerInput
    Feedback/  Easing, Tween, SpriteParticles, FxPresets, CameraShake, CameraFitter,
               ScreenFlash, FloatingTextSpawner, AudioManager, TimeFx
    UI/        HudController, HeartsView, BannerView, EndScreen, LavaRiseOverlay
```

See [ROADMAP.md](ROADMAP.md) for the requirement-by-requirement breakdown and the technical
decisions behind each one.

---

## Notes on the brief

The brief states both `16x8 (rows: 16, cols: 8)` and `Grid Size: 16 columns x 8 rows`. The
implementation follows the explicit **Grid Size** line (16 columns x 8 rows, landscape). Both values
are serialized fields, so flipping to a portrait 8 x 16 board is a change in `GameConfig` plus a
`Rebuild Scene Only`, with no code edit.
