using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BounceSceneCreator
{
    const string SpritesheetPath = "Assets/Graphics/Alien_Ko_Spritesheet.png";
    const string ScenePath = "Assets/Scenes/bounce.unity";

    [MenuItem("AlienOgKo/Create Bounce Scene")]
    static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var cameraGo = new GameObject("Main Camera");
        var cam = cameraGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
        cameraGo.AddComponent<AudioListener>();
        cameraGo.tag = "MainCamera";

        // Canvas (Screen Space – Overlay)
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // Image GameObject
        var imageGo = new GameObject("AlienKo");
        imageGo.transform.SetParent(canvasGo.transform, false);
        var rt = imageGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(256, 256);
        rt.anchoredPosition = Vector2.zero;

        var image = imageGo.AddComponent<Image>();
        image.raycastTarget = false;

        var controller = imageGo.AddComponent<GifFrameController>();
        imageGo.AddComponent<GifAutoPlay>();

        // Load sprites from sprite sheet
        var sprites = AssetDatabase.LoadAllAssetsAtPath(SpritesheetPath)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogWarning($"[BounceSceneCreator] No sprites found at {SpritesheetPath}. " +
                             "Make sure the sprite sheet has been imported first, then re-run this menu item.");
        }
        else
        {
            var framesField = typeof(GifFrameController)
                .GetField("frames", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            framesField?.SetValue(controller, sprites);
            image.sprite = sprites[0];
            Debug.Log($"[BounceSceneCreator] Loaded {sprites.Length} sprites.");
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[BounceSceneCreator] Saved scene to {ScenePath}");
    }
}
