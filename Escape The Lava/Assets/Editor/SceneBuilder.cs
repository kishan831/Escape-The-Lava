using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EscapeTheLava.EditorTools
{
    /// <summary>
    /// Builds the entire playable scene from scratch: camera, post-processing volume, background,
    /// systems, and the whole UI hierarchy, fully wired.
    ///
    /// The point is reproducibility. Running the menu command once on a clean clone produces exactly
    /// the same scene as it did here, and re-running it after a tweak to <see cref="GameConfig"/>
    /// rebuilds the layout without any manual dragging in the inspector.
    /// </summary>
    public static class SceneBuilder
    {
        const float ReferenceWidth = 1920f;
        const float ReferenceHeight = 1080f;

        public static void Build(GameConfig config, GameAssets art)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = BuildCamera(config);
            BuildVolume();
            BuildBackground(config, art);

            // --- systems ------------------------------------------------------------------------
            var systems = new GameObject("Systems").transform;

            var particles = new GameObject("Particles").AddComponent<SpriteParticles>();
            particles.transform.SetParent(systems, false);

            var audio = new GameObject("Audio").AddComponent<AudioManager>();
            audio.transform.SetParent(systems, false);
            audio.assets = art;

            var pointer = new GameObject("PointerInput").AddComponent<PointerInput>();
            pointer.transform.SetParent(systems, false);

            var grid = new GameObject("GridManager").AddComponent<GridManager>();
            grid.transform.SetParent(systems, false);
            grid.config = config;
            grid.assets = art;
            grid.particles = particles;

            // --- ui -----------------------------------------------------------------------------
            Canvas canvas = BuildCanvas();
            var canvasRect = (RectTransform)canvas.transform;

            ScreenFlash flash = BuildScreenFx(canvasRect, art);
            LavaRiseOverlay lavaRise = BuildLavaRise(canvasRect, art, config, audio);
            HudController hud = BuildHud(canvasRect, art, config);
            FloatingTextSpawner popups = BuildPopups(canvasRect, art, camera);
            BannerView banner = BuildBanner(canvasRect, art, config, audio);
            EndScreen endScreen = BuildEndScreen(canvasRect, art, config, audio, particles);

            BuildEventSystem();

            // --- game manager, wired last so every reference exists -----------------------------
            var manager = new GameObject("GameManager").AddComponent<GameManager>();
            manager.transform.SetParent(systems, false);
            manager.config = config;
            manager.assets = art;
            manager.worldCamera = camera;
            manager.grid = grid;
            manager.pointer = pointer;
            manager.particles = particles;
            manager.audioManager = audio;
            manager.cameraShake = camera.GetComponent<CameraShake>();
            manager.screenFlash = flash;
            manager.popups = popups;
            manager.hud = hud;
            manager.banner = banner;
            manager.endScreen = endScreen;
            manager.lavaRise = lavaRise;

            BuildPaths.EnsureFolder(BuildPaths.SceneFolder);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BuildPaths.ScenePath);

            RegisterInBuildSettings();
        }

        // ------------------------------------------------------------------ camera & rendering

        static Camera BuildCamera(GameConfig config)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";

            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = config.backgroundTop;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            go.AddComponent<AudioListener>();

            // Post-processing has to be on for the bloom in the volume profile to reach the screen.
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.renderShadows = false;
            data.antialiasing = AntialiasingMode.None;

            go.AddComponent<CameraShake>();

            CameraFitter fitter = go.AddComponent<CameraFitter>();
            fitter.config = config;

            return camera;
        }

        static void BuildVolume()
        {
            VolumeProfile profile = BuildVolumeProfile();

            var go = new GameObject("Global Volume");
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.profile = profile;
        }

        /// <summary>
        /// Recreated from scratch every build so re-running the command cannot stack duplicate
        /// overrides onto the profile.
        /// </summary>
        static VolumeProfile BuildVolumeProfile()
        {
            BuildPaths.EnsureFolder(BuildPaths.DataFolder);
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(BuildPaths.VolumeProfileAsset) != null)
            {
                AssetDatabase.DeleteAsset(BuildPaths.VolumeProfileAsset);
            }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, BuildPaths.VolumeProfileAsset);

            // Bloom is what turns the additive glows into actual heat.
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.15f);
            bloom.threshold.Override(0.75f);
            bloom.scatter.Override(0.68f);
            bloom.tint.Override(new Color(1f, 0.92f, 0.85f));

            var vignette = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.45f);

            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(10f);
            color.contrast.Override(8f);
            color.postExposure.Override(0.1f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        /// <summary>Layered backdrop: flat fill, a warm bloom behind the board, then a dark edge falloff.</summary>
        static void BuildBackground(GameConfig config, GameAssets art)
        {
            var root = new GameObject("Background").transform;

            AddBackgroundSprite(root, "Fill", art.solid, art.spriteUnlit,
                config.backgroundTop, new Vector3(80f, 50f, 1f), -200);

            AddBackgroundSprite(root, "Heat", art.glow, art.spriteAdditive,
                new Color(config.lavaGlow.r, config.lavaGlow.g, config.lavaGlow.b, 0.16f),
                new Vector3(34f, 20f, 1f), -190);

            // Sized close to the visible area so the dark ring actually lands on the screen edges.
            AddBackgroundSprite(root, "EdgeFalloff", art.vignette, art.spriteUnlit,
                new Color(config.backgroundBottom.r * 0.4f, config.backgroundBottom.g * 0.4f,
                    config.backgroundBottom.b * 0.4f, 0.85f),
                new Vector3(26f, 16f, 1f), -180);
        }

        static void AddBackgroundSprite(Transform parent, string name, Sprite sprite, Material material,
            Color color, Vector3 scale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = material;
            sr.color = color;
            sr.sortingOrder = order;
        }

        // ------------------------------------------------------------------ canvas & event system

        static Canvas BuildCanvas()
        {
            var go = new GameObject("UI Canvas", typeof(RectTransform));

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();

            // This project runs the Input System package only, so the legacy StandaloneInputModule
            // would silently do nothing. InputSystemUIInputModule is the matching module.
            var module = go.AddComponent<InputSystemUIInputModule>();

            // Point it at the action asset that ships with the project template; the module resolves
            // its UI actions (Point, Click, Submit, ...) from the asset by name.
            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            if (actions != null) module.actionsAsset = actions;
        }

        // ------------------------------------------------------------------ screen effects

        static ScreenFlash BuildScreenFx(RectTransform canvas, GameAssets art)
        {
            RectTransform root = UiFactory.Rect("ScreenFx", canvas);
            UiFactory.Stretch(root);

            Image vignette = UiFactory.Image(root, "Vignette", art.vignette, new Color(1f, 0f, 0f, 0f));
            UiFactory.Stretch((RectTransform)vignette.transform);
            vignette.raycastTarget = false;

            Image flash = UiFactory.Image(root, "Flash", art.solid, new Color(1f, 1f, 1f, 0f));
            UiFactory.Stretch((RectTransform)flash.transform);
            flash.raycastTarget = false;

            ScreenFlash component = root.gameObject.AddComponent<ScreenFlash>();
            component.vignetteImage = vignette;
            component.flashImage = flash;
            return component;
        }

        static LavaRiseOverlay BuildLavaRise(RectTransform canvas, GameAssets art, GameConfig config,
            AudioManager audio)
        {
            RectTransform group = UiFactory.Rect("LavaRise", canvas);
            UiFactory.Stretch(group);

            Image heat = UiFactory.Image(group, "HeatGlow", art.vignette, new Color(1f, 0.42f, 0.12f, 0f));
            UiFactory.Stretch((RectTransform)heat.transform);
            heat.raycastTarget = false;

            // Taller than any screen so the top edge is always off-screen when it starts.
            RectTransform root = UiFactory.Rect("Root", group);
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(1f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(0f, ReferenceHeight * 1.4f);
            root.anchoredPosition = new Vector2(0f, -ReferenceHeight * 1.4f);

            Image body = UiFactory.Image(root, "Body", art.solid, config.lavaDeep);
            UiFactory.Stretch((RectTransform)body.transform);
            body.raycastTarget = false;

            Image surfaceImage = UiFactory.Image(root, "Surface", art.lavaSurface, config.lavaHot);
            var surface = (RectTransform)surfaceImage.transform;
            surface.anchorMin = new Vector2(0f, 1f);
            surface.anchorMax = new Vector2(1f, 1f);
            surface.pivot = new Vector2(0.5f, 0f);
            surface.sizeDelta = new Vector2(120f, 110f);
            surface.anchoredPosition = Vector2.zero;
            surfaceImage.raycastTarget = false;

            LavaRiseOverlay overlay = group.gameObject.AddComponent<LavaRiseOverlay>();
            overlay.root = root;
            overlay.body = body;
            overlay.surface = surface;
            overlay.surfaceImage = surfaceImage;
            overlay.heatGlow = heat;
            overlay.config = config;
            overlay.audioManager = audio;
            return overlay;
        }

        // ------------------------------------------------------------------ hud

        static HudController BuildHud(RectTransform canvas, GameAssets art, GameConfig config)
        {
            RectTransform root = UiFactory.Rect("HUD", canvas);
            UiFactory.Stretch(root);

            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            HudController hud = root.gameObject.AddComponent<HudController>();
            hud.group = group;

            BuildTimer(root, art, config, hud);
            BuildScore(root, art, config, hud);
            BuildDiamondCounter(root, art, config, hud);
            BuildLives(root, art, config, hud);

            // Combo readout, just under the timer.
            Text combo = UiFactory.Text(root, "Combo", art.font, "", 56, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Color(1f, 1f, 1f, 0f));
            UiFactory.Place((RectTransform)combo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -240f), new Vector2(520f, 70f));
            hud.comboText = combo;

            return hud;
        }

        static void BuildTimer(RectTransform parent, GameAssets art, GameConfig config, HudController hud)
        {
            RectTransform root = UiFactory.Rect("Timer", parent);
            UiFactory.Place(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f),
                new Vector2(420f, 200f));

            Text label = UiFactory.Text(root, "Label", art.font, "TIME", 26, FontStyle.Bold,
                TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.55f));
            UiFactory.Place((RectTransform)label.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -6f), new Vector2(420f, 34f));

            Text value = UiFactory.Text(root, "Value", art.font, "30", 92, FontStyle.Bold,
                TextAnchor.UpperCenter, Color.white);
            UiFactory.Place((RectTransform)value.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -36f), new Vector2(420f, 110f));
            UiFactory.AddOutline(value, 3f);

            Image barBackground = UiFactory.Image(root, "BarBg", art.panel, new Color(0.1f, 0.08f, 0.12f, 0.85f));
            barBackground.type = Image.Type.Sliced;
            UiFactory.Place((RectTransform)barBackground.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -156f), new Vector2(340f, 20f));

            Image barFill = UiFactory.Image(root, "BarFill", art.panel, config.uiAccent);
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFill.fillAmount = 1f;
            UiFactory.Place((RectTransform)barFill.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -156f), new Vector2(340f, 20f));

            hud.timerRoot = root;
            hud.timerText = value;
            hud.timerBar = barFill;
        }

        static void BuildScore(RectTransform parent, GameAssets art, GameConfig config, HudController hud)
        {
            RectTransform root = UiFactory.Rect("Score", parent);
            UiFactory.Place(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -30f),
                new Vector2(460f, 130f));

            Text label = UiFactory.Text(root, "Label", art.font, "SCORE", 28, FontStyle.Bold,
                TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.55f));
            UiFactory.Place((RectTransform)label.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(4f, -2f), new Vector2(460f, 34f));

            Text value = UiFactory.Text(root, "Value", art.font, "0", 72, FontStyle.Bold,
                TextAnchor.UpperLeft, config.uiAccent);
            UiFactory.Place((RectTransform)value.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -30f), new Vector2(460f, 92f));
            UiFactory.AddOutline(value, 3f);

            // Invisible marker: collected diamonds fly to this point.
            RectTransform anchor = UiFactory.Rect("FlyAnchor", root);
            UiFactory.Place(anchor, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(70f, -72f),
                new Vector2(10f, 10f));

            hud.scoreRoot = root;
            hud.scoreText = value;
            hud.scoreAnchor = anchor;
        }

        static void BuildDiamondCounter(RectTransform parent, GameAssets art, GameConfig config, HudController hud)
        {
            RectTransform root = UiFactory.Rect("Diamonds", parent);
            UiFactory.Place(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(44f, -162f),
                new Vector2(460f, 74f));

            Image icon = UiFactory.Image(root, "Icon", art.diamond, Color.white);
            UiFactory.Place((RectTransform)icon.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(4f, -4f), new Vector2(52f, 60f));

            Text value = UiFactory.Text(root, "Value", art.font, "0 / 0", 44, FontStyle.Bold,
                TextAnchor.MiddleLeft, config.diamondCore);
            UiFactory.Place((RectTransform)value.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(66f, -34f), new Vector2(390f, 64f));
            UiFactory.AddOutline(value, 3f);

            hud.diamondRoot = root;
            hud.diamondText = value;
        }

        static void BuildLives(RectTransform parent, GameAssets art, GameConfig config, HudController hud)
        {
            RectTransform root = UiFactory.Rect("Lives", parent);
            UiFactory.Place(root, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-44f, -30f),
                new Vector2(440f, 130f));

            Text label = UiFactory.Text(root, "Label", art.font, "LIVES", 28, FontStyle.Bold,
                TextAnchor.UpperRight, new Color(1f, 1f, 1f, 0.55f));
            UiFactory.Place((RectTransform)label.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-4f, -2f), new Vector2(440f, 34f));

            RectTransform row = UiFactory.Rect("Row", root);
            UiFactory.Place(row, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -44f),
                new Vector2(config.startingLives * 76f, 76f));

            var hearts = new Image[config.startingLives];
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i] = UiFactory.Image(row, $"Heart{i}", art.heart, config.uiDanger);
                // Laid out right to left so the last heart lost is the leftmost one.
                UiFactory.Place((RectTransform)hearts[i].transform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(-38f - (hearts.Length - 1 - i) * 76f, 0f), new Vector2(64f, 64f));
            }

            HeartsView view = row.gameObject.AddComponent<HeartsView>();
            view.hearts = hearts;
            view.config = config;
            hud.hearts = view;
        }

        // ------------------------------------------------------------------ popups, banner, end screen

        static FloatingTextSpawner BuildPopups(RectTransform canvas, GameAssets art, Camera camera)
        {
            RectTransform root = UiFactory.Rect("Popups", canvas);
            UiFactory.Stretch(root);

            FloatingTextSpawner spawner = root.gameObject.AddComponent<FloatingTextSpawner>();
            spawner.assets = art;
            spawner.canvasRect = canvas;
            spawner.worldCamera = camera;
            return spawner;
        }

        static BannerView BuildBanner(RectTransform canvas, GameAssets art, GameConfig config, AudioManager audio)
        {
            RectTransform root = UiFactory.Rect("Banner", canvas);
            UiFactory.Stretch(root);

            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            RectTransform box = UiFactory.Rect("Box", root);
            UiFactory.Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f),
                new Vector2(1400f, 320f));

            Text headline = UiFactory.Text(box, "Headline", art.font, "GO!", 104, FontStyle.Bold,
                TextAnchor.MiddleCenter, config.uiAccent);
            UiFactory.Place((RectTransform)headline.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f), new Vector2(1400f, 160f));
            UiFactory.AddOutline(headline, 5f);

            Text subline = UiFactory.Text(box, "Subline", art.font, "", 48, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.8f));
            UiFactory.Place((RectTransform)subline.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -60f), new Vector2(1400f, 90f));
            UiFactory.AddOutline(subline, 3f);

            BannerView view = root.gameObject.AddComponent<BannerView>();
            view.group = group;
            view.headline = headline;
            view.subline = subline;
            view.audioManager = audio;
            return view;
        }

        static EndScreen BuildEndScreen(RectTransform canvas, GameAssets art, GameConfig config,
            AudioManager audio, SpriteParticles particles)
        {
            RectTransform root = UiFactory.Rect("EndScreen", canvas);
            UiFactory.Stretch(root);

            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            Image backdrop = UiFactory.Image(root, "Backdrop", art.solid, new Color(0.02f, 0.01f, 0.04f, 0f));
            UiFactory.Stretch((RectTransform)backdrop.transform);

            Image panelImage = UiFactory.Image(root, "Panel", art.panel, new Color(0.07f, 0.06f, 0.1f, 0.96f));
            panelImage.type = Image.Type.Sliced;
            var panel = (RectTransform)panelImage.transform;
            UiFactory.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(820f, 660f));

            Text headline = UiFactory.Text(panel, "Headline", art.font, "ESCAPED!", 92, FontStyle.Bold,
                TextAnchor.UpperCenter, config.diamondCore);
            UiFactory.Place((RectTransform)headline.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -44f), new Vector2(780f, 120f));
            UiFactory.AddOutline(headline, 5f);

            Text subline = UiFactory.Text(panel, "Subline", art.font, "", 34, FontStyle.Normal,
                TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0f));
            UiFactory.Place((RectTransform)subline.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -152f), new Vector2(780f, 50f));

            var statLines = new Text[4];
            for (int i = 0; i < statLines.Length; i++)
            {
                statLines[i] = UiFactory.Text(panel, $"Stat{i}", art.font, "", 36, FontStyle.Bold,
                    TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0f));
                UiFactory.Place((RectTransform)statLines[i].transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -230f - i * 64f), new Vector2(760f, 58f));
            }

            Image buttonImage = UiFactory.Image(panel, "RestartButton", art.panel, config.uiAccent);
            buttonImage.type = Image.Type.Sliced;
            var buttonRect = (RectTransform)buttonImage.transform;
            UiFactory.Place(buttonRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f),
                new Vector2(380f, 104f));

            // UiFactory turns raycasts off by default; the one interactive element needs them back on.
            buttonImage.raycastTarget = true;

            Button button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.88f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Text buttonLabel = UiFactory.Text(buttonRect, "Label", art.font, "PLAY AGAIN", 42, FontStyle.Bold,
                TextAnchor.MiddleCenter, new Color(0.09f, 0.06f, 0.04f, 1f));
            UiFactory.Stretch((RectTransform)buttonLabel.transform);

            EndScreen endScreen = root.gameObject.AddComponent<EndScreen>();
            endScreen.group = group;
            endScreen.backdrop = backdrop;
            endScreen.panel = panel;
            endScreen.panelImage = panelImage;
            endScreen.headline = headline;
            endScreen.subline = subline;
            endScreen.statLines = statLines;
            endScreen.restartButton = button;
            endScreen.restartLabel = buttonLabel;
            endScreen.config = config;
            endScreen.audioManager = audio;
            endScreen.particles = particles;
            endScreen.assets = art;
            return endScreen;
        }

        // ------------------------------------------------------------------ build settings

        static void RegisterInBuildSettings()
        {
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene entry in existing)
            {
                if (entry.path == BuildPaths.ScenePath) return;
            }

            var updated = new EditorBuildSettingsScene[existing.Length + 1];
            // The game scene goes first so a build boots straight into it.
            updated[0] = new EditorBuildSettingsScene(BuildPaths.ScenePath, true);
            for (int i = 0; i < existing.Length; i++) updated[i + 1] = existing[i];

            EditorBuildSettings.scenes = updated;
        }
    }
}
