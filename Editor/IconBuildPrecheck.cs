using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Fails an iOS or Android build at its start when an icon has an error and the settings ask to fail the build,
    ///     rather than once the player is built, when the build steps add the icons. The build steps log the problems.
    /// </summary>
    internal class IconBuildPrecheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var settings = AlternateAppIconsSettings.instance;
            if (!settings.FailBuildOnError || settings.Icons.Count == 0) return;

            Func<IconIssue, bool> concerns;
            switch (report.summary.platform)
            {
                case BuildTarget.iOS:
                    concerns = issue => issue.ConcernsIos;
                    break;
                case BuildTarget.Android:
                    concerns = issue => issue.ConcernsAndroid;
                    break;
                default:
                    return;
            }

            IconBuildCheck.Run(settings.Icons.Select(IconFiles.Read).ToList(), report.summary.platform.ToString(), concerns, true, _ => { });
        }
    }
}