using System;
using System.Collections.Generic;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Platform that cannot change the app icon: requests do nothing.
    /// </summary>
    internal class UnsupportedIconPlatform : IIconPlatform
    {
        public bool IsSupported => false;
        public IReadOnlyList<string> Icons => Array.Empty<string>();
        public string CurrentIcon => string.Empty;
        public bool DefersChange => false;

        public void Apply(string icon, Action<string> completed)
        {
            completed("This platform cannot change the app icon.");
        }
    }
}