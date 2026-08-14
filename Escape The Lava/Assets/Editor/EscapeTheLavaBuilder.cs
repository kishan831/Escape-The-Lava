using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EscapeTheLava.EditorTools
{
    /// <summary>
    /// One-click project setup.
    ///
    /// <c>Tools > Escape The Lava > Build Everything</c> generates all art, all audio, the config
    /// assets and the complete playable scene, then adds it to Build Settings. It is idempotent:
    /// running it again after changing <see cref="GameConfig"/> reproduces the scene from the new
    /// values instead of leaving half-updated objects behind.
    /// </summary>
    public static class EscapeTheLavaBuilder
    {
        const string Menu = "Tools/Escape The Lava/";

        [MenuItem(Menu + "Build Everything (One Click)", false, 0)]
        public static void BuildEverything()
        {
            // Never silently discard whatever the user had open.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RunFullBuild();
        }

        /// <summary>
        /// Entry point for headless runs:
        /// <c>Unity -batchmode -quit -executeMethod EscapeTheLava.EditorTools.EscapeTheLavaBuilder.BuildEverythingBatch</c>.
        /// Same work as the menu item, minus the save-changes prompt that has no meaning without a user.
        /// </summary>
        public static void BuildEverythingBatch() => RunFullBuild();

        static void RunFullBuild()
        {
            try
            {
                BuildPaths.EnsureFolder(BuildPaths.DataFolder);
                GameConfig config = BuildPaths.LoadOrCreate<GameConfig>(BuildPaths.ConfigAsset);
                GameAssets art = BuildPaths.LoadOrCreate<GameAssets>(BuildPaths.AssetsAsset);

                Step("Generating sprites", 0.1f);
                ArtGenerator.Generate(config, art);

                Step("Generating sound effects", 0.45f);
                AudioGenerator.Generate(art);

                Step("Saving assets", 0.7f);
                AssetDatabase.SaveAssets();

                if (!art.IsComplete)
                {
                    Debug.LogError("[Escape The Lava] Asset generation left gaps. The scene was not rebuilt.");
                    return;
                }

                Step("Building scene", 0.8f);
                SceneBuilder.Build(config, art);

                AssetDatabase.SaveAssets();

                Debug.Log($"[Escape The Lava] Build complete. Scene: {BuildPaths.ScenePath}\n" +
                          $"Board {config.columns}x{config.rows}, {config.diamondCount} diamonds, " +
                          $"{config.roundDuration:0}s, {config.startingLives} lives. Press Play.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem(Menu + "Rebuild Art Only", false, 20)]
        public static void RebuildArt()
        {
            try
            {
                GameConfig config = BuildPaths.LoadOrCreate<GameConfig>(BuildPaths.ConfigAsset);
                GameAssets art = BuildPaths.LoadOrCreate<GameAssets>(BuildPaths.AssetsAsset);

                Step("Generating sprites", 0.3f);
                ArtGenerator.Generate(config, art);
                AssetDatabase.SaveAssets();

                Debug.Log("[Escape The Lava] Sprites regenerated.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem(Menu + "Rebuild Audio Only", false, 21)]
        public static void RebuildAudio()
        {
            try
            {
                GameAssets art = BuildPaths.LoadOrCreate<GameAssets>(BuildPaths.AssetsAsset);

                Step("Generating sound effects", 0.3f);
                AudioGenerator.Generate(art);
                AssetDatabase.SaveAssets();

                Debug.Log("[Escape The Lava] Sound effects regenerated.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem(Menu + "Rebuild Scene Only", false, 22)]
        public static void RebuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            GameConfig config = BuildPaths.LoadOrCreate<GameConfig>(BuildPaths.ConfigAsset);
            GameAssets art = BuildPaths.LoadOrCreate<GameAssets>(BuildPaths.AssetsAsset);

            if (!art.IsComplete)
            {
                Debug.LogError("[Escape The Lava] Generated assets are missing. Run 'Build Everything' first.");
                return;
            }

            try
            {
                Step("Building scene", 0.5f);
                SceneBuilder.Build(config, art);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Escape The Lava] Scene rebuilt: {BuildPaths.ScenePath}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem(Menu + "Select Game Config", false, 40)]
        public static void SelectConfig()
        {
            GameConfig config = BuildPaths.LoadOrCreate<GameConfig>(BuildPaths.ConfigAsset);
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        static void Step(string label, float progress)
        {
            EditorUtility.DisplayProgressBar("Escape The Lava", label, progress);
        }
    }
}
