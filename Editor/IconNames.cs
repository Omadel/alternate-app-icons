using System;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Rules for the names of the alternate icons. A name is the identifier of the icon in the API, and also names its
    ///     icon set in the Xcode project and its alias and resources in the Gradle project.
    /// </summary>
    internal static class IconNames
    {
        /// <summary>
        ///     Whether a name can identify an icon: an ASCII letter, then ASCII letters, digits or underscores. A space
        ///     would split the Xcode setting that lists the icons, and Android aliases and resources take no other
        ///     characters.
        /// </summary>
        internal static bool IsValid(string name)
        {
            if (string.IsNullOrEmpty(name) || !IsAsciiLetter(name[0])) return false;

            foreach (var character in name)
            {
                if (!IsAsciiLetter(character) && !(character >= '0' && character <= '9') && character != '_') return false;
            }

            return true;
        }

        /// <summary>
        ///     Whether two names identify the same icon. Android resource names are lower case, so names that differ only
        ///     by case would share their resources.
        /// </summary>
        internal static bool AreSame(string first, string second)
        {
            return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAsciiLetter(char character)
        {
            return (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z');
        }
    }
}