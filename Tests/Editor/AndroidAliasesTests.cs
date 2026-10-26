using NUnit.Framework;

namespace Javienne.AlternateAppIcons.Tests
{
    public class AndroidAliasesTests
    {
        [Test]
        public void AliasName_DefaultIcon_ReturnsDefaultAlias()
        {
            Assert.That(AndroidAliases.AliasName(string.Empty), Is.EqualTo("com.javienne.alternateappicons.DefaultIcon"));
        }

        [Test]
        public void AliasName_Icon_ReturnsIconAliasKeepingCase()
        {
            Assert.That(AndroidAliases.AliasName("Hellhound"), Is.EqualTo("com.javienne.alternateappicons.icon.Hellhound"));
        }

        [TestCase("")]
        [TestCase("Hellhound")]
        public void TryGetIcon_AliasOfIcon_ReturnsIcon(string icon)
        {
            Assert.That(AndroidAliases.TryGetIcon(AndroidAliases.AliasName(icon), out var result), Is.True);
            Assert.That(result, Is.EqualTo(icon));
        }

        [TestCase(null)]
        [TestCase("com.unity3d.player.UnityPlayerGameActivity")]
        [TestCase("com.javienne.alternateappicons.icon.")]
        [TestCase("com.javienne.alternateappicons.Other")]
        public void TryGetIcon_OtherActivity_ReturnsFalse(string activity)
        {
            Assert.That(AndroidAliases.TryGetIcon(activity, out _), Is.False);
        }

        [TestCase(AndroidAliases.ENABLED_STATE_DEFAULT, true, ExpectedResult = true)]
        [TestCase(AndroidAliases.ENABLED_STATE_DEFAULT, false, ExpectedResult = false)]
        [TestCase(AndroidAliases.ENABLED_STATE_ENABLED, false, ExpectedResult = true)]
        [TestCase(AndroidAliases.ENABLED_STATE_DISABLED, true, ExpectedResult = false)]
        [TestCase(3, true, ExpectedResult = false)]
        [TestCase(4, true, ExpectedResult = false)]
        public bool IsEnabled_ReturnsStateOrManifestValue(int enabledState, bool isEnabledInManifest)
        {
            return AndroidAliases.IsEnabled(enabledState, isEnabledInManifest);
        }

        [Test]
        public void CurrentIcon_DefaultAliasEnabled_ReturnsDefaultIcon()
        {
            Assert.That(AndroidAliases.CurrentIcon(new[] { ("", true), ("Hellhound", false), ("Scifi", false) }), Is.Empty);
        }

        [Test]
        public void CurrentIcon_IconAliasEnabled_ReturnsIcon()
        {
            Assert.That(AndroidAliases.CurrentIcon(new[] { ("", false), ("Hellhound", false), ("Scifi", true) }), Is.EqualTo("Scifi"));
        }

        [Test]
        public void CurrentIcon_ChangeStoppedHalfWay_ReturnsAlternateIcon()
        {
            Assert.That(AndroidAliases.CurrentIcon(new[] { ("", true), ("Hellhound", true), ("Scifi", false) }), Is.EqualTo("Hellhound"));
        }

        [Test]
        public void CurrentIcon_NoAliasEnabled_ReturnsDefaultIcon()
        {
            Assert.That(AndroidAliases.CurrentIcon(new[] { ("", false), ("Hellhound", false) }), Is.Empty);
        }
    }
}