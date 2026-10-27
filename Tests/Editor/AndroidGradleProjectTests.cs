using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Javienne.AlternateAppIcons.Tests
{
    public class AndroidGradleProjectTests
    {
        private const string MANIFEST = @"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"">
  <application>
    <activity android:exported=""true"" android:name=""com.unity3d.player.UnityPlayerGameActivity"">
      <intent-filter>
        <category android:name=""android.intent.category.LAUNCHER"" />
        <action android:name=""android.intent.action.MAIN"" />
      </intent-filter>
    </activity>
  </application>
</manifest>";

        private const string LAUNCHER_MANIFEST = @"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"" xmlns:tools=""http://schemas.android.com/tools"">
  <application android:icon=""@mipmap/app_icon"" />
</manifest>";

        private string _unityLibrary;
        private string _manifestPath;
        private string _launcherManifestPath;
        private string _foregroundPath;
        private List<string> _warnings;

        [SetUp]
        public void SetUp()
        {
            var project = Path.Combine(Path.GetTempPath(), "AlternateAppIconsTests", Guid.NewGuid().ToString("N"));
            _unityLibrary = Path.Combine(project, "unityLibrary");
            var main = Path.Combine(_unityLibrary, "src", "main");
            Directory.CreateDirectory(main);
            _manifestPath = Path.Combine(main, "AndroidManifest.xml");
            File.WriteAllText(_manifestPath, MANIFEST);
            var launcherMain = Path.Combine(project, "launcher", "src", "main");
            Directory.CreateDirectory(launcherMain);
            _launcherManifestPath = Path.Combine(launcherMain, "AndroidManifest.xml");
            File.WriteAllText(_launcherManifestPath, LAUNCHER_MANIFEST);
            _foregroundPath = Path.Combine(main, "res", AndroidIconResources.LAYERS_FOLDER, "alternate_icon_hellhound_foreground.png");
            _warnings = new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(Path.GetDirectoryName(_unityLibrary), true);
        }

        [Test]
        public void AddIcons_ValidIcons_WritesAliasesFilterRemovalAndResources()
        {
            AndroidGradleProject.AddIcons(_unityLibrary, new[] { TestIcons.Valid("Hellhound") }, false, _warnings.Add);
            Assert.That(File.ReadAllText(_launcherManifestPath), Does.Contain(AndroidAliases.AliasName("Hellhound")));
            Assert.That(File.ReadAllText(_launcherManifestPath), Does.Contain("<intent-filter tools:node=\"remove\">"));
            Assert.That(File.Exists(_foregroundPath), Is.True);
            Assert.That(_warnings, Is.Empty);
        }

        [Test]
        public void AddIcons_ValidIcons_LeavesLibraryManifestUntouched()
        {
            var written = File.GetLastWriteTimeUtc(_manifestPath);
            AndroidGradleProject.AddIcons(_unityLibrary, new[] { TestIcons.Valid("Hellhound") }, false, _warnings.Add);
            Assert.That(File.GetLastWriteTimeUtc(_manifestPath), Is.EqualTo(written));
            Assert.That(File.ReadAllText(_manifestPath), Is.EqualTo(MANIFEST));
        }

        [Test]
        public void AddIcons_ErrorAfterPreviousBuild_LeavesEveryIconOut()
        {
            AndroidGradleProject.AddIcons(_unityLibrary, new[] { TestIcons.Valid("Hellhound") }, false, _warnings.Add);
            var broken = new IconFiles("Scifi", TestIcons.IosImage, null, false, Color.white, null, null);
            AndroidGradleProject.AddIcons(_unityLibrary, new[] { TestIcons.Valid("Hellhound"), broken }, false, _warnings.Add);
            Assert.That(File.ReadAllText(_manifestPath), Is.EqualTo(MANIFEST));
            Assert.That(File.ReadAllText(_launcherManifestPath), Is.EqualTo(LAUNCHER_MANIFEST));
            Assert.That(File.Exists(_foregroundPath), Is.False);
            Assert.That(_warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void AddIcons_NoIcon_LeavesManifestsUntouched()
        {
            var written = File.GetLastWriteTimeUtc(_manifestPath);
            var launcherWritten = File.GetLastWriteTimeUtc(_launcherManifestPath);
            AndroidGradleProject.AddIcons(_unityLibrary, new IconFiles[0], false, _warnings.Add);
            Assert.That(File.GetLastWriteTimeUtc(_manifestPath), Is.EqualTo(written));
            Assert.That(File.GetLastWriteTimeUtc(_launcherManifestPath), Is.EqualTo(launcherWritten));
            Assert.That(_warnings, Is.Empty);
        }

        [Test]
        public void AddIcons_NoLauncherActivity_LogsWarningWithoutResources()
        {
            File.WriteAllText(_manifestPath, MANIFEST.Replace("android.intent.category.LAUNCHER", "android.intent.category.DEFAULT"));
            AndroidGradleProject.AddIcons(_unityLibrary, new[] { TestIcons.Valid("Hellhound") }, false, _warnings.Add);
            Assert.That(File.ReadAllText(_launcherManifestPath), Is.EqualTo(LAUNCHER_MANIFEST));
            Assert.That(File.Exists(_foregroundPath), Is.False);
            Assert.That(_warnings.Count, Is.EqualTo(1));
            Assert.That(_warnings[0], Does.Contain("no activity"));
        }

        [Test]
        public void AddIcons_NoManifest_LogsWarning()
        {
            File.Delete(_manifestPath);
            AndroidGradleProject.AddIcons(_unityLibrary, new[] { TestIcons.Valid("Hellhound") }, false, _warnings.Add);
            Assert.That(File.ReadAllText(_launcherManifestPath), Is.EqualTo(LAUNCHER_MANIFEST));
            Assert.That(_warnings.Count, Is.EqualTo(1));
            Assert.That(_warnings[0], Does.Contain("has no manifest"));
        }

        [Test]
        public void AddIcons_NoLauncherManifest_LogsWarningWithoutResources()
        {
            File.Delete(_launcherManifestPath);
            AndroidGradleProject.AddIcons(_unityLibrary, new[] { TestIcons.Valid("Hellhound") }, false, _warnings.Add);
            Assert.That(File.ReadAllText(_manifestPath), Is.EqualTo(MANIFEST));
            Assert.That(File.Exists(_foregroundPath), Is.False);
            Assert.That(_warnings.Count, Is.EqualTo(1));
            Assert.That(_warnings[0], Does.Contain("has no manifest"));
        }
    }
}