using NUnit.Framework;

namespace Javienne.AlternateAppIcons.Tests
{
    public class IconNamesTests
    {
        [TestCase("Hellhound")]
        [TestCase("scifi")]
        [TestCase("Golden_Retriever_02")]
        [TestCase("A")]
        public void IsValid_LetterThenLettersDigitsOrUnderscores_ReturnsTrue(string name)
        {
            Assert.That(IconNames.IsValid(name), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("2Dogs")]
        [TestCase("_Hidden")]
        [TestCase("Golden Retriever")]
        [TestCase("Golden-Retriever")]
        [TestCase("Golden.Retriever")]
        [TestCase("Chiôt")]
        public void IsValid_EmptyOrOtherCharacters_ReturnsFalse(string name)
        {
            Assert.That(IconNames.IsValid(name), Is.False);
        }

        [Test]
        public void AreSame_DifferentCase_ReturnsTrue()
        {
            Assert.That(IconNames.AreSame("Scifi", "SCIFI"), Is.True);
        }

        [Test]
        public void AreSame_DifferentNames_ReturnsFalse()
        {
            Assert.That(IconNames.AreSame("Scifi", "Hellhound"), Is.False);
        }
    }
}