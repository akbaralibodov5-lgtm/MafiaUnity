using UnityEngine;

namespace MafiaUnity
{
    /// <summary>
    /// Small Android runtime bootstrap. Keeps the game in landscape and avoids
    /// Unity's default mobile sleep behaviour while the game is running.
    /// </summary>
    public sealed class AndroidRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var go = new GameObject("AndroidRuntimeBootstrap");
            DontDestroyOnLoad(go);
            go.AddComponent<AndroidRuntimeBootstrap>();
            go.AddComponent<MobileTouchUI>();
            go.AddComponent<AndroidDataPicker>();
#endif
        }

        private void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Application.targetFrameRate = targetFrameRate;
#endif
        }
    }
}
