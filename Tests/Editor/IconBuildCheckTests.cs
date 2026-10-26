using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Build;
using UnityEngine;

namespace Javienne.AlternateAppIcons.Tests
{
    public class IconBuildCheckTests
    {
        private List<string> _warnings;

        [SetUp]
        public void SetUp()
        {
            _warnings = new List<string>();
        }

        [Test]
        public void Run_ValidIcons_ReturnsTrueWithoutWarning()
        {
            Assert.That(RunAndroid(false, TestIcons.Valid("Hellhound"), TestIcons.Valid("Scifi")), Is.True);
            Assert.That(_warnings, Is.Empty);
        }

        [Test]
        public void Run_ErrorOnPlatform_ReturnsFalseAndLogsOneWarning()
        {
            var icon = new IconFiles("Scifi", TestIcons.IosImage, null, false, Color.white, null, null);
            Assert.That(RunAndroid(false, TestIcons.Valid("Hellhound"), icon), Is.False);
            Assert.That(_warnings.Count, Is.EqualTo(1));
            Assert.That(_warnings[0], Does.StartWith("Alternate App Icons: the Android build has no alternate icon"));
            Assert.That(_warnings[0], Does.Contain("\n- Scifi: No Android foreground is set."));
        }

        [Test]
        public void Run_ErrorOnPlatformWithFailBuildOnError_Throws()
        {
            var icon = new IconFiles("Scifi", TestIcons.IosImage, null, false, Color.white, null, null);
            var exception = Assert.Throws<BuildFailedException>(() => RunAndroid(true, icon));
            Assert.That(exception.Message, Does.Contain("fail the Android build"));
            Assert.That(_warnings, Is.Empty);
        }

        [Test]
        public void Run_ErrorOnOtherPlatform_IgnoresIt()
        {
            var icon = new IconFiles("Scifi", null, TestIcons.Foreground, false, Color.white, null, null);
            Assert.That(RunAndroid(true, icon), Is.True);
            Assert.That(_warnings, Is.Empty);
        }

        [Test]
        public void Run_WarningOnly_ReturnsTrueAndLogsIt()
        {
            var transparentBackground = IconChecksTests.Png(IconChecks.ANDROID_LAYER_SIZE, IconChecks.ANDROID_LAYER_SIZE, new Color32(0, 0, 0, 0), TextureFormat.RGBA32);
            var icon = new IconFiles("Scifi", TestIcons.IosImage, TestIcons.Foreground, true, Color.white, transparentBackground, null);
            Assert.That(RunAndroid(true, icon), Is.True);
            Assert.That(_warnings.Count, Is.EqualTo(1));
            Assert.That(_warnings[0], Does.StartWith("Alternate App Icons: some alternate icons have warnings, which do not stop the Android build."));
        }

        [Test]
        public void Run_IconWithoutName_NamesItByPosition()
        {
            RunAndroid(false, TestIcons.Valid("Hellhound"), TestIcons.Valid(""));
            Assert.That(_warnings[0], Does.Contain("\n- Icon 2: The name is empty"));
        }

        private bool RunAndroid(bool failBuildOnError, params IconFiles[] icons)
        {
            return IconBuildCheck.Run(icons, "Android", issue => issue.ConcernsAndroid, failBuildOnError, _warnings.Add);
        }
    }
}