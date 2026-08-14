# Escape The Lava — Build Roadmap

Unity assignment: **FOG (Future of Gaming) — Round 1, Unity Developer**.
Source brief: `Escape The Lava/Assets/Doc/Round.pdf`.

---

## 1. Requirements extracted from the brief

| Requirement | Value | Where it lives |
| --- | --- | --- |
| Grid size | 16 columns x 8 rows (128 cells) | `GameConfig.columns/rows` |
| Tile types | Blue Diamond (collectible), Red Lava (danger), Green Island (safe) | `TileType`, `DiamondTile`, `LavaTile`, `IslandTile` |
| Round time | 30 seconds | `GameConfig.roundDuration` |
| Lives | 5 | `GameConfig.startingLives` |
| Tap diamond | Collect + add score | `GameManager.CollectDiamond` |
| Tap lava | Lose 1 life | `GameManager.HitLava` |
| Tap island | Nothing (safe visual) | `IslandTile.OnTapped` |
| Win | All diamonds collected before timer hits 0 | `GameManager.Win` |
| Game over | Timer reaches 0 **or** all 5 lives lost | `GameManager.Lose` |
| Idle animation — lava | Bubbling + glow pulse | `LavaTile.Tick` |
| Idle animation — diamond | Float + shine sparkle | `DiamondTile.Tick` |
| Score animation | Floating popup at the exact click position | `FloatingTextSpawner` |
| Damage animation | Splash / burn burst on lava tap | `LavaTile.PlayHit` + `SpriteParticles` |
| Game over sequence | Dedicated animated win/lose screen | `EndScreen`, `LavaRiseOverlay` |
| UI — timer | 30s countdown | `HudController` |
| UI — lives | 5 heart icons | `HeartsView` |
| UI — score | Score + diamonds collected | `HudController` |

### Note on one ambiguity in the brief

The brief says both `16x8 (rows: 16, cols: 8)` and `Grid Size: 16 columns x 8 rows`.
The implementation follows the explicit **Grid Size** requirement line (16 columns x 8 rows,
landscape) and keeps both numbers as serialized fields, so the layout can be flipped to
8 x 16 (portrait) from `GameConfig` without touching code.

---

## 2. Technical decisions

| Decision | Reason |
| --- | --- |
| Unity 6000.3.14f1 + URP 2D (Renderer2D) — already configured | Matches the project as delivered |
| **No external packages, no imported art or audio** | Every sprite and sound effect is generated procedurally by an editor script, so the repo clones and runs with zero missing references |
| Input System package (project is set to `activeInputHandler: 1`) | Legacy `Input.*` is disabled in this project; `PointerInput` uses `UnityEngine.InputSystem.Pointer`, which covers mouse **and** touch |
| Math-based hit testing (world point -> cell index) instead of 128 `Collider2D`s | Exact, allocation-free, no physics setup |
| Tiles ticked by `GridManager` in one loop instead of 128 `Update()` calls | Fewer managed/native transitions |
| Hand-rolled `Tween` / `Easing` coroutine helpers | No DOTween dependency; keeps the repo self-contained |
| Pooled `SpriteParticles` instead of `ParticleSystem` | Fully code-driven and tunable, no serialized module soup |
| Legacy `UnityEngine.UI.Text` with the built-in `LegacyRuntime.ttf` | TextMeshPro needs a manual "Import TMP Essentials" step; the built-in font makes the one-click setup truly one click |
| Restart resets state in place instead of reloading the scene | Instant replay, no load hitch |

---

## 3. Phases

### Phase 0 — Recon *(done)*
Read the brief, confirm Unity version, URP 2D renderer, package list, and the active
input handler.

### Phase 1 — Foundation
`GameConfig` / `GameAssets` ScriptableObjects, `Easing`, `Tween`, `SpriteParticles`,
`PointerInput`, `CameraFitter`.

### Phase 2 — Grid & level
`LevelGenerator` (value-noise lava rivers, islands, diamonds biased towards lava edges
for tension), `GridManager` (build, index, hit test, tick, staggered intro), `Tile`
hierarchy.

### Phase 3 — Rules
`GameManager` state machine: `Boot -> Intro -> Playing -> Won | Lost`. Timer, lives,
score, combo multiplier, damage lockout, win/lose evaluation, restart.

### Phase 4 — Feel
Idle animations, collect pop + fly-to-HUD, lava splash + embers, hit-stop, camera shake,
red screen flash, floating score popups, procedural SFX.

### Phase 5 — UI
HUD (timer / hearts / score / diamond counter), round-start banner, animated end screen,
rising-lava loss overlay, restart button.

### Phase 6 — One-click integration
`Tools > Escape The Lava > Build Everything`: generates art, generates audio, creates
config assets, builds `Assets/Scenes/Game.unity` end to end, adds it to Build Settings,
opens it. Re-runnable and idempotent.

### Phase 7 — Polish pass
Bloom volume, vignette, tuning, README, gameplay capture.

---

## 4. One-click setup

1. Open the project in Unity 6000.3.14f1 and let it compile.
2. `Tools > Escape The Lava > Build Everything (One Click)`.
3. Press Play.

Sub-commands (all re-runnable, used while iterating):

- `Tools > Escape The Lava > Rebuild Art Only`
- `Tools > Escape The Lava > Rebuild Audio Only`
- `Tools > Escape The Lava > Rebuild Scene Only`

Everything the builder writes lives under `Assets/Generated/` (art, audio, materials,
config assets) plus `Assets/Scenes/Game.unity`, so a rebuild never clobbers hand-authored
files.

---

## 5. Layout

```
Assets/
  Art/Shaders/AdditiveSprite.shader   hand-written, used for every glow
  Editor/
    EscapeTheLavaBuilder.cs           menu items + orchestration
    ArtGenerator.cs                   procedural sprite PNGs
    AudioGenerator.cs                 procedural WAV SFX
    SceneBuilder.cs                   scene graph + UI hierarchy
  Generated/                          created by the builder (art, audio, assets)
  Scenes/Game.unity                   created by the builder
  Scripts/
    Config/    GameConfig, GameAssets
    Core/      GameManager, GridManager, LevelGenerator, GameState
    Tiles/     Tile, LavaTile, DiamondTile, IslandTile, TileType
    Input/     PointerInput
    Feedback/  Easing, Tween, SpriteParticles, CameraShake, ScreenFlash,
               FloatingTextSpawner, AudioManager, CameraFitter, TimeFx
    UI/        HudController, HeartsView, EndScreen, LavaRiseOverlay, BannerView
```

---

## 6. Deliverables checklist

- [ ] GitHub URL — public repo, clean and commented
- [ ] Output video — full playthrough showing every animation, a win, and a loss
