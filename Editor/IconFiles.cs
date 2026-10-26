using System.IO;
using UnityEditor;
using UnityEngine;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Name and image files of an alternate icon, as the checks and the build read them. An image is null when it is
    ///     not set, and empty when its asset has no file.
    /// </summary>
    internal class IconFiles
    {
        public IconFiles(string name, byte[] iosImage, byte[] androidForeground, bool usesBackgroundImage, byte[] androidBackground, byte[] androidMonochrome)
        {
            Name = name;
            IosImage = iosImage;
            AndroidForeground = androidForeground;
            UsesBackgroundImage = usesBackgroundImage;
            AndroidBackground = androidBackground;
            AndroidMonochrome = androidMonochrome;
        }

        public string Name { get; }
        public byte[] IosImage { get; }
        public byte[] AndroidForeground { get; }

        /// <summary>
        ///     Whether the background layer is an image rather than a colour.
        /// </summary>
        public bool UsesBackgroundImage { get; }

        public byte[] AndroidBackground { get; }
        public byte[] AndroidMonochrome { get; }

        /// <summary>
        ///     Files of an icon of the settings.
        /// </summary>
        internal static IconFiles Read(AlternateIcon icon)
        {
            var usesBackgroundImage = icon.AndroidBackgroundKind == AndroidBackgroundKind.Image;
            return new IconFiles(icon.Name, ReadFile(icon.IosImage), ReadFile(icon.AndroidForeground), usesBackgroundImage, usesBackgroundImage ? ReadFile(icon.AndroidBackgroundImage) : null, ReadFile(icon.AndroidMonochrome));
        }

        /// <summary>
        ///     Bytes of the file of an image asset: null without image, empty when the asset has no file.
        /// </summary>
        internal static byte[] ReadFile(Texture2D image)
        {
            if (image == null) return null;

            var path = AssetDatabase.GetAssetPath(image);
            var fullPath = string.IsNullOrEmpty(path) ? null : Path.GetFullPath(path);
            return fullPath != null && File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : new byte[0];
        }
    }
}