using System.Linq;
using System.Xml;
using NUnit.Framework;

namespace Javienne.AlternateAppIcons.Tests
{
    public class AndroidManifestAliasesTests
    {
        private const string ANDROID_NAMESPACE = "http://schemas.android.com/apk/res/android";
        private const string ACTIVITY = "com.unity3d.player.UnityPlayerGameActivity";

        /// <summary>
        ///     Manifest of the Unity library module as Unity 6000.3 generates it, shortened.
        /// </summary>
        private const string MANIFEST = @"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"" xmlns:tools=""http://schemas.android.com/tools"">
  <uses-permission android:name=""android.permission.INTERNET"" />
  <application android:appCategory=""game"" android:extractNativeLibs=""true"">
    <meta-data android:name=""unity.splash-mode"" android:value=""0"" />
    <activity android:enabled=""true"" android:exported=""true"" android:launchMode=""singleTask"" android:name=""com.unity3d.player.UnityPlayerGameActivity"" android:theme=""@style/BaseUnityGameActivityTheme"">
      <intent-filter>
        <category android:name=""android.intent.category.LAUNCHER"" />
        <action android:name=""android.intent.action.MAIN"" />
      </intent-filter>
      <meta-data android:name=""unityplayer.UnityActivity"" android:value=""true"" />
    </activity>
    <provider android:name=""com.javienne.rouflac.SplashScreenExit"" android:authorities=""${applicationId}.splash-screen-exit"" android:exported=""false"" />
  </application>
</manifest>";

        private static readonly string[] _icons = { "Hellhound", "Scifi" };

        [Test]
        public void AddAliases_NoIcon_ReturnsManifestUnchanged()
        {
            Assert.That(AndroidManifestAliases.AddAliases(MANIFEST, new string[0]), Is.EqualTo(MANIFEST));
        }

        [Test]
        public void AddAliases_Icons_MovesLauncherCategoryToEnabledDefaultAlias()
        {
            var manifest = Load(AndroidManifestAliases.AddAliases(MANIFEST, _icons));
            var activity = Element(manifest, "activity", ACTIVITY);
            Assert.That(Categories(activity), Is.Empty);
            Assert.That(Actions(activity), Is.EqualTo(new[] { "android.intent.action.MAIN" }));

            var alias = Element(manifest, "activity-alias", AndroidAliases.DEFAULT_ALIAS);
            Assert.That(Attribute(alias, "enabled"), Is.EqualTo("true"));
            Assert.That(Attribute(alias, "exported"), Is.EqualTo("true"));
            Assert.That(Attribute(alias, "targetActivity"), Is.EqualTo(ACTIVITY));
            Assert.That(alias.HasAttribute("icon", ANDROID_NAMESPACE), Is.False);
            Assert.That(Actions(alias), Is.EqualTo(new[] { "android.intent.action.MAIN" }));
            Assert.That(Categories(alias), Is.EqualTo(new[] { "android.intent.category.LAUNCHER" }));
        }

        [Test]
        public void AddAliases_Icons_DeclaresDisabledAliasWithAdaptiveIconForEachIcon()
        {
            var manifest = Load(AndroidManifestAliases.AddAliases(MANIFEST, _icons));
            foreach (var icon in _icons)
            {
                var alias = Element(manifest, "activity-alias", AndroidAliases.AliasName(icon));
                Assert.That(Attribute(alias, "enabled"), Is.EqualTo("false"));
                Assert.That(Attribute(alias, "exported"), Is.EqualTo("true"));
                Assert.That(Attribute(alias, "icon"), Is.EqualTo($"@mipmap/alternate_icon_{icon.ToLowerInvariant()}"));
                Assert.That(Attribute(alias, "roundIcon"), Is.EqualTo($"@mipmap/alternate_icon_{icon.ToLowerInvariant()}"));
                Assert.That(Attribute(alias, "targetActivity"), Is.EqualTo(ACTIVITY));
                Assert.That(Categories(alias), Is.EqualTo(new[] { "android.intent.category.LAUNCHER" }));
            }
        }

        [Test]
        public void AddAliases_Icons_PutsAliasesRightAfterTheirActivity()
        {
            var manifest = Load(AndroidManifestAliases.AddAliases(MANIFEST, _icons));
            var names = manifest.DocumentElement["application"].ChildNodes.OfType<XmlElement>().Select(element => element.GetAttribute("name", ANDROID_NAMESPACE));
            Assert.That(names, Is.EqualTo(new[] { "unity.splash-mode", ACTIVITY, AndroidAliases.DEFAULT_ALIAS, AndroidAliases.AliasName("Hellhound"), AndroidAliases.AliasName("Scifi"), "com.javienne.rouflac.SplashScreenExit" }));
        }

        [Test]
        public void AddAliases_Icons_KeepsDeclarationPrefixesAndLines()
        {
            var manifest = AndroidManifestAliases.AddAliases(MANIFEST, _icons);
            Assert.That(manifest, Does.StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<manifest xmlns:android="));
            Assert.That(manifest.Split("xmlns:android=").Length, Is.EqualTo(2));
            Assert.That(manifest, Does.Contain("\n    <activity-alias android:name=\"com.javienne.alternateappicons.icon.Scifi\""));
            Assert.That(manifest, Does.Contain("android:authorities=\"${applicationId}.splash-screen-exit\""));
        }

        [Test]
        public void AddAliases_AgainWithOtherIcons_ReplacesPreviousAliases()
        {
            var twice = AndroidManifestAliases.AddAliases(AndroidManifestAliases.AddAliases(MANIFEST, _icons), new[] { "Chocolate" });
            Assert.That(twice, Is.EqualTo(AndroidManifestAliases.AddAliases(MANIFEST, new[] { "Chocolate" })));
        }

        [Test]
        public void AddAliases_NoIconAfterIcons_RestoresManifest()
        {
            var restored = AndroidManifestAliases.AddAliases(AndroidManifestAliases.AddAliases(MANIFEST, _icons), new string[0]);
            Assert.That(Load(restored).OuterXml, Is.EqualTo(Load(MANIFEST).OuterXml));
        }

        [Test]
        public void AddAliases_NoLauncherActivity_ReturnsNull()
        {
            var manifest = MANIFEST.Replace("android.intent.category.LAUNCHER", "android.intent.category.DEFAULT");
            Assert.That(AndroidManifestAliases.AddAliases(manifest, _icons), Is.Null);
        }

        private static XmlDocument Load(string xml)
        {
            var document = new XmlDocument { PreserveWhitespace = true };
            document.LoadXml(xml);
            return document;
        }

        private static XmlElement Element(XmlDocument manifest, string tag, string name)
        {
            var element = manifest.GetElementsByTagName(tag).OfType<XmlElement>().SingleOrDefault(candidate => candidate.GetAttribute("name", ANDROID_NAMESPACE) == name);
            Assert.That(element, Is.Not.Null, $"No {tag} named {name}.");
            return element;
        }

        private static string Attribute(XmlElement element, string name)
        {
            return element.GetAttribute(name, ANDROID_NAMESPACE);
        }

        private static string[] Actions(XmlElement component)
        {
            return Names(component, "action");
        }

        private static string[] Categories(XmlElement component)
        {
            return Names(component, "category");
        }

        private static string[] Names(XmlElement component, string tag)
        {
            return component.GetElementsByTagName(tag).OfType<XmlElement>().Select(element => element.GetAttribute("name", ANDROID_NAMESPACE)).ToArray();
        }
    }
}