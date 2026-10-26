using System;
using System.Collections.Generic;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     What a platform does to change the app icon. Icons are named as in the settings, the default icon by an empty
    ///     name.
    /// </summary>
    internal interface IIconPlatform
    {
        /// <summary>
        ///     Whether this device can change the app icon.
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        ///     Names of the alternate icons the app was built with.
        /// </summary>
        IReadOnlyList<string> Icons { get; }

        /// <summary>
        ///     Icon shown now, empty for the default icon.
        /// </summary>
        string CurrentIcon { get; }

        /// <summary>
        ///     Whether a change waits for the app to go to the background, so that a restart of the app goes unseen.
        /// </summary>
        bool DefersChange { get; }

        /// <summary>
        ///     Shows an icon, then calls back on the main thread with null, or with the error when it failed.
        /// </summary>
        void Apply(string icon, Action<string> completed);
    }
}