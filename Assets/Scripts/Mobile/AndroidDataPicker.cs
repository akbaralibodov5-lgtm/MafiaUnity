using System;
using System.IO;
using UnityEngine;

namespace MafiaUnity
{
    /// <summary>
    /// Android equivalent of the desktop Mafia game-folder picker.
    /// The native SAF picker imports the selected folder's .dta archives into
    /// app-private storage, so scoped-storage rules do not break the engine.
    /// </summary>
    public sealed class AndroidDataPicker : MonoBehaviour
    {
        public static AndroidDataPicker Instance { get; private set; }

        public string Status { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            gameObject.name = "AndroidDataPicker";
            DontDestroyOnLoad(gameObject);
        }

        public void PickFolder()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Status = "Select your original Mafia game folder...";
            using (var playerActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = playerActivity.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var picker = new AndroidJavaClass("com.mafiaunity.android.MafiaUnityActivity"))
            {
                var destination = Path.Combine(Application.persistentDataPath, "MafiaData");
                picker.CallStatic("openMafiaFolderPicker", destination);
            }
#else
            Status = "Android data picker is available only on Android.";
#endif
        }

        public void OnFolderImported(string path)
        {
            try
            {
                if (!GameAPI.instance.SetGamePath(path))
                {
                    OnFolderImportFailed("Imported data failed engine validation.");
                    return;
                }

                GameAPI.instance.cvarManager.ForceSet(
                    "gamePath",
                    path,
                    CvarManager.CvarMode.Archived);
                GameAPI.instance.cvarManager.SaveMainConfig();

                Status = "Mafia data imported successfully.";
                Debug.Log(Status + " " + path);

                var setup = FindObjectOfType<SetupGUI>();
                if (setup != null)
                {
                    if (setup.pathSelection != null) setup.pathSelection.SetActive(false);
                    if (setup.mainMenu != null) setup.mainMenu.SetActive(true);
                    setup.SetupDefaultBackground();
                }
            }
            catch (Exception ex)
            {
                OnFolderImportFailed(ex.Message);
            }
        }

        public void OnFolderImportFailed(string message)
        {
            Status = message;
            Debug.LogError("Mafia data import failed: " + message);
        }
    }
}
