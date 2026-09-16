using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MazeZero
{
    public sealed class MiniMapTapToggle : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private RectTransform frame;
        [SerializeField] private Camera mapCamera;
        [SerializeField] private Vector2 collapsedSize = new Vector2(230, 230);
        [SerializeField] private Vector2 expandedSize = new Vector2(720, 720);
        [SerializeField] private Vector2 collapsedPosition = new Vector2(-28, -92);
        [SerializeField] private Vector2 expandedPosition = new Vector2(-90, -150);
        private float collapsedZoom;
        private bool expanded;
        private Vector2 targetSize;
        private Vector2 targetPosition;
        private float targetZoom;
        private int lastToggleFrame = -1;

        public void Configure(RectTransform targetFrame, Camera targetCamera)
        {
            frame = targetFrame; mapCamera = targetCamera; collapsedSize = frame.sizeDelta; collapsedPosition = frame.anchoredPosition;
            collapsedZoom = mapCamera != null ? mapCamera.orthographicSize : 10f;
            targetSize = collapsedSize; targetPosition = collapsedPosition; targetZoom = collapsedZoom;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Toggle();
        }

        private void Toggle()
        {
            if (lastToggleFrame == Time.frameCount) return;
            lastToggleFrame = Time.frameCount;
            expanded = !expanded;
            targetSize = expanded ? expandedSize : collapsedSize;
            targetPosition = expanded ? expandedPosition : collapsedPosition;
            targetZoom = collapsedZoom * (expanded ? 1.25f : 1f);
            if (expanded) frame.SetAsLastSibling();
        }

        private void Update()
        {
            if (frame == null) return;
            Vector2 pointerPosition = default;
            var pressed = false;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                pressed = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pointerPosition = Mouse.current.position.ReadValue();
                pressed = true;
            }
            if (pressed && RectTransformUtility.RectangleContainsScreenPoint(frame, pointerPosition)) Toggle();
            var speed = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            frame.sizeDelta = Vector2.Lerp(frame.sizeDelta, targetSize, speed);
            frame.anchoredPosition = Vector2.Lerp(frame.anchoredPosition, targetPosition, speed);
            if (mapCamera != null) mapCamera.orthographicSize = Mathf.Lerp(mapCamera.orthographicSize, targetZoom, speed);
        }
    }
}
