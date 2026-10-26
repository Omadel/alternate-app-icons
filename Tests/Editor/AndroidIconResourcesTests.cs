using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Javienne.AlternateAppIcons.Tests
{
    public class AndroidIconResourcesTests
    {
        private string _resources;

        [SetUp]
        public void SetUp()
        {
            _resources = Path.Combine(Path.GetTempPath(), "AlternateAppIconsTests", Guid.NewGuid().ToString("N"), "res");
            Directory.CreateDirectory(_resources);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(Path.GetDirectoryName(_resources), true);
        }

        [Test]
        public void ResourceName_ReturnsLowerCaseName()
        {
            Assert.That(AndroidIconResources.ResourceName("Hellhound_2"), Is.EqualTo("alternate_icon_hellhound_2"));
        }

        [Test]
        public void Colour_ReturnsOpaqueHexColour()
        {
            Assert.That(AndroidIconResources.Colour(new Color32(255, 128, 0, 100)), Is.EqualTo("#FF8000"));
        }

        [Test]
        public void Write_ColourBackground_WritesForegroundAdaptiveIconAndColour()
        {
            AndroidIconResources.Write(_resources, new[] { TestIcons.Valid("Hellhound") });
            Assert.That(File.ReadAllBytes(Layer("alternate_icon_hellhound_foreground")), Is.EqualTo(TestIcons.Foreground));
            Assert.That(File.Exists(Layer("alternate_icon_hellhound_background")), Is.False);

            var adaptiveIcon = File.ReadAllText(AdaptiveIcon("alternate_icon_hellhound"));
            Assert.That(adaptiveIcon, Does.Contain("<background android:drawable=\"@color/alternate_icon_hellhound_background\" />"));
            Assert.That(adaptiveIcon, Does.Contain("<foreground android:drawable=\"@mipmap/alternate_icon_hellhound_foreground\" />"));
            Assert.That(adaptiveIcon, Does.Not.Contain("monochrome"));

            var values = File.ReadAllText(Values());
            Assert.That(values, Does.Contain("<color name=\"alternate_icon_hellhound_background\">#FF8000</color>"));
            Assert.That(values, Does.Contain("<item name=\"alternate_icon_hellhound\" type=\"mipmap\">@mipmap/alternate_icon_hellhound_foreground</item>"));
        }

        [Test]
        public void Write_ImageBackgroundAndMonochrome_WritesTheirLayers()
        {
            var icon = new IconFiles("Scifi", TestIcons.IosImage, TestIcons.Foreground, true, Color.white, TestIcons.Background, TestIcons.Monochrome);
            AndroidIconResources.Write(_resources, new[] { icon });
            Assert.That(File.ReadAllBytes(Layer("alternate_icon_scifi_background")), Is.EqualTo(TestIcons.Background));
            Assert.That(File.ReadAllBytes(Layer("alternate_icon_scifi_monochrome")), Is.EqualTo(TestIcons.Monochrome));

            var adaptiveIcon = File.ReadAllText(AdaptiveIcon("alternate_icon_scifi"));
            Assert.That(adaptiveIcon, Does.Contain("<background android:drawable=\"@mipmap/alternate_icon_scifi_background\" />"));
            Assert.That(adaptiveIcon, Does.Contain("<monochrome android:drawable=\"@mipmap/alternate_icon_scifi_monochrome\" />"));
            Assert.That(File.ReadAllText(Values()), Does.Not.Contain("<color"));
        }

        [Test]
        public void Write_AfterPreviousBuild_ReplacesItsResourcesOnly()
        {
            var otherLayer = Path.Combine(_resources, AndroidIconResources.LAYERS_FOLDER, "app_icon.png");
            Directory.CreateDirectory(Path.GetDirectoryName(otherLayer));
            File.WriteAllText(otherLayer, "other");
            AndroidIconResources.Write(_resources, new[] { TestIcons.Valid("Hellhound") });
            AndroidIconResources.Write(_resources, new[] { TestIcons.Valid("Scifi") });
            Assert.That(File.Exists(Layer("alternate_icon_hellhound_foreground")), Is.False);
            Assert.That(File.Exists(AdaptiveIcon("alternate_icon_hellhound")), Is.False);
            Assert.That(File.Exists(Layer("alternate_icon_scifi_foreground")), Is.True);
            Assert.That(File.ReadAllText(Values()), Does.Not.Contain("hellhound"));
            Assert.That(File.Exists(otherLayer), Is.True);
        }

        [Test]
        public void Write_NoIconAfterPreviousBuild_RemovesItsResources()
        {
            AndroidIconResources.Write(_resources, new[] { TestIcons.Valid("Hellhound") });
            AndroidIconResources.Write(_resources, new IconFiles[0]);
            Assert.That(File.Exists(Layer("alternate_icon_hellhound_foreground")), Is.False);
            Assert.That(File.Exists(AdaptiveIcon("alternate_icon_hellhound")), Is.False);
            Assert.That(File.Exists(Values()), Is.False);
        }

        private string Layer(string name)
        {
            return Path.Combine(_resources, AndroidIconResources.LAYERS_FOLDER, name + ".png");
        }

        private string AdaptiveIcon(string name)
        {
            return Path.Combine(_resources, AndroidIconResources.ADAPTIVE_ICONS_FOLDER, name + ".xml");
        }

        private string Values()
        {
            return Path.Combine(_resources, "values", AndroidIconResources.VALUES_FILE);
        }
    }
}