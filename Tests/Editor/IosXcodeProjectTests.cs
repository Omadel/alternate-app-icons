using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Javienne.AlternateAppIcons.Tests
{
    public class IosXcodeProjectTests
    {
        private string _xcodeProject;
        private string _catalog;
        private string _appIconSet;
        private List<string> _warnings;

        [SetUp]
        public void SetUp()
        {
            _xcodeProject = Path.Combine(Path.GetTempPath(), "AlternateAppIconsTests", Guid.NewGuid().ToString("N"), "Xcode");
            _catalog = Path.Combine(_xcodeProject, "Unity-iPhone", "Images.xcassets");
            _appIconSet = Path.Combine(_catalog, "AppIcon.appiconset");
            Directory.CreateDirectory(_appIconSet);
            File.WriteAllText(Path.Combine(_appIconSet, "Contents.json"), "{}");
            _warnings = new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(Path.GetDirectoryName(_xcodeProject), true);
        }

        [Test]
        public void AddIcons_ValidIcons_WritesSingleSizeIconSets()
        {
            var iconSets = AddIcons(TestIcons.Valid("Hellhound"), TestIcons.Valid("Scifi"));
            Assert.That(iconSets, Is.EqualTo(new[] { "AlternateAppIcon-Hellhound", "AlternateAppIcon-Scifi" }));
            Assert.That(File.ReadAllBytes(Path.Combine(IconSet("Hellhound"), IosXcodeProject.IMAGE_FILE)), Is.EqualTo(TestIcons.IosImage));

            var contents = File.ReadAllText(Path.Combine(IconSet("Hellhound"), "Contents.json"));
            Assert.That(contents, Does.Contain("\"filename\" : \"Icon.png\""));
            Assert.That(contents, Does.Contain("\"idiom\" : \"universal\""));
            Assert.That(contents, Does.Contain("\"platform\" : \"ios\""));
            Assert.That(contents, Does.Contain("\"size\" : \"1024x1024\""));
            Assert.That(_warnings, Is.Empty);
        }

        [Test]
        public void AddIcons_AfterPreviousBuild_ReplacesItsIconSetsOnly()
        {
            AddIcons(TestIcons.Valid("Hellhound"));
            AddIcons(TestIcons.Valid("Scifi"));
            Assert.That(Directory.Exists(IconSet("Hellhound")), Is.False);
            Assert.That(Directory.Exists(IconSet("Scifi")), Is.True);
            Assert.That(Directory.Exists(_appIconSet), Is.True);
        }

        [Test]
        public void AddIcons_ErrorAfterPreviousBuild_LeavesEveryIconOut()
        {
            AddIcons(TestIcons.Valid("Hellhound"));
            var broken = new IconFiles("Scifi", null, TestIcons.Foreground, false, Color.white, null, null);
            Assert.That(AddIcons(TestIcons.Valid("Hellhound"), broken), Is.Empty);
            Assert.That(Directory.Exists(IconSet("Hellhound")), Is.False);
            Assert.That(_warnings.Count, Is.EqualTo(1));
            Assert.That(_warnings[0], Does.Contain("the iOS build has no alternate icon"));
        }

        [Test]
        public void AddIcons_ErrorOnAndroidOnly_KeepsIcons()
        {
            var icon = new IconFiles("Scifi", TestIcons.IosImage, null, false, Color.white, null, null);
            Assert.That(AddIcons(icon), Is.EqualTo(new[] { "AlternateAppIcon-Scifi" }));
            Assert.That(_warnings, Is.Empty);
        }

        [Test]
        public void AddIcons_NoAssetCatalog_LogsWarning()
        {
            Directory.Delete(_catalog, true);
            Assert.That(AddIcons(TestIcons.Valid("Hellhound")), Is.Empty);
            Assert.That(_warnings.Count, Is.EqualTo(1));
            Assert.That(_warnings[0], Does.Contain("has no asset catalog"));
        }

        private IReadOnlyList<string> AddIcons(params IconFiles[] icons)
        {
            return IosXcodeProject.AddIcons(_xcodeProject, icons, false, _warnings.Add);
        }

        private string IconSet(string icon)
        {
            return Path.Combine(_catalog, $"AlternateAppIcon-{icon}.appiconset");
        }
    }
}