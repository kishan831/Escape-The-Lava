using System.IO;
using UnityEditor;
using UnityEngine;

namespace EscapeTheLava.EditorTools
{
    /// <summary>Shared asset locations for the one-click builder.</summary>
    public static class BuildPaths
    {
        public const string Root = "Assets/Generated";
        public const string ArtFolder = Root + "/Art";
        public const string AudioFolder = Root + "/Audio";
        public const string MaterialFolder = Root + "/Materials";
        public const string DataFolder = Root + "/Data";

        public const string ConfigAsset = DataFolder + "/GameConfig.asset";
        public const string AssetsAsset = DataFolder + "/GameAssets.asset";
        public const string VolumeProfileAsset = DataFolder + "/GameVolumeProfile.asset";

        public const string SceneFolder = "Assets/Scenes";
        public const string ScenePath = SceneFolder + "/Game.unity";

        /// <summary>Absolute path to the folder that contains Assets/, used for raw File IO.</summary>
        public static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        /// <summary>Creates a project-relative folder and every missing parent.</summary>
        public static void EnsureFolder(string projectRelativePath)
        {
            if (AssetDatabase.IsValidFolder(projectRelativePath)) return;

            string[] parts = projectRelativePath.Split('/');
            string current = parts[0];                       // "Assets"

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(next))
                {
                    // The folder can exist on disk while the AssetDatabase view is stale. Importing it
                    // instead of creating it avoids ending up with "Data 1", "Data 2" siblings.
                    if (Directory.Exists(Path.Combine(ProjectRoot, next)))
                        AssetDatabase.ImportAsset(next, ImportAssetOptions.ForceUpdate);
                    else
                        AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        /// <summary>Loads a ScriptableObject, creating it if the builder has not run before.</summary>
        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
