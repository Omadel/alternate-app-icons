using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Adds the alternate icons to the Gradle project of an Android build: their resources in the Unity library
    ///     module, where the launcher activity is, and their activity aliases in the manifest of the launcher module, with
    ///     what removes the launcher filter of this activity from the merged manifest. The manifest of the Unity library
    ///     stays as Unity writes it.
    /// </summary>
    internal static class AndroidGradleProject
    {
        private const string PLATFORM = "Android";

        /// <summary>
        ///     Checks the icons, then writes them into the Gradle project, replacing those of a previous build; with an
        ///     error, the build has no alternate icon.
        /// </summary>
        internal static void AddIcons(string unityLibrary, IReadOnlyList<IconFiles> icons, bool failBuildOnError, Action<string> logWarning)
        {
            var main = Path.Combine(unityLibrary, "src", "main");
            var libraryPath = Path.Combine(main, "AndroidManifest.xml");
            var launcherPath = Path.GetFullPath(Path.Combine(unityLibrary, "..", "launcher", "src", "main", "AndroidManifest.xml"));
            var missing = new[] { libraryPath, launcherPath }.FirstOrDefault(path => !File.Exists(path));
            if (missing != null)
            {
                if (icons.Count > 0) logWarning($"Alternate App Icons: the Android build has no alternate icon, as the Gradle project has no manifest at {missing}.");

                return;
            }

            if (!IconBuildCheck.Run(icons, PLATFORM, issue => issue.ConcernsAndroid, failBuildOnError, logWarning)) icons = Array.Empty<IconFiles>();

            var library = File.ReadAllText(libraryPath);
            var launcher = File.ReadAllText(launcherPath);
            var withAliases = AndroidManifestAliases.AddAliases(library, launcher, icons.Select(icon => icon.Name).ToList());
            if (withAliases == null)
            {
                logWarning($"Alternate App Icons: the Android build has no alternate icon, as no activity of {libraryPath} has the main action and the launcher category.");
                icons = Array.Empty<IconFiles>();
                withAliases = AndroidManifestAliases.AddAliases(library, launcher, Array.Empty<string>());
            }

            AndroidIconResources.Write(Path.Combine(main, "res"), icons);
            if (withAliases != launcher) File.WriteAllText(launcherPath, withAliases);
        }
    }
}