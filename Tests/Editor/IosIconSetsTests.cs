using NUnit.Framework;

namespace Javienne.AlternateAppIcons.Tests
{
    public class IosIconSetsTests
    {
        [Test]
        public void IconSetName_ReturnsPrefixedName()
        {
            Assert.That(IosIconSets.IconSetName("Hellhound"), Is.EqualTo("AlternateAppIcon-Hellhound"));
        }

        [Test]
        public void TryGetIcon_IconSetOfIcon_ReturnsIcon()
        {
            Assert.That(IosIconSets.TryGetIcon("AlternateAppIcon-Hellhound", out var icon), Is.True);
            Assert.That(icon, Is.EqualTo("Hellhound"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("AppIcon")]
        [TestCase("AlternateAppIcon-")]
        public void TryGetIcon_OtherIconSet_ReturnsFalse(string iconSet)
        {
            Assert.That(IosIconSets.TryGetIcon(iconSet, out _), Is.False);
        }

        [Test]
        public void ParseIcons_IconSetsOfInfoPlist_ReturnsTheirIconsOnce()
        {
            var icons = IosIconSets.ParseIcons("AlternateAppIcon-Scifi\nAppIcon\nAlternateAppIcon-Hellhound\nAlternateAppIcon-Scifi");
            Assert.That(icons, Is.EqualTo(new[] { "Scifi", "Hellhound" }));
        }

        [TestCase(null)]
        [TestCase("")]
        public void ParseIcons_NoIconSet_ReturnsNoIcon(string iconSets)
        {
            Assert.That(IosIconSets.ParseIcons(iconSets), Is.Empty);
        }
    }
}