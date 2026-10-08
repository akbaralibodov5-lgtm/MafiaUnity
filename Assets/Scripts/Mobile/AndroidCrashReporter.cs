using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace MafiaUnity
{
    /// <summary>
    /// Keeps a compact persistent Unity log on Android for diagnosing device
    /// crashes without requiring logcat on the user's phone.
    /// </summary>
    public sealed class AndroidCrashReporter : MonoBehaviour
    {
        private static AndroidCrashReporter instance;
        private StringBuilder buffer = new StringBuilder(32 * 1024);
        private string logPath;
        private int logCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (instance != null) return;
            var go = new GameObject("AndroidCrashReporter");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AndroidCrashReporter>();
#endif
        }

        private void Awake()
        {
            logPath = Path.Combine(Application.persistentDataPath, "MafiaUnity-Android.log");
            Application.logMessageReceived += OnLog;
            Application.lowMemory += OnLowMemory;
            Write("=== MafiaUnity Android session " + DateTime.UtcNow.ToString("O") + " ===");
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            Application.lowMemory -= OnLowMemory;
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception &&
                type != LogType.Assert && type != LogType.Log)
                return;

            Write(type + ": " + condition + (string.IsNullOrEmpty(stackTrace) ? "" : "\n" + stackTrace));
        }

        private void OnLowMemory()
        {
            Write("LOW MEMORY: " + SystemInfo.systemMemorySize + " MB RAM reported");
            Flush();
        }

        private void Write(string line)
        {
            buffer.AppendLine(line);
            logCount++;
            if (logCount >= 16)
                Flush();
        }

        private void Flush()
        {
            try
            {
                File.AppendAllText(logPath, buffer.ToString());
                buffer.Length = 0;
                logCount = 0;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Could not write Android log: " + ex.Message);
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) Flush();
        }

        private void OnApplicationQuit()
        {
            Flush();
        }
    }
}
