using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Decides what a request to change the app icon does on a platform: nothing when the icon is already in place,
    ///     an error for an unknown name, the change right away, or a change kept until the app goes to the background.
    /// </summary>
    internal class IconSwitcher
    {
        private const string LOG_TAG = "Alternate App Icons";

        private readonly IIconPlatform _platform;
        private readonly ILogger _logger;
        private bool _isApplying;

        /// <summary>
        ///     Switcher of a platform, logging to the Unity console unless given another logger.
        /// </summary>
        public IconSwitcher(IIconPlatform platform, ILogger logger = null)
        {
            _platform = platform;
            _logger = logger ?? Debug.unityLogger;
        }

        public bool IsSupported => _platform.IsSupported;

        /// <summary>
        ///     Icons of the platform in alphabetical order, the same on every platform: iOS lists them in no order.
        /// </summary>
        public IReadOnlyList<string> Icons => _platform.IsSupported ? _platform.Icons.OrderBy(icon => icon, StringComparer.OrdinalIgnoreCase).ToArray() : Array.Empty<string>();
        public string CurrentIcon => _platform.IsSupported ? _platform.CurrentIcon : string.Empty;

        /// <summary>
        ///     Icon waiting for the app to go to the background: null when none, empty for the default icon.
        /// </summary>
        public string PendingIcon { get; private set; }

        /// <summary>
        ///     Requests an icon, null or empty for the default icon. The awaitable ends once the change is done, kept for
        ///     later, or refused.
        /// </summary>
        public Awaitable<IconChangeResult> SetIcon(string name)
        {
            var source = new AwaitableCompletionSource<IconChangeResult>();
            Request(name, result => source.SetResult(result));
            return source.Awaitable;
        }

        /// <summary>
        ///     Requests an icon, null or empty for the default icon, and calls back with the result.
        /// </summary>
        internal void Request(string name, Action<IconChangeResult> completed)
        {
            if (!_platform.IsSupported)
            {
                completed(new IconChangeResult(IconChangeStatus.NotSupported, name ?? string.Empty));
                return;
            }

            if (!TryResolve(name, out var icon))
            {
                var error = $"No alternate icon is named \"{name}\". The app has {Describe(Icons)}.";
                _logger.LogError(LOG_TAG, error);
                completed(new IconChangeResult(IconChangeStatus.Failed, name, error));
                return;
            }

            if (_platform.DefersChange)
            {
                var isInPlace = icon == _platform.CurrentIcon;
                PendingIcon = isInPlace ? null : icon;
                completed(new IconChangeResult(isInPlace ? IconChangeStatus.AlreadySet : IconChangeStatus.Pending, icon));
                return;
            }

            if (icon == _platform.CurrentIcon)
            {
                completed(new IconChangeResult(IconChangeStatus.AlreadySet, icon));
                return;
            }

            if (_isApplying)
            {
                completed(new IconChangeResult(IconChangeStatus.Failed, icon, "Another icon change is in progress: wait for its result before asking for another icon."));
                return;
            }

            _isApplying = true;
            _platform.Apply(icon, error =>
            {
                _isApplying = false;
                if (error != null) _logger.LogWarning(LOG_TAG, $"The {Describe(icon)} could not be set: {error}");

                completed(new IconChangeResult(error == null ? IconChangeStatus.Changed : IconChangeStatus.Failed, icon, error));
            });
        }

        /// <summary>
        ///     Applies the pending icon, if any, now that the app goes to the background.
        /// </summary>
        internal void ApplyPending()
        {
            if (PendingIcon == null) return;

            var icon = PendingIcon;
            PendingIcon = null;
            _platform.Apply(icon, error =>
            {
                if (error != null) _logger.LogWarning(LOG_TAG, $"The {Describe(icon)} could not be set: {error}");
            });
        }

        /// <summary>
        ///     Name of the requested icon as the app declares it, empty for the default icon. Names are compared regardless
        ///     of case, as they are unique regardless of case.
        /// </summary>
        private bool TryResolve(string name, out string icon)
        {
            icon = string.Empty;
            if (string.IsNullOrEmpty(name)) return true;

            foreach (var declared in _platform.Icons)
            {
                if (!string.Equals(name, declared, StringComparison.OrdinalIgnoreCase)) continue;

                icon = declared;
                return true;
            }

            return false;
        }

        private static string Describe(IReadOnlyList<string> icons)
        {
            return icons.Count == 0 ? "no alternate icon" : $"these alternate icons: {string.Join(", ", icons)}";
        }

        private static string Describe(string icon)
        {
            return icon.Length == 0 ? "default icon" : $"{icon} icon";
        }
    }
}