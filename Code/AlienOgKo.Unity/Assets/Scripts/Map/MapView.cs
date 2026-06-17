using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace AlienOgKo
{
    [RequireComponent(typeof(RectTransform))]
    public class MapView : MonoBehaviour
    {
        [SerializeField] float minZoom = 1f;
        [SerializeField] float maxZoom = 8f;
        [SerializeField] float scrollZoomSpeed = 0.1f;

        RectTransform rt;
        RectTransform parentRt;

        void Awake()
        {
            rt = GetComponent<RectTransform>();
            parentRt = (RectTransform)rt.parent;
        }

        void OnEnable()  => EnhancedTouchSupport.Enable();
        void OnDisable() => EnhancedTouchSupport.Disable();

        void Update()
        {
            var touches = Touch.activeTouches;

            if (touches.Count == 1)
                HandlePan(touches[0]);
            else if (touches.Count == 2)
                HandlePinch(touches[0], touches[1]);
            else
                HandleMouse();

            Clamp();
        }

        void HandlePan(Touch t)
        {
            if (t.delta == default) return;
            rt.anchoredPosition += t.delta / CanvasScale();
        }

        void HandlePinch(Touch t0, Touch t1)
        {
            Vector2 prev0 = t0.screenPosition - t0.delta;
            Vector2 prev1 = t1.screenPosition - t1.delta;

            float prevDist = Vector2.Distance(prev0, prev1);
            float currDist = Vector2.Distance(t0.screenPosition, t1.screenPosition);
            if (prevDist == 0f) return;

            float factor = currDist / prevDist;
            ZoomTowards((t0.screenPosition + t1.screenPosition) * 0.5f, factor);
        }

        void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            bool shift = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;

            if (mouse.leftButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                if (shift)
                {
                    float factor = 1f + delta.y * scrollZoomSpeed;
                    ZoomTowards(mouse.position.ReadValue(), factor);
                }
                else
                {
                    rt.anchoredPosition += delta / CanvasScale();
                }
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                float factor = 1f + scroll * scrollZoomSpeed;
                ZoomTowards(mouse.position.ReadValue(), factor);
            }
        }

        void ZoomTowards(Vector2 screenPivot, float factor)
        {
            float newScale = Mathf.Clamp(rt.localScale.x * factor, minZoom, maxZoom);
            float actualFactor = newScale / rt.localScale.x;

            Vector2 pivot = ScreenToCanvas(screenPivot);
            rt.anchoredPosition = pivot + (rt.anchoredPosition - pivot) * actualFactor;
            rt.localScale = new Vector3(newScale, newScale, 1f);
        }

        void Clamp()
        {
            Vector2 parentSize = parentRt.rect.size;
            Vector2 mapSize = rt.rect.size * rt.localScale.x;

            Vector2 limit = Vector2.Max((mapSize - parentSize) * 0.5f, Vector2.zero);

            Vector2 pos = rt.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, -limit.x, limit.x);
            pos.y = Mathf.Clamp(pos.y, -limit.y, limit.y);
            rt.anchoredPosition = pos;
        }

        float CanvasScale()
        {
            var canvas = GetComponentInParent<Canvas>();
            return canvas != null ? canvas.scaleFactor : 1f;
        }

        Vector2 ScreenToCanvas(Vector2 screenPos)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return screenPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRt, screenPos, canvas.worldCamera, out Vector2 local);
            return local;
        }
    }
}
