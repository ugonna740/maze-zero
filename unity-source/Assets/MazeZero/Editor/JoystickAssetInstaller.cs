#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MazeZero.Editor
{
    [InitializeOnLoad]
    public static class JoystickAssetInstaller
    {
        private const string SourceFolder = "Assets/MazeZero/Simple Mobile Joystick/Simple Mobile Joystick";
        private const string ResourceFolder = "Assets/MazeZero/Resources/UI";

        static JoystickAssetInstaller()
        {
            EditorApplication.delayCall += Install;
        }

        [MenuItem("Maze Zero/Refresh Joystick Sprites")]
        private static void Install()
        {
            EnsureFolder("Assets/MazeZero/Resources", "UI");
            CopyAndConfigure("Move Joystick Background.png");
            CopyAndConfigure("Move Joystick Handle.png");
            AssetDatabase.SaveAssets();
            Debug.Log("MAZE//ZERO joystick sprites are ready.");
        }

        private static void CopyAndConfigure(string fileName)
        {
            var source = SourceFolder + "/" + fileName;
            var destination = ResourceFolder + "/" + fileName;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(source) == null) return;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(destination) == null)
                AssetDatabase.CopyAsset(source, destination);
            if (AssetImporter.GetAtPath(destination) is not TextureImporter importer) return;
            if (importer.textureType == TextureImporterType.Sprite && importer.alphaIsTransparency) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
