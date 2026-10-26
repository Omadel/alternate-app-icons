using System.Collections.Generic;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Activity aliases of the Android build, one per icon: the launcher shows the app with the icon of the alias that
    ///     is enabled. The build step declares them and the Android platform enables them; their names do not depend on
    ///     the package name of the app.
    /// </summary>
    internal static class AndroidAliases
    {
        internal const string PREFIX = "com.javienne.alternateappicons.";

        /// <summary>
        ///     Alias of the default icon, without icon of its own: it shows the icon of the application.
        /// </summary>
        internal const string DEFAULT_ALIAS = PREFIX + "DefaultIcon";

        internal const string ICON_ALIAS_PREFIX = PREFIX + "icon.";

        /// <summary>
        ///     <c>PackageManager.COMPONENT_ENABLED_STATE_DEFAULT</c>: the component is enabled as the manifest declares it.
        /// </summary>
        internal const int ENABLED_STATE_DEFAULT = 0;

        internal const int ENABLED_STATE_ENABLED = 1;
        internal const int ENABLED_STATE_DISABLED = 2;

        /// <summary>
        ///     Name of the alias of an icon, empty for the default icon.
        /// </summary>
        internal static string AliasName(string icon)
        {
            return icon.Length == 0 ? DEFAULT_ALIAS : ICON_ALIAS_PREFIX + icon;
        }

        /// <summary>
        ///     Icon of an alias, empty for the default icon: false for an activity that is not one of these aliases.
        /// </summary>
        internal static bool TryGetIcon(string alias, out string icon)
        {
            icon = string.Empty;
            if (alias == DEFAULT_ALIAS) return true;
            if (alias == null || !alias.StartsWith(ICON_ALIAS_PREFIX) || alias.Length == ICON_ALIAS_PREFIX.Length) return false;

            icon = alias.Substring(ICON_ALIAS_PREFIX.Length);
            return true;
        }

        /// <summary>
        ///     Whether a component is enabled, from its enabled state in the package manager and in the manifest.
        /// </summary>
        internal static bool IsEnabled(int enabledState, bool isEnabledInManifest)
        {
            return enabledState == ENABLED_STATE_ENABLED || (enabledState == ENABLED_STATE_DEFAULT && isEnabledInManifest);
        }

        /// <summary>
        ///     Icon of the enabled alias, empty for the default icon. Should a change have stopped half way, with the
        ///     alias of the new icon enabled before the one of the old icon is disabled, the alternate icon wins.
        /// </summary>
        internal static string CurrentIcon(IEnumerable<(string icon, bool isEnabled)> aliases)
        {
            foreach (var (icon, isEnabled) in aliases)
            {
                if (isEnabled && icon.Length > 0) return icon;
            }

            return string.Empty;
        }
    }
}