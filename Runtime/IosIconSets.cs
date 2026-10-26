using System.Collections.Generic;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Icon sets of the alternate icons in the asset catalog of the iOS build, one per icon. Xcode lists them in the
    ///     Info.plist by these names, which iOS passes back to change the icon. Their prefix sets them apart from the
    ///     other icon sets of the catalog, the default <c>AppIcon</c> among them.
    /// </summary>
    internal static class IosIconSets
    {
        internal const string PREFIX = "AlternateAppIcon-";

        internal static string IconSetName(string icon)
        {
            return PREFIX + icon;
        }

        /// <summary>
        ///     Icon of an icon set: false for an icon set that is not one of these.
        /// </summary>
        internal static bool TryGetIcon(string iconSet, out string icon)
        {
            icon = string.Empty;
            if (iconSet == null || !iconSet.StartsWith(PREFIX) || iconSet.Length == PREFIX.Length) return false;

            icon = iconSet.Substring(PREFIX.Length);
            return true;
        }

        /// <summary>
        ///     Icons of the icon sets that the Info.plist lists, one per line, leaving out the other icon sets.
        /// </summary>
        internal static string[] ParseIcons(string iconSets)
        {
            var icons = new List<string>();
            foreach (var iconSet in (iconSets ?? string.Empty).Split('\n'))
            {
                if (TryGetIcon(iconSet, out var icon) && !icons.Contains(icon)) icons.Add(icon);
            }

            return icons.ToArray();
        }
    }
}