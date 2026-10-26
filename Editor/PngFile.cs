using System.Text;
using UnityEngine;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Reads PNG files as the builds copy them: their signature, their pixels and whether they store opacity.
    /// </summary>
    internal static class PngFile
    {
        private const int COLOUR_TYPE_OFFSET = 25;
        private const byte GREYSCALE_ALPHA = 4;
        private const byte TRUE_COLOUR_ALPHA = 6;

        private static readonly byte[] _signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>
        ///     Whether the bytes start with the PNG signature.
        /// </summary>
        internal static bool IsPng(byte[] file)
        {
            if (file.Length < _signature.Length) return false;

            for (var i = 0; i < _signature.Length; i++)
            {
                if (file[i] != _signature[i]) return false;
            }

            return true;
        }

        /// <summary>
        ///     Pixels of a PNG file, decoded as they are stored.
        /// </summary>
        internal static bool TryReadPixels(byte[] file, out int width, out int height, out Color32[] pixels)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                var loaded = texture.LoadImage(file);
                width = texture.width;
                height = texture.height;
                pixels = loaded ? texture.GetPixels32() : null;
                return loaded;
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        /// <summary>
        ///     Whether a PNG file stores opacity, even if all its pixels are opaque: a colour type with alpha, or a
        ///     transparency chunk before the image data.
        /// </summary>
        internal static bool HasAlphaChannel(byte[] file)
        {
            if (!IsPng(file) || file.Length <= COLOUR_TYPE_OFFSET) return false;

            var colourType = file[COLOUR_TYPE_OFFSET];
            if (colourType == GREYSCALE_ALPHA || colourType == TRUE_COLOUR_ALPHA) return true;

            var offset = _signature.Length;
            while (offset + 8 <= file.Length)
            {
                var length = (file[offset] << 24) | (file[offset + 1] << 16) | (file[offset + 2] << 8) | file[offset + 3];
                var type = Encoding.ASCII.GetString(file, offset + 4, 4);
                if (type == "tRNS") return true;

                if (type == "IDAT" || length < 0) return false;

                offset += 12 + length;
            }

            return false;
        }
    }
}