using UnityEngine;
using UnityEngine.UI;

namespace MazeZero
{
    public sealed class MiniMap : MonoBehaviour
    {
        private Transform player;
        private RectTransform marker;
        private float worldWidth;
        private float worldHeight;
        [SerializeField] private RectTransform frame;

        private void Awake()
        {
            EnsureTapToggle();
        }

        public bool EnsureTapToggle()
        {
            var repairedReference = false;
            if (frame == null)
            {
                var hud = FindFirstObjectByType<PracticeHud>(FindObjectsInactive.Include);
                var existingFrame = hud != null ? hud.transform.Find("MiniMap") : null;
                frame = existingFrame as RectTransform;
                repairedReference = frame != null;
            }
            if (frame == null) return false;
            var rawImage = frame.GetComponent<RawImage>();
            if (rawImage != null) rawImage.raycastTarget = true;
            var toggle = frame.GetComponent<MiniMapTapToggle>();
            var added = toggle == null;
            if (added) toggle = frame.gameObject.AddComponent<MiniMapTapToggle>();
            toggle.Configure(frame, GetComponent<Camera>());
            return added || repairedReference;
        }

        public static void Create(Transform owner, Transform hudCanvas, Transform player, int width, int height, float cellSize)
        {
            var mapObject = new GameObject("MiniMap Camera", typeof(Camera), typeof(MiniMap));
            mapObject.transform.SetParent(owner);
            var map = mapObject.GetComponent<MiniMap>();
            map.player = player;
            map.worldWidth = width * cellSize;
            map.worldHeight = height * cellSize;
            mapObject.transform.position = new Vector3((width - 1) * cellSize * .5f, 65f, (height - 1) * cellSize * .5f);
            mapObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var camera = mapObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(width, height) * cellSize * .55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.015f, .02f, .025f);
            camera.depth = -5;
            camera.enabled = true;
            camera.targetTexture = new RenderTexture(320, 320, 16) { name = "Maze MiniMap" };

            var frameObject = new GameObject("MiniMap", typeof(RectTransform), typeof(RawImage), typeof(Outline), typeof(MiniMapTapToggle));
            frameObject.transform.SetParent(hudCanvas, false);
            map.frame = frameObject.GetComponent<RectTransform>();
            var rect = map.frame;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-28f, -92f);
            rect.sizeDelta = new Vector2(230f, 230f);
            frameObject.GetComponent<RawImage>().texture = camera.targetTexture;
            frameObject.GetComponent<RawImage>().color = new Color(1, 1, 1, .9f);
            frameObject.GetComponent<RawImage>().raycastTarget = true;
            frameObject.GetComponent<Outline>().effectColor = new Color(.15f, .85f, 1f, .8f);
            frameObject.GetComponent<MiniMapTapToggle>().Configure(rect, camera);
            var markerObject = new GameObject("Player Marker", typeof(RectTransform), typeof(Image));
            markerObject.transform.SetParent(frameObject.transform, false);
            map.marker = markerObject.GetComponent<RectTransform>();
            map.marker.sizeDelta = new Vector2(14f, 14f);
            markerObject.GetComponent<Image>().color = new Color(1f, .25f, .12f);
        }

        private void LateUpdate()
        {
            if (player == null || marker == null) return;
            var x = Mathf.Clamp01(player.position.x / worldWidth);
            var y = Mathf.Clamp01(player.position.z / worldHeight);
            marker.anchorMin = marker.anchorMax = new Vector2(x, y);
            marker.anchoredPosition = Vector2.zero;
            marker.localRotation = Quaternion.Euler(0, 0, -player.eulerAngles.y);
        }

        private void OnDestroy()
        {
            var camera = GetComponent<Camera>();
            if (camera != null && camera.targetTexture != null) camera.targetTexture.Release();
            if (frame != null)
            {
                if (Application.isPlaying) Destroy(frame.gameObject);
                else DestroyImmediate(frame.gameObject);
            }
        }
    }
}
