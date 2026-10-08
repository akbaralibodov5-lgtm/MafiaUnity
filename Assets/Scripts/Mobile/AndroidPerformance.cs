using UnityEngine;

namespace MafiaUnity
{
    /// <summary>
    /// Conservative Android performance defaults. The original game renderer
    /// is kept intact; these settings mainly remove avoidable synchronization
    /// and sleep overhead on mobile GPUs.
    /// </summary>
    public sealed class AndroidPerformance : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var go = new GameObject("AndroidPerformance");
            DontDestroyOnLoad(go);
            go.AddComponent<AndroidPerformance>();
#endif
        }

        private void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Application.backgroundLoadingPriority = ThreadPriority.BelowNormal;

            // Avoid forcing expensive per-frame texture work on devices where
            // the driver already handles filtering efficiently.
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;

            Debug.Log(
                "Android performance: " +
                SystemInfo.graphicsDeviceName + ", " +
                SystemInfo.systemMemorySize + "MB RAM, " +
                SystemInfo.graphicsMemorySize + "MB VRAM");
#endif
        }

        private void OnApplicationPause(bool pause)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (pause)
                Resources.UnloadUnusedAssets();
#endif
        }
    }
}
