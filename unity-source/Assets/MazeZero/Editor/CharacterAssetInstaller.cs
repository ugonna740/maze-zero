#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Linq;

namespace MazeZero.Editor
{
    [InitializeOnLoad]
    public static class CharacterAssetInstaller
    {
        private const string SourcePath = "Assets/MazeZero/3d/rig+fbx.fbx";
        private const string OutputFolder = "Assets/MazeZero/Resources";
        private const string OutputPath = OutputFolder + "/MazeZeroCharacter.prefab";
        private const string ControllerPath = OutputFolder + "/MazeZeroCharacter.controller";
        private const string IdlePath = "Assets/MazeZero/3d/X Bot@Idle.fbx";
        private const string RunPath = "Assets/MazeZero/3d/X Bot@Fast Run.fbx";

        static CharacterAssetInstaller()
        {
            EditorApplication.delayCall += EnsureCharacterPrefab;
        }

        [MenuItem("Maze Zero/Rebuild Character Prefab")]
        private static void EnsureCharacterPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            if (source == null) return;
            if (!Directory.Exists(OutputFolder)) Directory.CreateDirectory(OutputFolder);
            var controller = EnsureAnimatorController();
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = "MazeZeroCharacter";
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            PrefabUtility.SaveAsPrefabAsset(instance, OutputPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            Debug.Log("MAZE//ZERO character prefab is ready at " + OutputPath);
        }

        private static AnimatorController EnsureAnimatorController()
        {
            EnsureLooping(IdlePath);
            EnsureLooping(RunPath);
            var idle = FirstClip(IdlePath);
            var run = FirstClip(RunPath);
            if (idle == null || run == null)
            {
                Debug.LogWarning("MAZE//ZERO is waiting for the Idle and Fast Run clips to finish importing.");
                return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
                var stateMachine = controller.layers[0].stateMachine;
                var state = stateMachine.AddState("Locomotion");
                var tree = new BlendTree { name = "Idle Run", blendParameter = "Speed", blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false };
                AssetDatabase.AddObjectToAsset(tree, controller);
                tree.AddChild(idle, 0f);
                tree.AddChild(run, 1f);
                state.motion = tree;
                stateMachine.defaultState = state;
            }
            else
            {
                var tree = controller.layers[0].stateMachine.states.Select(s => s.state.motion).OfType<BlendTree>().FirstOrDefault();
                if (tree != null)
                {
                    var children = tree.children;
                    if (children.Length >= 2)
                    {
                        children[0].motion = idle;
                        children[1].motion = run;
                        tree.children = children;
                    }
                }
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static AnimationClip FirstClip(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));

        private static void EnsureLooping(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer) return;
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;
            var changed = false;
            for (var i = 0; i < clips.Length; i++)
            {
                if (clips[i].loopTime) continue;
                clips[i].loopTime = true;
                clips[i].loopPose = true;
                changed = true;
            }
            if (!changed) return;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }
}
#endif
