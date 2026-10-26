using System;
using System.Collections.Generic;
using System.IO;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Adds the alternate icons to the Xcode project of an iOS build: an icon set per icon in the asset catalog of the
    ///     app, with its single image of 1024 px copied as it is, from which Xcode makes every size. The build setting
    ///     <see cref="ALTERNATE_ICONS_SETTING" /> then makes Xcode build them and list them in the Info.plist.
    /// </summary>
    internal static class IosXcodeProject
    {
        internal const string ALTERNATE_ICONS_SETTING = "ASSETCATALOG_COMPILER_ALTERNATE_APPICON_NAMES";
        internal const string IMAGE_FILE = "Icon.png";
        private const string PLATFORM = "iOS";

        /// <summary>
        ///     Single size icon set of iOS, for every device.
        /// </summary>
        private const string CONTENTS = "{\n  \"images\" : [\n    {\n      \"filename\" : \"" + IMAGE_FILE + "\",\n      \"idiom\" : \"universal\",\n      \"platform\" : \"ios\",\n      \"size\" : \"1024x1024\"\n    }\n  ],\n  \"info\" : {\n    \"author\" : \"xcode\",\n    \"version\" : 1\n  }\n}\n";

        /// <summary>
        ///     Checks the icons, then writes their icon sets into the asset catalog of the app, replacing those of a
        ///     previous build; with an error, the build has no alternate icon.
        /// </summary>
        /// <returns>Names of the icon sets, for <see cref="ALTERNATE_ICONS_SETTING" />: empty without alternate icon.</returns>
        internal static IReadOnlyList<string> AddIcons(string xcodeProject, IReadOnlyList<IconFiles> icons, bool failBuildOnError, Action<string> logWarning)
        {
            var catalog = Path.Combine(xcodeProject, "Unity-iPhone", "Images.xcassets");
            if (!Directory.Exists(catalog))
            {
                if (icons.Count > 0) logWarning($"Alternate App Icons: the iOS build has no alternate icon, as the Xcode project has no asset catalog at {catalog}.");

                return Array.Empty<string>();
            }

            if (!IconBuildCheck.Run(icons, PLATFORM, issue => issue.ConcernsIos, failBuildOnError, logWarning)) icons = Array.Empty<IconFiles>();

            foreach (var previous in Directory.GetDirectories(catalog, IosIconSets.PREFIX + "*.appiconset"))
            {
                Directory.Delete(previous, true);
            }

            var iconSets = new List<string>();
            foreach (var icon in icons)
            {
                var iconSet = IosIconSets.IconSetName(icon.Name);
                var folder = Directory.CreateDirectory(Path.Combine(catalog, iconSet + ".appiconset")).FullName;
                File.WriteAllBytes(Path.Combine(folder, IMAGE_FILE), icon.IosImage);
                File.WriteAllText(Path.Combine(folder, "Contents.json"), CONTENTS);
                iconSets.Add(iconSet);
            }

            return iconSets;
        }
    }
}