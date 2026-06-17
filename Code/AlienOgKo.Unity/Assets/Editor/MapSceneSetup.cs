using AlienOgKo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class MapSceneSetup
{
    [MenuItem("AlienOgKo/Setup Map Scene")]
    static void Setup()
    {
        var existing = GameObject.Find("MapCanvas");
        if (existing != null)
            Object.DestroyImmediate(existing);

        var existingEs = Object.FindAnyObjectByType<EventSystem>();
        if (existingEs != null)
            Object.DestroyImmediate(existingEs.gameObject);

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();

        var canvasGO = new GameObject("MapCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Map/map.png");
        if (tex == null)
        {
            Debug.LogError("Assets/Map/map.png not found — let Unity finish importing then try again.");
            Object.DestroyImmediate(canvasGO);
            return;
        }

        var imageGO = new GameObject("MapImage");
        imageGO.transform.SetParent(canvasGO.transform, false);
        var rawImage = imageGO.AddComponent<RawImage>();
        rawImage.texture = tex;

        var rt = imageGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;

        var fitter = imageGO.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 1.0f;

        imageGO.transform.localScale = new Vector3(2f, 2f, 1f);

        imageGO.AddComponent<MapView>();

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("AlienOgKo: Map scene setup complete.");
    }
}
