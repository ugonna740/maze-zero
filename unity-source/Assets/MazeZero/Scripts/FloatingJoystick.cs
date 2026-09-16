using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MazeZero
{
    public sealed class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform baseRect;
        [SerializeField] private RectTransform knob;
        private const float Radius = 78f;

        private void Awake()
        {
            RepairReferences();
            HideJoystick();
        }

        public void Build()
        {
            var background = Resources.Load<Sprite>("UI/Move Joystick Background");
            var handle = Resources.Load<Sprite>("UI/Move Joystick Handle");
            baseRect = CreateImage(transform, "Move", 170f, background, new Color(1f, 1f, 1f, .9f));
            knob = CreateImage(baseRect, "Knob", 76f, handle, Color.white);
            HideJoystick();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            RepairReferences();
            if (baseRect == null || knob == null) return;
            baseRect.gameObject.SetActive(true);
            baseRect.position = eventData.position;
            knob.anchoredPosition = Vector2.zero;
            UpdateValue(eventData);
        }

        public void OnDrag(PointerEventData eventData) => UpdateValue(eventData);

        public void OnPointerUp(PointerEventData eventData)
        {
            OnScreenControls.SetMove(Vector2.zero);
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            HideJoystick();
        }

        private void UpdateValue(PointerEventData eventData)
        {
            if (baseRect == null || knob == null) return;
            var delta = Vector2.ClampMagnitude(eventData.position - (Vector2)baseRect.position, Radius);
            knob.anchoredPosition = delta;
            OnScreenControls.SetMove(delta / Radius);
        }

        private void RepairReferences()
        {
            if (baseRect == null) baseRect = transform.Find("Move") as RectTransform;
            if (baseRect != null && knob == null) knob = baseRect.Find("Knob") as RectTransform;
        }

        private void HideJoystick()
        {
            if (baseRect == null) return;
            baseRect.gameObject.SetActive(false);
        }

        private static RectTransform CreateImage(Transform parent, string name, float size, Sprite sprite, Color color)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.sizeDelta = Vector2.one * size;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            var image = item.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite != null ? sprite : Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return rect;
        }
    }
}
