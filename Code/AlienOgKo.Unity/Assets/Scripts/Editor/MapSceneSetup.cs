using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MapSceneSetup
{
    [MenuItem("AlienOgKo/Setup Map Scene")]
    static void Setup()
    {
        var existing = GameObject.Find("MapCanvas");
        if (existing != null)
            Object.DestroyImmediate(existing);

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

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
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("AlienOgKo: Map scene setup complete.");
    }
}
