using UnityEditor;
using UnityEngine;

namespace AlienOgKo.EditorTools
{
    public static class MapSettingsAssetCreator
    {
        private const string AssetPath = "Assets/Resources/MapSettings.asset";

        [MenuItem("Gosuman/Create Map Settings Asset")]
        public static void CreateAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<MapSettings>(AssetPath) != null)
            {
                Debug.Log($"{AssetPath} already exists, skipping.");
                return;
            }

            var settings = ScriptableObject.CreateInstance<MapSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {AssetPath}");
        }
    }
}
