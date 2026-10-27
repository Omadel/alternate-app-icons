using System.Linq;
using System.Xml;
using NUnit.Framework;

namespace Javienne.AlternateAppIcons.Tests
{
    public class AndroidManifestAliasesTests
    {
        private const string ANDROID_NAMESPACE = "http://schemas.android.com/apk/res/android";
        private const string TOOLS_NAMESPACE = "http://schemas.android.com/tools";
        private const string ACTIVITY = "com.unity3d.player.UnityPlayerGameActivity";
        private const string MAIN_ACTION = "android.intent.action.MAIN";
        private const string LAUNCHER_CATEGORY = "android.intent.category.LAUNCHER";

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
    <provider android:name=""com.example.game.SplashScreenExit"" android:authorities=""${applicationId}.splash-screen-exit"" android:exported=""false"" />
  </application>
</manifest>";

        /// <summary>
        ///     Manifest of the launcher module as Unity 6000.3 generates it, shortened.
        /// </summary>
        private const string LAUNCHER_MANIFEST = @"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest android:installLocation=""preferExternal"" xmlns:android=""http://schemas.android.com/apk/res/android"" xmlns:tools=""http://schemas.android.com/tools"">
  <application android:icon=""@mipmap/app_icon"" android:label=""@string/app_name"" android:roundIcon=""@mipmap/app_icon_round"" />
  <supports-screens android:smallScreens=""true"" android:anyDensity=""true"" />
</manifest>";

        /// <summary>
        ///     Build steps of other packages look for the main activity with this path, and may run after this one.
        /// </summary>
        private const string LAUNCHER_ACTIVITY_PATH = "/manifest/application/activity[intent-filter/category[@android:name='android.intent.category.LAUNCHER']]";

        private static readonly string[] _icons = { "Hellhound", "Scifi" };

        [Test]
        public void AddAliases_NoIcon_ReturnsManifestsUnchanged()
        {
            var (library, launcher) = AndroidManifestAliases.AddAliases(MANIFEST, LAUNCHER_MANIFEST, new string[0]).Value;
            Assert.That(library, Is.EqualTo(MANIFEST));
            Assert.That(launcher, Is.EqualTo(LAUNCHER_MANIFEST));
        }

        [Test]
        public void AddAliases_Icons_KeepsLauncherFilterOfActivity()
        {
            var manifest = Load(AndroidManifestAliases.AddAliases(MANIFEST, LAUNCHER_MANIFEST, _icons).Value.Library);
            var activity = Element(manifest, "activity", ACTIVITY);
            Assert.That(Categories(activity), Is.EqualTo(new[] { LAUNCHER_CATEGORY }));
            Assert.That(Actions(activity), Is.EqualTo(new[] { MAIN_ACTION }));
        }

        [Test]
        public void AddAliases_Icons_LeavesActivityToBuildStepsLookingForLauncher()
        {
            var manifest = Load(AndroidManifestAliases.AddAliases(MANIFEST, LAUNCHER_MANIFEST, _icons).Value.Library);
            var namespaces = new XmlNamespaceManager(manifest.NameTable);
            namespaces.AddNamespace("android", ANDROID_NAMESPACE);
            var activity = manifest.SelectSingleNode(LAUNCHER_ACTIVITY_PATH, namespaces) as XmlElement;
            Assert.That(activity, Is.Not.Null);
            Assert.That(Attribute(activity, "name"), Is.EqualTo(ACTIVITY));
        }

        [Test]
        public void AddAliases_Icons_DeclaresEnabledDefaultAlias()
        {
            var manifest = Load(AndroidManifestAliases.AddAliases(MANIFEST, LAUNCHER_MANIFEST, _icons).Value.Library);
            var alias = Element(manifest, "activity-alias", AndroidAliases.DEFAULT_ALIAS);
            Assert.That(Attribute(alias, "enabled"), Is.EqualTo("true"));
            Assert.That(Attribute(alias, "exported"), Is.EqualTo("true"));
            Assert.That(Attribute(alias, "targetActivity"), Is.EqualTo(ACTIVITY));
            Assert.That(alias.HasAttribute("icon", ANDROID_NAMESPACE), Is.False);
            Assert.That(Actions(alias), Is.EqualTo(new[] { MAIN_ACTION }));
            Assert.That(Categories(alias), Is.EqualTo(new[] { LAUNCHER_CATEGORY }));
        }

