#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Android;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Platform of Android: the launcher shows the icon of the enabled activity alias, among those the build declares
    ///     for each icon. The change waits for the app to go to the background, as some launchers restart the app or go
    ///     back to the home screen when an alias is disabled. Only Android 8.0 and later, which have adaptive icons, can
    ///     change the icon.
    /// </summary>
    internal class AndroidIconPlatform : IIconPlatform
    {
        private const int ADAPTIVE_ICONS_API_LEVEL = 26;
        private const int GET_ACTIVITIES = 0x1;
        private const int MATCH_DISABLED_COMPONENTS = 0x200;
        private const int DONT_KILL_APP = 0x1;

        private readonly AndroidJavaObject _packageManager;
        private readonly List<Alias> _aliases;
        private readonly string[] _icons;

        private AndroidIconPlatform(AndroidJavaObject packageManager, List<Alias> aliases)
        {
            _packageManager = packageManager;
            _aliases = aliases;
            _icons = aliases.Where(alias => alias.Icon.Length > 0).Select(alias => alias.Icon).ToArray();
        }

        public bool IsSupported => true;
        public IReadOnlyList<string> Icons => _icons;
        public string CurrentIcon => AndroidAliases.CurrentIcon(_aliases.Select(alias => (alias.Icon, IsEnabled(alias))));
        public bool DefersChange => true;

        /// <summary>
        ///     Platform of this device: one that cannot change the icon before Android 8.0, or when the app was built
        ///     without alternate icons.
        /// </summary>
        internal static IIconPlatform Create()
        {
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    if (version.GetStatic<int>("SDK_INT") < ADAPTIVE_ICONS_API_LEVEL) return new UnsupportedIconPlatform();
                }

                var context = AndroidApplication.currentContext;
                var packageName = context.Call<string>("getPackageName");
                var packageManager = context.Call<AndroidJavaObject>("getPackageManager");
                var aliases = ReadAliases(packageManager, packageName);
                if (aliases.All(alias => alias.Icon.Length > 0)) return new UnsupportedIconPlatform();

                var platform = new AndroidIconPlatform(packageManager, aliases);
                platform.RestoreLauncherEntry();
                return platform;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Alternate App Icons: the app icon cannot be changed, as the activity aliases of the icons could not be read: {exception.Message}");
                return new UnsupportedIconPlatform();
            }
        }

        public void Apply(string icon, Action<string> completed)
        {
            var target = _aliases.FirstOrDefault(alias => alias.Icon == icon);
            if (target == null)
            {
                completed($"The app has no activity alias for the {(icon.Length == 0 ? "default icon" : $"{icon} icon")}.");
                return;
            }

            try
            {
                SetEnabled(target, true);
                foreach (var alias in _aliases)
                {
                    if (alias != target && IsEnabled(alias)) SetEnabled(alias, false);
                }
            }
            catch (Exception exception)
            {
                completed(exception.Message);
                return;
            }

            completed(null);
        }

        /// <summary>
        ///     Aliases of the icons in the manifest of the app, disabled ones included.
        /// </summary>
        private static List<Alias> ReadAliases(AndroidJavaObject packageManager, string packageName)
        {
            var aliases = new List<Alias>();
            using (var packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, GET_ACTIVITIES | MATCH_DISABLED_COMPONENTS))
            {
                var activities = packageInfo.Get<AndroidJavaObject[]>("activities") ?? Array.Empty<AndroidJavaObject>();
                foreach (var activity in activities)
                {
                    var name = activity.Get<string>("name");
                    if (activity.Get<string>("targetActivity") != null && AndroidAliases.TryGetIcon(name, out var icon)) aliases.Add(new Alias(icon, activity.Get<bool>("enabled"), new AndroidJavaObject("android.content.ComponentName", packageName, name)));

                    activity.Dispose();
                }
            }

            return aliases;
        }

        /// <summary>
        ///     Enables the alias of the default icon when no alias is enabled, which leaves the app without launcher entry:
        ///     after an update that removed the alternate icon shown.
        /// </summary>
        private void RestoreLauncherEntry()
        {
            if (_aliases.Any(IsEnabled)) return;

            Debug.LogWarning("Alternate App Icons: no activity alias of the icons was enabled, which hides the app from the launcher: the default icon is back.");
            SetEnabled(_aliases.First(alias => alias.Icon.Length == 0), true);
        }

        private bool IsEnabled(Alias alias)
        {
            return AndroidAliases.IsEnabled(_packageManager.Call<int>("getComponentEnabledSetting", alias.Component), alias.IsEnabledInManifest);
        }

        private void SetEnabled(Alias alias, bool isEnabled)
        {
            _packageManager.Call("setComponentEnabledSetting", alias.Component, isEnabled ? AndroidAliases.ENABLED_STATE_ENABLED : AndroidAliases.ENABLED_STATE_DISABLED, DONT_KILL_APP);
        }

        private class Alias
        {
            public Alias(string icon, bool isEnabledInManifest, AndroidJavaObject component)
            {
                Icon = icon;
                IsEnabledInManifest = isEnabledInManifest;
                Component = component;
            }

            /// <summary>
            ///     Icon of the alias, empty for the default icon.
            /// </summary>
            public string Icon { get; }

            public bool IsEnabledInManifest { get; }
            public AndroidJavaObject Component { get; }
        }
    }
}
#endif