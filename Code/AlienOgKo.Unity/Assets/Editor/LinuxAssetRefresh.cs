using System;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace AlienOgKo.Editor
{
    // Unity's native C++ directory watcher stops at filesystem mount boundaries.
    // On Linux with the project on a btrfs loop mount (different device from /home),
    // it sets up zero inotify watches on the project path, so auto-refresh never fires.
    // This script uses .NET's FileSystemWatcher, which calls inotify_add_watch directly
    // without the cross-device restriction, and forwards detected changes to AssetDatabase.
    [InitializeOnLoad]
    static class LinuxAssetRefresh
    {
        static FileSystemWatcher watcher;
        static System.Threading.Timer debounceTimer;
        static volatile bool pendingRefresh;

        static LinuxAssetRefresh()
        {
            if (Application.platform != RuntimePlatform.LinuxEditor)
                return;

            watcher = new FileSystemWatcher(Application.dataPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                EnableRaisingEvents = true,
            };

            watcher.Changed += ScheduleRefresh;
            watcher.Created += ScheduleRefresh;
            watcher.Deleted += ScheduleRefresh;
            watcher.Renamed += (s, e) => ScheduleRefresh(s, e);

            EditorApplication.update += FlushRefresh;
            AppDomain.CurrentDomain.DomainUnload += Cleanup;
        }

        static void ScheduleRefresh(object sender, FileSystemEventArgs e)
        {
            debounceTimer?.Dispose();
            debounceTimer = new System.Threading.Timer(_ => pendingRefresh = true, null, 400, Timeout.Infinite);
        }

        static void FlushRefresh()
        {
            if (!pendingRefresh) return;
            pendingRefresh = false;
            AssetDatabase.Refresh();
        }

        static void Cleanup(object sender, EventArgs e)
        {
            EditorApplication.update -= FlushRefresh;
            watcher?.Dispose();
            debounceTimer?.Dispose();
        }
    }
}
