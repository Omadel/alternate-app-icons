#if UNITY_IOS
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Adds the alternate icons of the settings to the Xcode project of the iOS builds, and names them in the build
    ///     settings of the app target. Compiled only for the iOS platform, as the Xcode API comes with the iOS build
    ///     support.
    /// </summary>
    internal class IosBuildStep : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;

            var settings = AlternateAppIconsSettings.instance;
            var iconSets = IosXcodeProject.AddIcons(report.summary.outputPath, settings.Icons.Select(IconFiles.Read).ToList(), settings.FailBuildOnError, Debug.LogWarning);
            var projectPath = PBXProject.GetPBXProjectPath(report.summary.outputPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            var target = project.GetUnityMainTargetGuid();
            if (iconSets.Count == 0 && project.GetBuildPropertyForAnyConfig(target, IosXcodeProject.ALTERNATE_ICONS_SETTING) == null) return;

            project.SetBuildProperty(target, IosXcodeProject.ALTERNATE_ICONS_SETTING, string.Join(" ", iconSets));
            project.WriteToFile(projectPath);
        }
    }
}
#endif