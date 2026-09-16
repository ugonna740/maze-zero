#if UNITY_EDITOR
using System.IO;
using MazeZero;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MazeZero.Editor
{
    [InitializeOnLoad]
    public static class CartoonUiInstaller
    {
        private const string Folder = "Assets/MazeZero/Resources/CartoonUI";
        private const string FontPath = Folder + "/Fredoka.ttf";
        private const string FontAssetPath = Folder + "/Fredoka SDF.asset";
        static CartoonUiInstaller() { EditorApplication.delayCall += InitializeWithoutOverwritingScene; }
        [MenuItem("Maze Zero/UI/Rebuild Cartoon HUD")]
        public static void RebuildHud()
        {
            EnsureAssets();
            foreach (var hud in Object.FindObjectsByType<PracticeHud>(FindObjectsInactive.Include, FindObjectsSortMode.None)) hud.RebuildVisuals();
            SaveOpenScene();
            Debug.Log("Maze Zero cartoon HUD rebuilt by explicit menu command.");
        }

        [MenuItem("Maze Zero/UI/Install Horizontal Compass Only")]
        public static void InstallHorizontalCompassOnly()
        {
            EnsureAssets();
            var changed = false;
            foreach (var hud in Object.FindObjectsByType<PracticeHud>(FindObjectsInactive.Include, FindObjectsSortMode.None)) changed |= hud.ReplaceCompassWithLinear();
            if (changed) SaveOpenScene();
        }

        private static void InitializeWithoutOverwritingScene()
        {
            EnsureAssets();
            // One surgical migration for the requested compass; all other manual UI edits remain untouched.
            var changed = false;
            foreach (var hud in Object.FindObjectsByType<PracticeHud>(FindObjectsInactive.Include, FindObjectsSortMode.None)) changed |= hud.ReplaceCompassWithLinear();
            foreach (var map in Object.FindObjectsByType<MiniMap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) changed |= map.EnsureTapToggle();
            if (changed) SaveOpenScene();
        }

        private static void EnsureAssets()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            ConfigurePackSprites();
            var source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            var missingAtlas = existing != null && (existing.atlasTextures == null || existing.atlasTextures.Length == 0 || existing.atlasTextures[0] == null);
            if (source != null && (existing == null || missingAtlas))
            {
                if (existing != null) AssetDatabase.DeleteAsset(FontAssetPath);
                var fontAsset = TMP_FontAsset.CreateFontAsset(source);
                fontAsset.name = "Fredoka SDF";
                AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
                foreach (var atlas in fontAsset.atlasTextures)
                    if (atlas != null && !AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material)) AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();
            }
            Debug.Log("Maze Zero UI assets are ready; serialized scene UI was not rebuilt.");
        }

        private static void SaveOpenScene()
        {
            if (Application.isPlaying || !UnityEngine.SceneManagement.SceneManager.GetActiveScene().IsValid()) return;
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        private static void ConfigurePackSprites()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder + "/ChequeredInk" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var filename = Path.GetFileNameWithoutExtension(path);
                importer.spriteBorder = filename.Contains("Panel") || filename.Contains("Popup") || filename.Contains("Button") || filename.Contains("Gauge")
                    ? new Vector4(18, 18, 18, 18) : Vector4.zero;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
