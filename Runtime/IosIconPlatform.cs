#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Platform of iOS: the alternate icons are icon sets that Xcode lists in the Info.plist, which
    ///     <c>setAlternateIconName</c> shows. iOS shows an alert at each change, then calls back once the icon is in
    ///     place. The native side is <c>Plugins/iOS/AlternateAppIcons.mm</c>.
    /// </summary>
    internal class IosIconPlatform : IIconPlatform
    {
        /// <summary>
        ///     Callback of a change, with the error or null.
        /// </summary>
        private delegate void Completion(int request, string error);

        private static readonly Dictionary<int, Action<string>> _requests = new Dictionary<int, Action<string>>();
        private static readonly Completion _onCompleted = OnCompleted;
        private static int _lastRequest;

        private readonly string[] _icons;

        private IosIconPlatform(string[] icons)
        {
            _icons = icons;
        }

        public bool IsSupported => true;
        public IReadOnlyList<string> Icons => _icons;
        public string CurrentIcon => IosIconSets.TryGetIcon(AlternateAppIcons_GetAlternateIconName(), out var icon) ? icon : string.Empty;
        public bool DefersChange => false;

        /// <summary>
        ///     Platform of this device: one that cannot change the icon when iOS does not allow it, or when the app was
        ///     built without alternate icons.
        /// </summary>
        internal static IIconPlatform Create()
        {
            if (AlternateAppIcons_SupportsAlternateIcons() == 0) return new UnsupportedIconPlatform();

            var icons = IosIconSets.ParseIcons(AlternateAppIcons_GetIconSets());
            return icons.Length == 0 ? new UnsupportedIconPlatform() : new IosIconPlatform(icons);
        }

        public void Apply(string icon, Action<string> completed)
        {
            var request = ++_lastRequest;
            _requests[request] = completed;
            AlternateAppIcons_SetAlternateIconName(icon.Length == 0 ? null : IosIconSets.IconSetName(icon), request, _onCompleted);
        }

        [MonoPInvokeCallback(typeof(Completion))]
        private static void OnCompleted(int request, string error)
        {
            if (_requests.Remove(request, out var completed)) completed(error);
        }

        [DllImport("__Internal")]
        private static extern int AlternateAppIcons_SupportsAlternateIcons();

        [DllImport("__Internal")]
        private static extern string AlternateAppIcons_GetIconSets();

        [DllImport("__Internal")]
        private static extern string AlternateAppIcons_GetAlternateIconName();

        [DllImport("__Internal")]
        private static extern void AlternateAppIcons_SetAlternateIconName(string iconSet, int request, Completion completion);
    }
}
#endif