        [Test]
        public void AddAliases_Icons_DeclaresDisabledAliasWithAdaptiveIconForEachIcon()
        {
            var manifest = Load(AndroidManifestAliases.AddAliases(MANIFEST, LAUNCHER_MANIFEST, _icons).Value.Library);
            foreach (var icon in _icons)
            {
                var alias = Element(manifest, "activity-alias", AndroidAliases.AliasName(icon));
                Assert.That(Attribute(alias, "enabled"), Is.EqualTo("false"));
                Assert.That(Attribute(alias, "exported"), Is.EqualTo("true"));
                Assert.That(Attribute(alias, "icon"), Is.EqualTo($"@mipmap/alternate_icon_{icon.ToLowerInvariant()}"));
                Assert.That(Attribute(alias, "roundIcon"), Is.EqualTo($"@mipmap/alternate_icon_{icon.ToLowerInvariant()}"));
                Assert.That(Attribute(alias, "targetActivity"), Is.EqualTo(ACTIVITY));
                Assert.That(Categories(alias), Is.EqualTo(new[] { LAUNCHER_CATEGORY }));
            }
        }

        [Test]
        public void AddAliases_Icons_PutsAliasesRightAfterTheirActivity()
        {
            var manifest = Load(AndroidManifestAliases.AddAliases(MANIFEST, LAUNCHER_MANIFEST, _icons).Value.Library);
            var names = manifest.DocumentElement["application"].ChildNodes.OfType<XmlElement>().Select(element => element.GetAttribute("name", ANDROID_NAMESPACE));
            Assert.That(names, Is.EqualTo(new[] { "unity.splash-mode", ACTIVITY, AndroidAliases.DEFAULT_ALIAS, AndroidAliases.AliasName("Hellhound"), AndroidAliases.AliasName("Scifi"), "com.example.game.SplashScreenExit" }));
        }

        [Test]
        public void AddAliases_Icons_RemovesLauncherFilterInLauncherManifest()
        {
            var activity = Element(Load(AndroidManifestAliases.AddAliases(MANIFEST, LAUNCHER_MANIFEST, _icons).Value.Launcher), "activity", ACTIVITY);
            var filters = Filters(activity);
            Assert.That(activity.Attributes.Count, Is.EqualTo(1));
            Assert.That(filters.Length, Is.EqualTo(2));
            Assert.That(filters[0].GetAttribute("node", TOOLS_NAMESPACE), Is.EqualTo("remove"));
            Assert.That(Names(filters[0], null), Is.EqualTo(new[] { LAUNCHER_CATEGORY, MAIN_ACTION }));
            Assert.That(filters[1].HasAttribute("node", TOOLS_NAMESPACE), Is.False);
            Assert.That(Names(filters[1], null), Is.EqualTo(new[] { MAIN_ACTION }));
        }

        [Test]
        public void AddAliases_LauncherFilterWithOtherElements_CopiesThemAll()
        {
            var manifest = MANIFEST.Replace("<action android:name=\"android.intent.action.MAIN\" />", "<action android:name=\"android.intent.action.MAIN\" />\n        <category android:name=\"android.intent.category.LEANBACK_LAUNCHER\" />");
            var filters = Filters(Element(Load(AndroidManifestAliases.AddAliases(manifest, LAUNCHER_MANIFEST, _icons).Value.Launcher), "activity", ACTIVITY));
            Assert.That(Names(filters[0], null), Is.EqualTo(new[] { LAUNCHER_CATEGORY, MAIN_ACTION, "android.intent.category.LEANBACK_LAUNCHER" }));
            Assert.That(Names(filters[1], null), Is.EqualTo(new[] { MAIN_ACTION, "android.intent.category.LEANBACK_LAUNCHER" }));
        }

        [Test]
        public void AddAliases_LauncherManifestDeclaringActivity_AddsFiltersToIt()
        {
            var launcher = LAUNCHER_MANIFEST.Replace("android:roundIcon=\"@mipmap/app_icon_round\" />", "android:roundIcon=\"@mipmap/app_icon_round\">\n    <activity android:name=\"com.unity3d.player.UnityPlayerGameActivity\" android:resizeableActivity=\"false\" />\n  </application>");
            var manifest = Load(AndroidManifestAliases.AddAliases(MANIFEST, launcher, _icons).Value.Launcher);
            var activity = Element(manifest, "activity", ACTIVITY);
            Assert.That(Attribute(activity, "resizeableActivity"), Is.EqualTo("false"));
            Assert.That(Filters(activity).Length, Is.EqualTo(2));
        }

