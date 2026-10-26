using System;
using UnityEngine;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     An alternate icon of the project settings: its name, which the game passes to the API to show it, and its
    ///     images for each platform, copied as they are into the builds.
    /// </summary>
    [Serializable]
    internal class AlternateIcon
    {
        [SerializeField] private string _name = string.Empty;
        [SerializeField] private Texture2D _iosImage;
        [SerializeField] private Texture2D _androidForeground;
        [SerializeField] private AndroidBackgroundKind _androidBackgroundKind;
        [SerializeField] private Color _androidBackgroundColour = Color.white;
        [SerializeField] private Texture2D _androidBackgroundImage;
        [SerializeField] private Texture2D _androidMonochrome;

        public string Name => _name;

        /// <summary>
        ///     Square PNG of 1024 px without transparency, from which iOS scales the icon down for every place it shows it.
        /// </summary>
        public Texture2D IosImage => _iosImage;

        /// <summary>
        ///     Foreground layer of the Android adaptive icon: PNG of 432 px, the 108 dp layer at the xxxhdpi density.
        /// </summary>
        public Texture2D AndroidForeground => _androidForeground;

        public AndroidBackgroundKind AndroidBackgroundKind => _androidBackgroundKind;
        public Color AndroidBackgroundColour => _androidBackgroundColour;

        /// <summary>
        ///     Background layer of the Android adaptive icon when it is an image: PNG of 432 px.
        /// </summary>
        public Texture2D AndroidBackgroundImage => _androidBackgroundImage;

        /// <summary>
        ///     Optional monochrome layer of the Android adaptive icon, which Android 13 and later tint when themed icons are
        ///     on: PNG of 432 px, of which Android only keeps the opacity.
        /// </summary>
        public Texture2D AndroidMonochrome => _androidMonochrome;
    }
}