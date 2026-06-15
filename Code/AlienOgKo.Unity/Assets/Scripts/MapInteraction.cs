using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[RequireComponent(typeof(RectTransform))]
public class MapInteraction : MonoBehaviour
{
    [SerializeField] float minZoom = 1f;
    [SerializeField] float maxZoom = 8f;

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

        Clamp();
    }

    void HandlePan(Touch t)
    {
        if (t.delta == default) return;
        // delta is in screen pixels; convert to canvas local units
        float scale = rt.localScale.x;
        Vector2 delta = t.delta / CanvasScale();
        rt.anchoredPosition += delta;
    }

    void HandlePinch(Touch t0, Touch t1)
    {
        Vector2 prev0 = t0.screenPosition - t0.delta;
        Vector2 prev1 = t1.screenPosition - t1.delta;

        float prevDist = Vector2.Distance(prev0, prev1);
        float currDist = Vector2.Distance(t0.screenPosition, t1.screenPosition);
        if (prevDist == 0f) return;

        float factor = currDist / prevDist;
        float newScale = Mathf.Clamp(rt.localScale.x * factor, minZoom, maxZoom);
        float actualFactor = newScale / rt.localScale.x;

        // Zoom towards the midpoint between fingers
        Vector2 midScreen = (t0.screenPosition + t1.screenPosition) * 0.5f;
        Vector2 midCanvas = ScreenToCanvas(midScreen);

        rt.anchoredPosition = midCanvas + (rt.anchoredPosition - midCanvas) * actualFactor;
        rt.localScale = new Vector3(newScale, newScale, 1f);
    }

    // Keep the map covering the full parent rect — no empty edges visible.
    void Clamp()
    {
        float scale = rt.localScale.x;
        Vector2 parentSize = parentRt.rect.size;
        // Size of the map in canvas space after scaling
        Vector2 mapSize = rt.rect.size * scale;

        // Half-extents of the visible region the map can travel
        Vector2 limit = (mapSize - parentSize) * 0.5f;
        limit = Vector2.Max(limit, Vector2.zero);

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
