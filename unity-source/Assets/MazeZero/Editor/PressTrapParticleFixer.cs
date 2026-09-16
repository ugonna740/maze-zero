#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MazeZero.Editor
{
    [InitializeOnLoad]
    public static class PressTrapParticleFixer
    {
        private const string PrefabPath = "Assets/MazeZero/preferbss/PressTrap.prefab";
        private const string MaterialFolder = "Assets/MazeZero/Materials";
        private const string MaterialPath = MaterialFolder + "/PressTrap Smoke URP.mat";
        private const string TexturePath = MaterialFolder + "/PressTrap Smoke Soft Particle.asset";

        static PressTrapParticleFixer() { EditorApplication.delayCall += Repair; }

        [MenuItem("Maze Zero/Repair PressTrap Smoke Material")]
        private static void Repair()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) return;
            EnsureFolder();
            var material = EnsureMaterial();
            if (material == null) return;

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            var renderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var particleRenderer in renderers)
            {
                particleRenderer.sharedMaterial = material;
                particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"MAZE//ZERO repaired {renderers.Length} PressTrap particle renderer(s) with a URP smoke material.");
        }

        private static Material EnsureMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                Debug.LogError("URP Particles/Unlit shader was not found.");
                return null;
            }
            if (material == null)
            {
                material = new Material(shader) { name = "PressTrap Smoke URP" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else material.shader = shader;

            var texture = EnsureSoftTexture();
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(.28f, .3f, .33f, .62f));
            if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(.28f, .3f, .33f, .62f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D EnsureSoftTexture()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture != null) return texture;
            const int size = 64;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "PressTrap Smoke Soft Particle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var uv = new Vector2((x + .5f) / size, (y + .5f) / size) * 2f - Vector2.one;
                var alpha = Mathf.Pow(Mathf.Clamp01(1f - uv.magnitude), 2.2f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            AssetDatabase.CreateAsset(texture, TexturePath);
            return texture;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/MazeZero", "Materials");
        }
    }
}
#endif
