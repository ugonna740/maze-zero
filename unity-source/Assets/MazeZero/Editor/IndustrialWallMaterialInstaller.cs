using UnityEditor;
using UnityEngine;

namespace MazeZero.Editor
{
    [InitializeOnLoad]
    public static class IndustrialWallMaterialInstaller
    {
        private const string Source = "Assets/industrial_wall_02_1k/industrial_wall_02_";
        private const string ResourceFolder = "Assets/MazeZero/Resources";
        private const string MaterialPath = ResourceFolder + "/MazeZeroIndustrialWall.mat";

        static IndustrialWallMaterialInstaller() => EditorApplication.delayCall += InstallIfMissing;

        [MenuItem("Maze Zero/Materials/Rebuild Industrial Wall Material")]
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
            var baseColor = Load("baseColor_1k.png");
            if (baseColor == null) return;
            EnsureFolder("Assets/MazeZero", "Resources");
            ConfigureNormalMap(Source + "normal_gl_1k.png");

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return;
            var material = new Material(shader) { name = "Maze Zero Industrial Wall" };
            material.SetTexture("_BaseMap", baseColor);
            material.SetColor("_BaseColor", Color.white);
            material.SetTextureScale("_BaseMap", new Vector2(2f, 1f));

            SetTexture(material, "_BumpMap", Load("normal_gl_1k.png"), "_NORMALMAP");
            if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", 1f);
            SetTexture(material, "_MetallicGlossMap", Load("metallic_1k.png"), "_METALLICSPECGLOSSMAP");
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .75f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .4f);
            SetTexture(material, "_OcclusionMap", Load("ambientOcclusion_1k.png"), "_OCCLUSIONMAP");
            if (material.HasProperty("_OcclusionStrength")) material.SetFloat("_OcclusionStrength", 1f);
            SetTexture(material, "_ParallaxMap", Load("height_1k.png"), "_PARALLAXMAP");
            if (material.HasProperty("_Parallax")) material.SetFloat("_Parallax", .015f);

            var emission = Load("emissive_1k.png");
            if (emission != null && material.HasProperty("_EmissionMap"))
            {
                material.SetTexture("_EmissionMap", emission);
                material.SetColor("_EmissionColor", Color.white);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            AssetDatabase.CreateAsset(material, MaterialPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Maze Zero industrial wall material created and assigned to generated maze walls.");
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
