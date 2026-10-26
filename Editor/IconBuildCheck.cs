using System;
using System.Collections.Generic;
using UnityEditor.Build;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Checks of the alternate icons at the build of a platform, on the problems that concern it. An error leaves
    ///     every alternate icon out of the build, or fails it when the settings ask for it; the problems are logged in a
    ///     single warning.
    /// </summary>
    internal static class IconBuildCheck
    {
        /// <summary>
        ///     Whether the build of a platform gets the alternate icons: true unless an icon has an error on that platform.
        /// </summary>
        /// <exception cref="BuildFailedException">An icon has an error, and the settings ask to fail the build.</exception>
        internal static bool Run(IReadOnlyList<IconFiles> icons, string platform, Func<IconIssue, bool> concerns, bool failBuildOnError, Action<string> logWarning)
        {
            var issues = IconChecks.Check(icons);
            var lines = new List<string>();
            var hasErrors = false;
            for (var i = 0; i < icons.Count; i++)
            {
                foreach (var issue in issues[i])
                {
                    if (!concerns(issue)) continue;

                    lines.Add($"- {Label(icons[i].Name, i)}: {issue.Message}");
                    hasErrors |= issue.IsError;
                }
            }

            if (lines.Count == 0) return true;

            var list = string.Join("\n", lines);
            if (hasErrors && failBuildOnError) throw new BuildFailedException($"Alternate App Icons: some alternate icons have errors, which fail the {platform} build. Fix them in Project Settings > Alternate App Icons:\n{list}");

            logWarning(hasErrors ? $"Alternate App Icons: the {platform} build has no alternate icon, as some have errors. Fix them in Project Settings > Alternate App Icons, where Fail Build On Error makes such a build fail instead:\n{list}" : $"Alternate App Icons: some alternate icons have warnings, which do not stop the {platform} build. See Project Settings > Alternate App Icons:\n{list}");
            return !hasErrors;
        }

        /// <summary>
        ///     Name of an icon in the messages, or its position in the settings when it has none.
        /// </summary>
        private static string Label(string name, int index)
        {
            return string.IsNullOrWhiteSpace(name) ? $"Icon {index + 1}" : name;
        }
    }
}