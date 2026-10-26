using System;
using NUnit.Framework;
using UnityEngine;

namespace Javienne.AlternateAppIcons.Tests
{
    public class PngFileTests
    {
        private static readonly Color32 _opaque = new Color32(200, 120, 40, 255);

        [Test]
        public void IsPng_TooShort_ReturnsFalse()
        {
            Assert.That(PngFile.IsPng(new byte[] { 0x89, 0x50, 0x4E }), Is.False);
        }

        [Test]
        public void HasAlphaChannel_RgbPng_ReturnsFalse()
        {
            Assert.That(PngFile.HasAlphaChannel(IconChecksTests.Png(8, 8, _opaque, TextureFormat.RGB24)), Is.False);
        }

        [Test]
        public void HasAlphaChannel_RgbaPng_ReturnsTrue()
        {
            Assert.That(PngFile.HasAlphaChannel(IconChecksTests.Png(8, 8, _opaque, TextureFormat.RGBA32)), Is.True);
        }

        [Test]
        public void HasAlphaChannel_RgbPngWithTransparencyChunk_ReturnsTrue()
        {
            Assert.That(PngFile.HasAlphaChannel(WithChunkBeforeData(IconChecksTests.Png(8, 8, _opaque, TextureFormat.RGB24), "tRNS")), Is.True);
        }

        [Test]
        public void HasAlphaChannel_RgbPngWithOtherChunk_ReturnsFalse()
        {
            Assert.That(PngFile.HasAlphaChannel(WithChunkBeforeData(IconChecksTests.Png(8, 8, _opaque, TextureFormat.RGB24), "gAMA")), Is.False);
        }

        [Test]
        public void HasAlphaChannel_NotPng_ReturnsFalse()
        {
            Assert.That(PngFile.HasAlphaChannel(new byte[32]), Is.False);
        }

        /// <summary>
        ///     PNG file with an empty chunk of the given type inserted after the header chunk, before the image data. The
        ///     checks read the chunk types only, so its CRC is left at zero.
        /// </summary>
        private static byte[] WithChunkBeforeData(byte[] png, string type)
        {
            const int HEADER_END = 8 + 12 + 13;
            var chunk = new byte[12];
            for (var i = 0; i < 4; i++)
            {
                chunk[4 + i] = (byte)type[i];
            }

            var file = new byte[png.Length + chunk.Length];
            Array.Copy(png, file, HEADER_END);
            Array.Copy(chunk, 0, file, HEADER_END, chunk.Length);
            Array.Copy(png, HEADER_END, file, HEADER_END + chunk.Length, png.Length - HEADER_END);
            return file;
        }
    }
}