        [Test]
        public void AddAliases_LauncherManifestWithoutToolsNamespace_DeclaresIt()
        {
            var launcher = LAUNCHER_MANIFEST.Replace(" xmlns:tools=\"http://schemas.android.com/tools\"", string.Empty);
            var manifest = AndroidManifestAliases.AddAliases(MANIFEST, launcher, _icons).Value.Launcher;
            Assert.That(manifest.Split("xmlns:tools=").Length, Is.EqualTo(2));
            Assert.That(Filters(Element(Load(manifest), "activity", ACTIVITY))[0].GetAttribute("node", TOOLS_NAMESPACE), Is.EqualTo("remove"));
        }

        [Test]
        public void AddAliases_Icons_KeepsDeclarationPrefixesAndLines()
        {
            var (library, launcher) = AndroidManifestAliases.AddAliases(MANIFEST, LAUNCHER_MANIFEST, _icons).Value;
            Assert.That(library, Does.StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<manifest xmlns:android="));
            Assert.That(library.Split("xmlns:android=").Length, Is.EqualTo(2));
            Assert.That(library, Does.Contain("\n    <activity-alias android:name=\"com.javienne.alternateappicons.icon.Scifi\""));
            Assert.That(library, Does.Contain("android:authorities=\"${applicationId}.splash-screen-exit\""));
            Assert.That(launcher, Does.StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<manifest android:installLocation="));
            Assert.That(launcher, Does.Contain("android:roundIcon=\"@mipmap/app_icon_round\">\n    <activity android:name=\"com.unity3d.player.UnityPlayerGameActivity\">\n      <intent-filter tools:node=\"remove\">\n        <category android:name=\"android.intent.category.LAUNCHER\" />\n        <action android:name=\"android.intent.action.MAIN\" />\n      </intent-filter>\n      <intent-filter>\n        <action android:name=\"android.intent.action.MAIN\" />\n      </intent-filter>\n    </activity>\n  </application>\n  <supports-screens"));
        }

        [Test]
        public void AddAliases_AgainWithOtherIcons_ReplacesPreviousAliasesAndFilterRemoval()
        {
            var (library, launcher) = AndroidManifestAliases.AddAliases(MANIFEST, LAUNCHER_MANIFEST, _icons).Value;
            var twice = AndroidManifestAliases.AddAliases(library, launcher, new[] { "Chocolate" });
            Assert.That(twice, Is.EqualTo(AndroidManifestAliases.AddAliases(MANIFEST, LAUNCHER_MANIFEST, new[] { "Chocolate" })));
        }

        [Test]
        public void AddAliases_NoIconAfterIcons_RestoresManifests()
        {
            var (library, launcher) = AndroidManifestAliases.AddAliases(MANIFEST, LAUNCHER_MANIFEST, _icons).Value;
            var restored = AndroidManifestAliases.AddAliases(library, launcher, new string[0]).Value;
            Assert.That(Load(restored.Library).OuterXml, Is.EqualTo(Load(MANIFEST).OuterXml));
            Assert.That(restored.Launcher, Is.EqualTo(LAUNCHER_MANIFEST));
        }

        [Test]
        public void AddAliases_NoIconAfterIconsInLauncherManifestDeclaringActivity_RestoresActivity()
        {
            var launcher = LAUNCHER_MANIFEST.Replace("android:roundIcon=\"@mipmap/app_icon_round\" />", "android:roundIcon=\"@mipmap/app_icon_round\">\n    <activity android:name=\"com.unity3d.player.UnityPlayerGameActivity\" android:resizeableActivity=\"false\" />\n  </application>");
            var withIcons = AndroidManifestAliases.AddAliases(MANIFEST, launcher, _icons).Value.Launcher;
            Assert.That(AndroidManifestAliases.AddAliases(MANIFEST, withIcons, new string[0]).Value.Launcher, Is.EqualTo(launcher));
        }

        [Test]
        public void AddAliases_NoLauncherActivity_ReturnsNull()
        {
            var manifest = MANIFEST.Replace("android.intent.category.LAUNCHER", "android.intent.category.DEFAULT");
            Assert.That(AndroidManifestAliases.AddAliases(manifest, LAUNCHER_MANIFEST, _icons), Is.Null);
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

        private static XmlElement[] Filters(XmlElement component)
        {
            return component.ChildNodes.OfType<XmlElement>().Where(element => element.Name == "intent-filter").ToArray();
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

        /// <summary>
        ///     Names of the elements of a component, of one tag or of all tags, at any depth.
        /// </summary>
        private static string[] Names(XmlElement component, string tag)
        {
            return component.GetElementsByTagName(tag ?? "*").OfType<XmlElement>().Select(element => element.GetAttribute("name", ANDROID_NAMESPACE)).ToArray();
        }
    }
}