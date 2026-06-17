using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gosuman.TBF.Editor
{
    public class PlayerIdWindow : EditorWindow
    {
        private const string EditorIdFileName = "id-editor";
        private const string PlayerIdFileName = "id";

        [MenuItem("Window/Gosuman/Player details")]
        public static void ShowWindow()
        {
            var window = GetWindow<PlayerIdWindow>("Player details");
            window.minSize = new Vector2(360, 140);
        }

        private static string EditorIdPath => Path.Combine(Application.persistentDataPath, EditorIdFileName);
        private static string PlayerIdPath => Path.Combine(Application.persistentDataPath, PlayerIdFileName);

        private static string ActiveIdPath => Application.isPlaying && !Application.isEditor
            ? PlayerIdPath
            : EditorIdPath;

        private const double PollIntervalSeconds = 0.25;

        private string id;
        private bool loaded;
        private bool polling;
        private double nextPollTime;

        private void OnEnable()
        {
            LoadFromFile();
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnFocus()
        {
            LoadFromFile();
            Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && string.IsNullOrEmpty(id))
            {
                polling = true;
                nextPollTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                polling = false;
            }
        }

        private void OnEditorUpdate()
        {
            if (!polling) return;

            // User typed into the field — preserve their input, stop polling.
            if (!string.IsNullOrEmpty(id))
            {
                polling = false;
                return;
            }

            if (EditorApplication.timeSinceStartup < nextPollTime) return;
            nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;

            LoadFromFile();
            if (!string.IsNullOrEmpty(id))
            {
                polling = false;
                Repaint();
            }
        }

        private void LoadFromFile()
        {
            var path = ActiveIdPath;
            id = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
            loaded = true;
        }

        private const float Margin = 4f;

        private void OnGUI()
        {
            if (!loaded)
            {
                LoadFromFile();
            }

            GUILayout.BeginArea(new Rect(Margin, Margin, position.width - Margin * 2, position.height - Margin * 2));

            EditorGUILayout.LabelField("Player ID", EditorStyles.boldLabel);

            id = EditorGUILayout.TextField(id ?? string.Empty);

            if (EditorGUILayout.LinkButton(ActiveIdPath))
            {
                EditorUtility.RevealInFinder(ActiveIdPath);
            }

            var isUlid = !string.IsNullOrWhiteSpace(id) && Ulid.TryParse(id, out _);
            if (polling)
            {
                EditorGUILayout.HelpBox("Waiting for the ID to be created", MessageType.Info);
            }
            else if (!isUlid)
            {
                EditorGUILayout.HelpBox("Run the game to generate a new ID or paste an existing ID", MessageType.Warning);
            }

            GUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!isUlid))
                {
                    if (GUILayout.Button("Save"))
                    {
                        var path = ActiveIdPath;
                        File.WriteAllText(path, id);
                        Debug.Log($"Player ID '{id}' saved to {path}");
                    }
                    if (GUILayout.Button("Copy to clipboard"))
                    {
                        EditorGUIUtility.systemCopyBuffer = id;
                    }
                }

                using (new EditorGUI.DisabledScope(!File.Exists(ActiveIdPath)))
                {
                    if (GUILayout.Button("Reset"))
                    {
                        if (EditorUtility.DisplayDialog(
                            "Reset Player ID",
                            $"Delete {ActiveIdPath}?\n\nA new ID will be generated the next time you enter Play mode.",
                            "Delete",
                            "Cancel"))
                        {
                            File.Delete(ActiveIdPath);
                            LoadFromFile();
                        }
                    }
                }
            }

            GUILayout.EndArea();
        }
    }
}
