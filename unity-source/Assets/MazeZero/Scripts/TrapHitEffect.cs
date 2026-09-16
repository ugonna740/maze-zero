using UnityEngine;
using UnityEngine.Rendering;

namespace MazeZero
{
    // A short-lived, unlit spark burst. No lights, texture downloads or prefab edits.
    public sealed class TrapHitEffect : MonoBehaviour
    {
        private Material ownedMaterial;

        public static void Spawn(Vector3 position, Transform owner)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) return;
            var effect = new GameObject("Trap Hit Sparks");
            effect.SetActive(false);
            effect.transform.SetParent(owner, false);
            effect.transform.position = position;
            var cleanup = effect.AddComponent<TrapHitEffect>();
            var particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = .15f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.2f, .45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(.06f, .13f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .5f, .12f), new Color(1f, .9f, .5f));
            main.gravityModifier = .65f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 16;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .08f;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            cleanup.ownedMaterial = new Material(shader) { name = "Trap Hit Sparks (Runtime)" };
            var material = cleanup.ownedMaterial;
            material.SetColor("_BaseColor", new Color(1f, .8f, .3f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            effect.SetActive(true);
            particles.Play();
            particles.Emit(16);
            Destroy(effect, 1f);
        }

        private void OnDestroy()
        {
            if (ownedMaterial != null) Destroy(ownedMaterial);
        }
    }
}
