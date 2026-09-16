using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MazeZero
{
    public sealed class OnScreenControls : MonoBehaviour
    {
        public static Vector2 Move { get; private set; }
        private static Vector2 cameraDelta;

        public static Vector2 ConsumeCameraDelta()
        {
            var value = cameraDelta;
            cameraDelta = Vector2.zero;
            return value;
        }

        public static void SetMove(Vector2 value) => Move = Vector2.ClampMagnitude(value, 1f);
        public static void AddCameraDelta(Vector2 value) => cameraDelta += value;

        public static void Create(Transform canvas)
        {
            EnsureEventSystem();
            var root = new GameObject("On-screen Controls", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            var controls = root.AddComponent<OnScreenControls>();
            if (controls.GetComponentInChildren<FloatingJoystick>(true) == null) controls.RebuildControls();
        }

        private void Awake()
        {
            EnsureEventSystem();
            // Repairs older baked scenes whose nested input handlers could not serialize.
            if (GetComponentInChildren<FloatingJoystick>(true) == null) RebuildControls();
        }

        private void RebuildControls()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
                else DestroyImmediate(transform.GetChild(i).gameObject);
            }

            var cameraZone = Panel(transform, "Camera Pan Zone", new Color(0, 0, 0, 0), new Vector2(.46f, 0), Vector2.one);
            cameraZone.AddComponent<CameraDragZone>();
            var joystickArea = Panel(transform, "Movement Zone", new Color(0, 0, 0, 0), Vector2.zero, new Vector2(.46f, .62f));
            var joystick = joystickArea.AddComponent<FloatingJoystick>();
            joystick.Build();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        private static GameObject Panel(Transform parent, string name, Color color, Vector2 min, Vector2 max)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            item.GetComponent<Image>().color = color;
            return item;
        }
    }
}
