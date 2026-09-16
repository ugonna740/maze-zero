using UnityEngine;
using UnityEngine.SceneManagement;

namespace MazeZero
{
    public static class MazeZeroBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (SceneManager.GetActiveScene().name == "Lobby") return;
            if (Object.FindFirstObjectByType<PracticeRunManager>() != null) return;
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) Object.Destroy(camera.gameObject);
            var sunObject = new GameObject("Directional Light", typeof(Light));
            var sun = sunObject.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.color = new Color(1f, .96f, .86f);
            sunObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = new Color(.45f, .5f, .58f);
            new GameObject("MAZE ZERO", typeof(PracticeRunManager));
        }
    }
}
