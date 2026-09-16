using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MazeZero
{
    public sealed class HorizontalCompass : MonoBehaviour
    {
        [SerializeField] private RectTransform[] ticks;
        [SerializeField] private float[] headings;
        [SerializeField] private TMP_Text degrees;
        [SerializeField] private float visibleHalfAngle = 100f;
        [SerializeField] private float tapeHalfWidth = 320f;

        public static HorizontalCompass Create(Transform parent, TMP_FontAsset font)
        {
            var root = new GameObject("Horizontal Compass", typeof(RectTransform), typeof(Image), typeof(Shadow), typeof(HorizontalCompass));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 24);
            rect.sizeDelta = new Vector2(720, 88);
            var background = root.GetComponent<Image>();
            background.sprite = Resources.Load<Sprite>("CartoonUI/ChequeredInk/Blue/ButtonIdle");
            background.type = background.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.color = new Color(1f, 1f, 1f, .92f);
            background.raycastTarget = false;
            var shadow = root.GetComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .35f); shadow.effectDistance = new Vector2(0, -5);

            var viewport = new GameObject("Compass Tape Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero; viewportRect.anchorMax = Vector2.one; viewportRect.offsetMin = new Vector2(28, 20); viewportRect.offsetMax = new Vector2(-28, -5);

            const int count = 16;
            var compass = root.GetComponent<HorizontalCompass>();
            compass.ticks = new RectTransform[count];
            compass.headings = new float[count];
            for (var i = 0; i < count; i++)
            {
                var angle = i * 22.5f;
                var item = new GameObject("Heading " + angle.ToString("0"), typeof(RectTransform), typeof(TextMeshProUGUI));
                item.transform.SetParent(viewport.transform, false);
                var itemRect = item.GetComponent<RectTransform>(); itemRect.anchorMin = itemRect.anchorMax = new Vector2(.5f, .5f); itemRect.sizeDelta = new Vector2(82, 52);
                var text = item.GetComponent<TextMeshProUGUI>(); text.font = font; text.fontStyle = FontStyles.Bold; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
                var major = i % 2 == 0;
                text.text = major ? Cardinal(i / 2) : Mathf.RoundToInt(angle).ToString();
                text.fontSize = major ? 29 : 16; text.color = major ? new Color32(255, 245, 210, 255) : new Color32(205, 235, 255, 255);
                text.outlineColor = new Color32(20, 35, 65, 220); text.outlineWidth = .18f;
                compass.ticks[i] = itemRect; compass.headings[i] = angle;
            }

            var marker = new GameObject("Center Heading Marker", typeof(RectTransform), typeof(Image)); marker.transform.SetParent(root.transform, false);
            var markerRect = marker.GetComponent<RectTransform>(); markerRect.anchorMin = markerRect.anchorMax = new Vector2(.5f, .5f); markerRect.anchoredPosition = new Vector2(0, -23); markerRect.sizeDelta = new Vector2(78, 34);
            var markerImage = marker.GetComponent<Image>(); markerImage.sprite = Resources.Load<Sprite>("CartoonUI/ChequeredInk/Orange/ButtonIdle"); markerImage.type = markerImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple; markerImage.raycastTarget = false;
            compass.degrees = AddLabel(marker.transform, font, "000°", 21, Color.white);
            var pointer = new GameObject("Compass Pointer", typeof(RectTransform), typeof(Image)); pointer.transform.SetParent(root.transform, false);
            var pointerRect = pointer.GetComponent<RectTransform>(); pointerRect.anchorMin = pointerRect.anchorMax = new Vector2(.5f, 1f); pointerRect.pivot = new Vector2(.5f, 1f); pointerRect.anchoredPosition = new Vector2(0, -4); pointerRect.sizeDelta = new Vector2(5, 50);
            pointer.GetComponent<Image>().color = new Color32(255, 174, 50, 255); pointer.GetComponent<Image>().raycastTarget = false;
            compass.UpdateTape(0f);
            return compass;
        }

        private void LateUpdate()
        {
            if (Camera.main != null) UpdateTape(Camera.main.transform.eulerAngles.y);
        }

        private void UpdateTape(float yaw)
        {
            yaw = Mathf.Repeat(yaw, 360f);
            if (degrees != null) degrees.text = Mathf.RoundToInt(yaw).ToString("000") + "°";
            if (ticks == null || headings == null) return;
            for (var i = 0; i < ticks.Length && i < headings.Length; i++)
            {
                if (ticks[i] == null) continue;
                var delta = Mathf.DeltaAngle(yaw, headings[i]);
                ticks[i].anchoredPosition = new Vector2(delta / visibleHalfAngle * tapeHalfWidth, 7);
                ticks[i].gameObject.SetActive(Mathf.Abs(delta) <= visibleHalfAngle + 15f);
            }
        }

        private static TMP_Text AddLabel(Transform parent, TMP_FontAsset font, string value, float size, Color color)
        {
            var item = new GameObject("Heading Degrees", typeof(RectTransform), typeof(TextMeshProUGUI)); item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = item.GetComponent<TextMeshProUGUI>(); text.font = font; text.text = value; text.fontSize = size; text.fontStyle = FontStyles.Bold; text.alignment = TextAlignmentOptions.Center; text.color = color; text.raycastTarget = false; return text;
        }

        private static string Cardinal(int index)
        {
            string[] names = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            return names[index % names.Length];
        }
    }
}
