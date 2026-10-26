using System.Collections.Generic;
using UnityEngine;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Changes the app icon from the game, among the alternate icons declared in Project Settings > Alternate App
    ///     Icons, each called by its name; the default icon is the one of the Player Settings.
    ///     <list type="bullet">
    ///         <item>iOS shows an alert at each change: change the icon at a calm moment.</item>
    ///         <item>
    ///             Android applies the change when the app goes to the background, as some devices restart the app or
    ///             go back to the home screen when the icon changes. Android 7.1 and earlier cannot change it.
    ///         </item>
    ///         <item>The editor simulates the change and logs it.</item>
    ///         <item>Other platforms cannot change the icon: requests do nothing.</item>
    ///     </list>
    /// </summary>
    public static class AppIcon
    {
        private static IconSwitcher _switcher;

        /// <summary>
        ///     Whether this platform and device can change the app icon.
        /// </summary>
        public static bool IsSupported => Switcher.IsSupported;

        /// <summary>
        ///     Names of the alternate icons the app was built with.
        /// </summary>
        public static IReadOnlyList<string> Icons => Switcher.Icons;

        /// <summary>
        ///     Icon shown now, empty for the default icon.
        /// </summary>
        public static string CurrentIcon => Switcher.CurrentIcon;

        /// <summary>
        ///     Icon waiting for the app to go to the background (Android): null when none, empty for the default icon.
        /// </summary>
        public static string PendingIcon => Switcher.PendingIcon;

        /// <summary>
        ///     Platform of the editor, set by the editor assembly: a simulation of the change.
        /// </summary>
        internal static IIconPlatform EditorPlatform { get; set; }

        private static IconSwitcher Switcher => _switcher ??= new IconSwitcher(CreatePlatform());

        /// <summary>
        ///     Shows an alternate icon by its name, or the default icon for null or an empty name. Asking for the icon
        ///     already in place does nothing; an unknown name is an error, logged.
        /// </summary>
        /// <returns>The result, once the icon is changed, kept until the app goes to the background, or refused.</returns>
        public static Awaitable<IconChangeResult> SetIcon(string name)
        {
            var result = Switcher.SetIcon(name);
            if (Switcher.PendingIcon != null) BackgroundWatcher.Ensure();

            return result;
        }

        internal static void ApplyPending()
        {
            Switcher.ApplyPending();
        }

        private static IIconPlatform CreatePlatform()
        {
#if UNITY_EDITOR
            return EditorPlatform ?? new UnsupportedIconPlatform();
#else
            return new UnsupportedIconPlatform();
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            _switcher = null;
        }
    }
}