using UnityEditor;
using UnityEngine;

namespace MazeZero.Editor
{
    [InitializeOnLoad]
    public static class GroundTileMaterialInstaller
    {
        private const string Source = "Assets/bricks_wall_04_1k/bricks_wall_04_";
        private const string ResourceFolder = "Assets/MazeZero/Resources";
        private const string MaterialPath = ResourceFolder + "/MazeZeroGroundTiles.mat";

        static GroundTileMaterialInstaller() => EditorApplication.delayCall += InstallIfMissing;

        [MenuItem("Maze Zero/Materials/Rebuild Ground Tile Material")]
        public static void Rebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null)
                AssetDatabase.DeleteAsset(MaterialPath);
            CreateMaterial();
        }

        private static void InstallIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) == null) CreateMaterial();
        }

        private static void CreateMaterial()
        {
            var baseColor = Load("color_1k.png");
            if (baseColor == null) return;
            EnsureFolder("Assets/MazeZero", "Resources");
            ConfigureNormalMap(Source + "normal_gl_1k.png");

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return;
            var material = new Material(shader) { name = "Maze Zero Brick Ground" };
            material.SetTexture("_BaseMap", baseColor);
            material.SetColor("_BaseColor", Color.white);

            SetTexture(material, "_BumpMap", Load("normal_gl_1k.png"), "_NORMALMAP");
            if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", 1f);
            SetTexture(material, "_OcclusionMap", Load("ambient_occlusion_1k.png"), "_OCCLUSIONMAP");
            if (material.HasProperty("_OcclusionStrength")) material.SetFloat("_OcclusionStrength", 1f);
            SetTexture(material, "_ParallaxMap", Load("height_1k.png"), "_PARALLAXMAP");
            if (material.HasProperty("_Parallax")) material.SetFloat("_Parallax", .012f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .05f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .28f);

            AssetDatabase.CreateAsset(material, MaterialPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Maze Zero brick ground material created and assigned to the generated ground.");
        }

        private static Texture2D Load(string suffix) => AssetDatabase.LoadAssetAtPath<Texture2D>(Source + suffix);

        private static void SetTexture(Material material, string property, Texture texture, string keyword)
        {
            if (texture == null || !material.HasProperty(property)) return;
            material.SetTexture(property, texture);
            material.EnableKeyword(keyword);
        }

        private static void ConfigureNormalMap(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer || importer.textureType == TextureImporterType.NormalMap) return;
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string parent, string name)
        {
            var path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
