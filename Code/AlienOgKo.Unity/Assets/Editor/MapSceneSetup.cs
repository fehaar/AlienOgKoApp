using AlienOgKo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class MapSceneSetup
{
    private const float BarHeight = 150f;

    [MenuItem("AlienOgKo/Setup Map Scene")]
    static void Setup()
    {
        // EventSystem
        var existingEs = Object.FindAnyObjectByType<EventSystem>();
        if (existingEs != null) Object.DestroyImmediate(existingEs.gameObject);

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        // Root canvas
        var existing = GameObject.Find("MapCanvas");
        if (existing != null) Object.DestroyImmediate(existing);

        var canvasGO = new GameObject("MapCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Venue map texture
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Map/map-venue.png");
        if (tex == null)
        {
            Debug.LogError("Assets/Map/map-venue.png not found — let Unity finish importing then try again.");
            Object.DestroyImmediate(canvasGO);
            return;
        }

        // MapContainer — fills canvas minus the bar insets on top and bottom
        var containerGO = new GameObject("MapContainer");
        containerGO.transform.SetParent(canvasGO.transform, false);
        var containerRt = containerGO.AddComponent<RectTransform>();
        containerRt.anchorMin = Vector2.zero;
        containerRt.anchorMax = Vector2.one;
        containerRt.offsetMin = new Vector2(0, BarHeight);
        containerRt.offsetMax = new Vector2(0, -BarHeight);

        // Map image inside container
        var imageGO = new GameObject("MapImage");
        imageGO.transform.SetParent(containerGO.transform, false);
        var rawImage = imageGO.AddComponent<RawImage>();
        rawImage.texture = tex;

        var imageRt = imageGO.GetComponent<RectTransform>();
        imageRt.anchorMin = new Vector2(0.5f, 0.5f);
        imageRt.anchorMax = new Vector2(0.5f, 0.5f);
        imageRt.pivot = new Vector2(0.5f, 0.5f);
        imageRt.sizeDelta = Vector2.zero;

        var fitter = imageGO.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = (float)tex.width / tex.height;

        imageGO.AddComponent<MapView>();

        // TopBar — anchored to the top of the canvas, rendered above the map
        var topBarGO = new GameObject("TopBar");
        topBarGO.transform.SetParent(canvasGO.transform, false);
        var topBarRt = topBarGO.AddComponent<RectTransform>();
        topBarRt.anchorMin = new Vector2(0, 1);
        topBarRt.anchorMax = new Vector2(1, 1);
        topBarRt.pivot = new Vector2(0.5f, 1f);
        topBarRt.sizeDelta = new Vector2(0, BarHeight);
        topBarRt.anchoredPosition = Vector2.zero;
        var topBarBg = topBarGO.AddComponent<Image>();
        topBarBg.color = new Color(0, 0, 0, 0.5f);
        topBarGO.AddComponent<TopBarView>();

        // BottomBar — anchored to the bottom of the canvas, rendered above the map
        var bottomBarGO = new GameObject("BottomBar");
        bottomBarGO.transform.SetParent(canvasGO.transform, false);
        var bottomBarRt = bottomBarGO.AddComponent<RectTransform>();
        bottomBarRt.anchorMin = new Vector2(0, 0);
        bottomBarRt.anchorMax = new Vector2(1, 0);
        bottomBarRt.pivot = new Vector2(0.5f, 0f);
        bottomBarRt.sizeDelta = new Vector2(0, BarHeight);
        bottomBarRt.anchoredPosition = Vector2.zero;
        var bottomBarBg = bottomBarGO.AddComponent<Image>();
        bottomBarBg.color = new Color(0, 0, 0, 0.5f);
        bottomBarGO.AddComponent<BottomBarView>();

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("AlienOgKo: Map scene setup complete.");
    }
}
