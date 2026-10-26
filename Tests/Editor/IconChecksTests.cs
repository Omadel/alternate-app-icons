using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Javienne.AlternateAppIcons.Tests
{
    public class IconChecksTests
    {
        private const int IOS = IconChecks.IOS_IMAGE_SIZE;
        private const int ANDROID = IconChecks.ANDROID_LAYER_SIZE;

        private static readonly Color32 _opaque = new Color32(200, 120, 40, 255);
        private static readonly Color32 _white = new Color32(255, 255, 255, 255);
        private static readonly Color32 _clear = new Color32(255, 255, 255, 0);

        [Test]
        public void CheckNames_ValidDistinctNames_ReturnsNoIssue()
        {
            var issues = IconChecks.CheckNames(new[] { "Hellhound", "Scifi", "Chocolate" });
            Assert.That(issues.All(icon => icon.Count == 0), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void CheckNames_EmptyName_ReturnsEmptyName(string name)
        {
            Assert.That(Kinds(IconChecks.CheckNames(new[] { name })[0]), Is.EqualTo(new[] { IconIssueKind.EmptyName }));
        }

        [Test]
        public void CheckNames_InvalidName_ReturnsInvalidName()
        {
            Assert.That(Kinds(IconChecks.CheckNames(new[] { "Golden Retriever" })[0]), Is.EqualTo(new[] { IconIssueKind.InvalidName }));
        }

        [Test]
        public void CheckNames_SameNameInAnotherCase_ReturnsDuplicateForBoth()
        {
            var issues = IconChecks.CheckNames(new[] { "Scifi", "Hellhound", "SCIFI" });
            Assert.That(Kinds(issues[0]), Is.EqualTo(new[] { IconIssueKind.DuplicateName }));
            Assert.That(issues[1], Is.Empty);
            Assert.That(Kinds(issues[2]), Is.EqualTo(new[] { IconIssueKind.DuplicateName }));
        }

        [Test]
        public void CheckNames_TwoEmptyNames_ReturnsNoDuplicate()
        {
            var issues = IconChecks.CheckNames(new[] { string.Empty, string.Empty });
            Assert.That(issues.Select(Kinds), Is.All.EqualTo(new[] { IconIssueKind.EmptyName }));
        }

        [Test]
        public void CheckImage_RequiredImageNotSet_ReturnsMissingImage()
        {
            var fields = new[] { IconField.IosImage, IconField.AndroidForeground, IconField.AndroidBackground };
            Assert.That(fields.Select(field => Kinds(IconChecks.CheckImage(null, field))), Is.All.EqualTo(new[] { IconIssueKind.MissingImage }));
        }

        [Test]
        public void CheckImage_MonochromeNotSet_ReturnsNoIssue()
        {
            Assert.That(IconChecks.CheckImage(null, IconField.AndroidMonochrome), Is.Empty);
        }

        [Test]
        public void CheckImage_AssetWithoutFile_ReturnsNotPng()
        {
            Assert.That(Kinds(IconChecks.CheckImage(new byte[0], IconField.IosImage)), Is.EqualTo(new[] { IconIssueKind.NotPng }));
        }

        [Test]
        public void CheckImage_JpgFile_ReturnsNotPng()
        {
            var texture = Texture(ANDROID, ANDROID, _opaque, TextureFormat.RGB24);
            var jpg = texture.EncodeToJPG();
            Object.DestroyImmediate(texture);
            Assert.That(Kinds(IconChecks.CheckImage(jpg, IconField.AndroidForeground)), Is.EqualTo(new[] { IconIssueKind.NotPng }));
        }

        [Test]
        public void CheckImage_OpaqueIosImageWithoutAlpha_ReturnsNoIssue()
        {
            Assert.That(IconChecks.CheckImage(Png(IOS, IOS, _opaque, TextureFormat.RGB24), IconField.IosImage), Is.Empty);
        }

        [Test]
        public void CheckImage_IosImageAtAndroidSize_ReturnsWrongSize()
        {
            Assert.That(Kinds(IconChecks.CheckImage(Png(ANDROID, ANDROID, _opaque, TextureFormat.RGB24), IconField.IosImage)), Is.EqualTo(new[] { IconIssueKind.WrongSize }));
        }

        [Test]
        public void CheckImage_TransparentPixelInIosImage_ReturnsTransparentIosImage()
        {
            var issues = IconChecks.CheckImage(Png(IOS, IOS, _opaque, TextureFormat.RGBA32, new Color32(0, 0, 0, 254)), IconField.IosImage);
            Assert.That(Kinds(issues), Is.EqualTo(new[] { IconIssueKind.TransparentIosImage }));
            Assert.That(issues[0].IsError, Is.True);
        }

        [Test]
        public void CheckImage_OpaqueIosImageWithAlphaChannel_ReturnsAlphaChannelWarning()
        {
            var issues = IconChecks.CheckImage(Png(IOS, IOS, _opaque, TextureFormat.RGBA32), IconField.IosImage);
            Assert.That(Kinds(issues), Is.EqualTo(new[] { IconIssueKind.IosAlphaChannel }));
            Assert.That(issues[0].IsError, Is.False);
        }

        [Test]
        public void CheckImage_TransparentForeground_ReturnsNoIssue()
        {
            Assert.That(IconChecks.CheckImage(Png(ANDROID, ANDROID, _clear, TextureFormat.RGBA32, _opaque), IconField.AndroidForeground), Is.Empty);
        }

        [Test]
        public void CheckImage_AndroidLayerAtIosSize_ReturnsWrongSize()
        {
            var file = Png(IOS, IOS, _white, TextureFormat.RGB24);
            var fields = new[] { IconField.AndroidForeground, IconField.AndroidBackground, IconField.AndroidMonochrome };
            Assert.That(fields.Select(field => Kinds(IconChecks.CheckImage(file, field))), Is.All.EqualTo(new[] { IconIssueKind.WrongSize }));
        }

        [Test]
        public void CheckImage_AndroidLayerNotSquare_ReturnsWrongSize()
        {
            Assert.That(Kinds(IconChecks.CheckImage(Png(ANDROID, 400, _opaque, TextureFormat.RGB24), IconField.AndroidForeground)), Is.EqualTo(new[] { IconIssueKind.WrongSize }));
        }

        [Test]
        public void CheckImage_TransparentBackground_ReturnsTransparentBackgroundWarning()
        {
            var issues = IconChecks.CheckImage(Png(ANDROID, ANDROID, _opaque, TextureFormat.RGBA32, _clear), IconField.AndroidBackground);
            Assert.That(Kinds(issues), Is.EqualTo(new[] { IconIssueKind.TransparentBackground }));
            Assert.That(issues[0].IsError, Is.False);
        }

        [Test]
        public void CheckImage_WhiteMonochrome_ReturnsNoIssue()
        {
            Assert.That(IconChecks.CheckImage(Png(ANDROID, ANDROID, _clear, TextureFormat.RGBA32, _white), IconField.AndroidMonochrome), Is.Empty);
        }

        [Test]
        public void CheckImage_ColouredMonochrome_ReturnsNotWhiteWarning()
        {
            var issues = IconChecks.CheckImage(Png(ANDROID, ANDROID, _clear, TextureFormat.RGBA32, _opaque), IconField.AndroidMonochrome);
            Assert.That(Kinds(issues), Is.EqualTo(new[] { IconIssueKind.NotWhiteMonochrome }));
            Assert.That(issues[0].IsError, Is.False);
        }

        [Test]
        public void Check_ValidIconWithColourBackground_ReturnsNoIssue()
        {
            var icon = new IconFiles("Hellhound", Png(IOS, IOS, _opaque, TextureFormat.RGB24), Png(ANDROID, ANDROID, _clear, TextureFormat.RGBA32, _opaque), false, Color.white, null, null);
            Assert.That(IconChecks.Check(new[] { icon })[0], Is.Empty);
        }

        [Test]
        public void Check_BackgroundImageNotSet_ReturnsMissingBackground()
        {
            var icon = new IconFiles("Hellhound", Png(IOS, IOS, _opaque, TextureFormat.RGB24), Png(ANDROID, ANDROID, _opaque, TextureFormat.RGB24), true, Color.white, null, null);
            var issues = IconChecks.Check(new[] { icon })[0];
            Assert.That(issues.Select(issue => (issue.Field, issue.Kind)), Is.EqualTo(new[] { (IconField.AndroidBackground, IconIssueKind.MissingImage) }));
        }

        [Test]
        public void Check_SeveralProblems_ReturnsErrorsBeforeWarnings()
        {
            var icon = new IconFiles("Scifi", Png(IOS, IOS, _opaque, TextureFormat.RGBA32), null, false, Color.white, null, Png(ANDROID, ANDROID, _opaque, TextureFormat.RGB24));
            var issues = IconChecks.Check(new[] { icon, new IconFiles("scifi", null, null, false, Color.white, null, null) })[0];
            Assert.That(Kinds(issues), Is.EqualTo(new[] { IconIssueKind.DuplicateName, IconIssueKind.MissingImage, IconIssueKind.IosAlphaChannel, IconIssueKind.NotWhiteMonochrome }));
        }

        [Test]
        public void Concerns_EachField_ReturnsItsPlatforms()
        {
            var fields = new[] { IconField.Name, IconField.IosImage, IconField.AndroidForeground, IconField.AndroidBackground, IconField.AndroidMonochrome };
            var issues = fields.Select(field => new IconIssue(field, IconIssueKind.MissingImage, string.Empty)).ToArray();
            Assert.That(issues.Select(issue => issue.ConcernsIos), Is.EqualTo(new[] { true, true, false, false, false }));
            Assert.That(issues.Select(issue => issue.ConcernsAndroid), Is.EqualTo(new[] { true, false, true, true, true }));
        }

        [Test]
        public void CountTransparentPixels_MixedPixels_CountsPixelsNotFullyOpaque()
        {
            var pixels = new[] { _opaque, _clear, new Color32(0, 0, 0, 254), _white };
            Assert.That(IconChecks.CountTransparentPixels(pixels), Is.EqualTo(2));
        }

        [Test]
        public void CountNonWhitePixels_MixedPixels_CountsVisibleNonWhiteOnes()
        {
            var pixels = new[] { _white, _clear, new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 1), new Color32(255, 255, 0, 255) };
            Assert.That(IconChecks.CountNonWhitePixels(pixels), Is.EqualTo(2));
        }

        private static IconIssueKind[] Kinds(IEnumerable<IconIssue> issues)
        {
            return issues.Select(issue => issue.Kind).ToArray();
        }

        /// <summary>
        ///     PNG file filled with one colour, with the first pixel of another colour if given. An RGB24 format writes a
        ///     PNG without alpha channel, RGBA32 one with it.
        /// </summary>
        internal static byte[] Png(int width, int height, Color32 fill, TextureFormat format, Color32? firstPixel = null)
        {
            var texture = Texture(width, height, fill, format, firstPixel);
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }

        private static Texture2D Texture(int width, int height, Color32 fill, TextureFormat format, Color32? firstPixel = null)
        {
            var pixels = Enumerable.Repeat(fill, width * height).ToArray();
            if (firstPixel.HasValue) pixels[0] = firstPixel.Value;

            var texture = new Texture2D(width, height, format, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}