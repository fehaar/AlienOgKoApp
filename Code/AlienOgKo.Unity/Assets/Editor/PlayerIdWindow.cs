using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace AlienOgKo.Editor
{
    public class PlayerIdWindow : EditorWindow
    {
        private const string EditorIdFileName = "id-editor";
        private const string PlayerIdFileName = "id";
        private const string SecretKeyPref = "AlienOgKo.PlayFab.DevSecretKey";
        private const string PlayerDataKey = "AlienOgKoPlayer";

        [MenuItem("Window/AlienOgKo/Player details")]
        public static void ShowWindow()
        {
            var window = GetWindow<PlayerIdWindow>("Player details");
            window.minSize = new Vector2(420, 360);
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

        private string secretKey = string.Empty;
        private bool showSecretKey;
        private string playFabId = string.Empty;
        private string playFabJson = string.Empty;
        private string status = string.Empty;
        private MessageType statusType = MessageType.None;
        private double statusClearTime = -1;
        private bool busy;
        private Vector2 jsonScroll;
        private string serverUrl = string.Empty;

        private void OnEnable()
        {
            LoadFromFile();
            secretKey = EditorPrefs.GetString(SecretKeyPref, string.Empty);
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
            if (statusClearTime > 0 && EditorApplication.timeSinceStartup >= statusClearTime)
            {
                status = string.Empty;
                statusType = MessageType.None;
                statusClearTime = -1;
                Repaint();
            }

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

        private string GetTitleId()
        {
            var settings = Resources.Load<PlayFabSharedSettings>("PlayFabSharedSettings");
            return settings != null ? settings.TitleId : null;
        }

        private void OnGUI()
        {
            if (!loaded)
            {
                LoadFromFile();
            }

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

            GUILayout.Space(12);
            EditorGUILayout.LabelField("PlayFab", EditorStyles.boldLabel);

            var hasSecret = !string.IsNullOrEmpty(secretKey);
            var canCallPlayFab = isUlid && hasSecret && !busy;

            var foldoutStyle = new GUIStyle(EditorStyles.foldout);
            var secretColor = hasSecret ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
            foldoutStyle.normal.textColor = secretColor;
            foldoutStyle.onNormal.textColor = secretColor;
            foldoutStyle.focused.textColor = secretColor;
            foldoutStyle.onFocused.textColor = secretColor;
            foldoutStyle.active.textColor = secretColor;
            foldoutStyle.onActive.textColor = secretColor;
            showSecretKey = EditorGUILayout.Foldout(showSecretKey, "Developer secret", true, foldoutStyle);
            if (showSecretKey)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.BeginChangeCheck();
                    var newSecret = EditorGUILayout.PasswordField("Secret key", secretKey);
                    if (EditorGUI.EndChangeCheck())
                    {
                        secretKey = newSecret;
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(secretKey)))
                        {
                            if (GUILayout.Button("Save to EditorPrefs"))
                            {
                                EditorPrefs.SetString(SecretKeyPref, secretKey);
                                SetStatus($"Secret key saved ({secretKey.Length} chars).", MessageType.Info);
                            }
                        }
                        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(secretKey) && !EditorPrefs.HasKey(SecretKeyPref)))
                        {
                            if (GUILayout.Button("Forget"))
                            {
                                secretKey = string.Empty;
                                EditorPrefs.DeleteKey(SecretKeyPref);
                                SetStatus("Secret key cleared.", MessageType.Info);
                            }
                        }
                    }
                    EditorGUILayout.HelpBox("Stored locally in EditorPrefs on this machine. Do not commit.", MessageType.None);
                }
            }

            GUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!canCallPlayFab))
                {
                    if (GUILayout.Button("Fetch from PlayFab"))
                    {
                        FetchPlayerData();
                    }
                }
                using (new EditorGUI.DisabledScope(!canCallPlayFab || string.IsNullOrEmpty(playFabId)))
                {
                    if (GUILayout.Button("Reset PlayFab data"))
                    {
                        ResetPlayerData();
                    }
                }
            }

            if (!isUlid)
            {
                EditorGUILayout.HelpBox("Player ID above must be a valid ULID to call PlayFab.", MessageType.Info);
            }

            if (!string.IsNullOrEmpty(playFabId))
            {
                EditorGUILayout.LabelField("PlayFab ID", playFabId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                EditorGUILayout.HelpBox(status, statusType);
            }

            using (var scope = new EditorGUILayout.ScrollViewScope(jsonScroll, GUILayout.ExpandHeight(true)))
            {
                jsonScroll = scope.scrollPosition;
                var style = new GUIStyle(EditorStyles.textArea) { wordWrap = false };
                var content = new GUIContent(playFabJson ?? string.Empty);
                var height = style.CalcHeight(content, EditorGUIUtility.currentViewWidth);
                EditorGUILayout.SelectableLabel(playFabJson ?? string.Empty, style, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            }
        }

        private async void FetchPlayerData()
        {
            var titleId = GetTitleId();
            if (string.IsNullOrEmpty(titleId))
            {
                SetStatus("PlayFabSharedSettings is missing or has no TitleId.", MessageType.Error);
                return;
            }

            busy = true;
            SetStatus("Looking up PlayFab account...", MessageType.Info);
            try
            {
                var pfId = await PlayFabAdminClient.LoginAsync(titleId, secretKey, id);
                if (pfId == null)
                {
                    playFabId = string.Empty;
                    playFabJson = string.Empty;
                    SetStatus("No PlayFab account exists for this ULID yet.", MessageType.Warning);
                    return;
                }
                playFabId = pfId;
                var json = await PlayFabAdminClient.GetReadOnlyDataAsync(titleId, secretKey, pfId, PlayerDataKey);
                if (string.IsNullOrEmpty(json))
                {
                    playFabJson = string.Empty;
                    SetStatus($"PlayFab account found, but no '{PlayerDataKey}' record.", MessageType.Warning);
                }
                else
                {
                    playFabJson = PrettyPrint(json);
                    SetStatus("Fetched.", MessageType.Info);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Fetch failed: {ex.Message}", MessageType.Error);
            }
            finally
            {
                busy = false;
                Repaint();
            }
        }

        private async void ResetPlayerData()
        {
            if (!EditorUtility.DisplayDialog(
                "Reset PlayFab data",
                $"Remove the '{PlayerDataKey}' record from PlayFab account {playFabId}?\n\nA fresh record will be created the next time this player signs in.",
                "Delete",
                "Cancel"))
            {
                return;
            }

            var titleId = GetTitleId();
            busy = true;
            SetStatus("Resetting...", MessageType.Info);
            try
            {
                await PlayFabAdminClient.RemoveReadOnlyKeyAsync(titleId, secretKey, playFabId, PlayerDataKey);
                playFabJson = string.Empty;

                if (!string.IsNullOrEmpty(serverUrl))
                {
                    try
                    {
                        await PlayFabAdminClient.EvictPlayerCacheAsync(serverUrl, secretKey, id);
                        SetStatus($"Removed '{PlayerDataKey}' from PlayFab and evicted server cache.", MessageType.Info);
                    }
                    catch (Exception serverEx)
                    {
                        SetStatus($"Removed '{PlayerDataKey}' from PlayFab. Server cache eviction failed (server may not be running): {serverEx.Message}", MessageType.Warning);
                    }
                }
                else
                {
                    SetStatus($"Removed '{PlayerDataKey}' from PlayFab.", MessageType.Info);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Reset failed: {ex.Message}", MessageType.Error);
            }
            finally
            {
                busy = false;
                Repaint();
            }
        }

        private static string PrettyPrint(string json)
        {
            try
            {
                return JToken.Parse(json).ToString(Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }

        private void SetStatus(string message, MessageType type)
        {
            status = message;
            statusType = type;
            statusClearTime = type == MessageType.Info
                ? EditorApplication.timeSinceStartup + 3
                : -1;
            Repaint();
        }
    }
}
