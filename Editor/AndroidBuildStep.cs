#if UNITY_ANDROID
using System.Linq;
using UnityEditor.Android;
using UnityEngine;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Adds the alternate icons of the settings to the Gradle project of the Android builds. Compiled only for the
    ///     Android platform, as its interface comes with the Android build support.
    /// </summary>
    internal class AndroidBuildStep : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 0;

        /// <summary>
        ///     Unity passes the folder of the Unity library module.
        /// </summary>
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var settings = AlternateAppIconsSettings.instance;
            AndroidGradleProject.AddIcons(path, settings.Icons.Select(IconFiles.Read).ToList(), settings.FailBuildOnError, Debug.LogWarning);
        }
    }
}
#endif