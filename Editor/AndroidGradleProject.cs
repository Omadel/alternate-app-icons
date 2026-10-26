using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Adds the alternate icons to the Gradle project of an Android build, in the Unity library module, where the
    ///     launcher activity is: their resources, and their activity aliases in its manifest.
    /// </summary>
    internal static class AndroidGradleProject
    {
        private const string PLATFORM = "Android";

        /// <summary>
        ///     Checks the icons, then writes them into the Unity library module, replacing those of a previous build; with
        ///     an error, the build has no alternate icon.
        /// </summary>
        internal static void AddIcons(string unityLibrary, IReadOnlyList<IconFiles> icons, bool failBuildOnError, Action<string> logWarning)
        {
            var main = Path.Combine(unityLibrary, "src", "main");
            var manifestPath = Path.Combine(main, "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                if (icons.Count > 0) logWarning($"Alternate App Icons: the Android build has no alternate icon, as the Unity library module has no manifest at {manifestPath}.");

                return;
            }

            if (!IconBuildCheck.Run(icons, PLATFORM, issue => issue.ConcernsAndroid, failBuildOnError, logWarning)) icons = Array.Empty<IconFiles>();

            var manifest = File.ReadAllText(manifestPath);
            var withAliases = AndroidManifestAliases.AddAliases(manifest, icons.Select(icon => icon.Name).ToList());
            if (withAliases == null)
            {
                logWarning($"Alternate App Icons: the Android build has no alternate icon, as no activity of {manifestPath} has the main action and the launcher category.");
                icons = Array.Empty<IconFiles>();
                withAliases = AndroidManifestAliases.AddAliases(manifest, Array.Empty<string>());
            }

            AndroidIconResources.Write(Path.Combine(main, "res"), icons);
            if (withAliases != manifest) File.WriteAllText(manifestPath, withAliases);
        }
    }
}