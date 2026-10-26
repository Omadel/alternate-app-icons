using UnityEngine;

namespace Javienne.AlternateAppIcons.Tests
{
    /// <summary>
    ///     Image files of the tests: valid ones, encoded once.
    /// </summary>
    internal static class TestIcons
    {
        private static byte[] _iosImage;
        private static byte[] _foreground;
        private static byte[] _background;
        private static byte[] _monochrome;

        public static byte[] IosImage => _iosImage ??= IconChecksTests.Png(IconChecks.IOS_IMAGE_SIZE, IconChecks.IOS_IMAGE_SIZE, new Color32(200, 120, 40, 255), TextureFormat.RGB24);
        public static byte[] Foreground => _foreground ??= IconChecksTests.Png(IconChecks.ANDROID_LAYER_SIZE, IconChecks.ANDROID_LAYER_SIZE, new Color32(200, 120, 40, 0), TextureFormat.RGBA32, new Color32(200, 120, 40, 255));
        public static byte[] Background => _background ??= IconChecksTests.Png(IconChecks.ANDROID_LAYER_SIZE, IconChecks.ANDROID_LAYER_SIZE, new Color32(40, 120, 200, 255), TextureFormat.RGB24);
        public static byte[] Monochrome => _monochrome ??= IconChecksTests.Png(IconChecks.ANDROID_LAYER_SIZE, IconChecks.ANDROID_LAYER_SIZE, new Color32(255, 255, 255, 0), TextureFormat.RGBA32, new Color32(255, 255, 255, 255));

        /// <summary>
        ///     Icon without problem, with a background colour and without monochrome layer.
        /// </summary>
        public static IconFiles Valid(string name)
        {
            return new IconFiles(name, IosImage, Foreground, false, new Color32(255, 128, 0, 255), null, null);
        }
    }
}