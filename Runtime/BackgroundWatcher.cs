using UnityEngine;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Hidden object that applies the pending icon when the app goes to the background, created with the first change
    ///     a platform defers.
    /// </summary>
    internal class BackgroundWatcher : MonoBehaviour
    {
        private static BackgroundWatcher _instance;

        internal static void Ensure()
        {
            if (_instance != null) return;

            var gameObject = new GameObject(nameof(BackgroundWatcher)) { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(gameObject);
            _instance = gameObject.AddComponent<BackgroundWatcher>();
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused) AppIcon.ApplyPending();
        }
    }
}