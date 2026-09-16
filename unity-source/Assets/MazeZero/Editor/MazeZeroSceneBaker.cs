#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeZero.Editor
{
    [InitializeOnLoad]
    public static class MazeZeroSceneBaker
    {
        static MazeZeroSceneBaker()
        {
            EditorApplication.delayCall += BakeWhenMissing;
        }

        [MenuItem("Maze Zero/Bake Editable Scene Preview")]
        public static void BakeEditableScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var trapManager = Object.FindFirstObjectByType<TrapManager>();
            if (trapManager == null)
                trapManager = new GameObject("Trap Manager - ADD YOUR PREFABS").AddComponent<TrapManager>();
            var existing = Object.FindFirstObjectByType<PracticeRunManager>();
            var manager = existing != null ? existing : new GameObject("MAZE ZERO - EDITABLE").AddComponent<PracticeRunManager>();
            manager.BuildPreviewInEditor();
            DisableTemplateCameras(manager.transform);
            Selection.activeGameObject = manager.gameObject;
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            EditorSceneManager.SaveScene(manager.gameObject.scene);
            Debug.Log("MAZE//ZERO editable scene preview baked. All gameplay objects are visible in the Hierarchy.");
        }

        private static void BakeWhenMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path)) return;
            if (Object.FindFirstObjectByType<PracticeRunManager>() != null && Object.FindFirstObjectByType<TrapManager>() != null) return;
            BakeEditableScene();
        }

        private static void DisableTemplateCameras(Transform manager)
        {
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (camera.transform.IsChildOf(manager)) continue;
                camera.gameObject.SetActive(false);
            }
        }
    }
}
#endif
