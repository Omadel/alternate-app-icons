using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Resources of the alternate icons in the Android build: the layers of each adaptive icon, copied as they are at
    ///     the xxxhdpi density from which Android scales them down, and the adaptive icon that puts them together. Before
    ///     Android 8.0, which cannot change the icon, the icon is only an alias of its foreground, never shown.
    /// </summary>
    internal static class AndroidIconResources
    {
        internal const string RESOURCE_PREFIX = "alternate_icon_";
        internal const string LAYERS_FOLDER = "mipmap-xxxhdpi";
        internal const string ADAPTIVE_ICONS_FOLDER = "mipmap-anydpi-v26";
        internal const string VALUES_FILE = "alternate_app_icons.xml";

        /// <summary>
        ///     Name of the adaptive icon of an icon, in lower case as Android resource names are: names are unique
        ///     regardless of case.
        /// </summary>
        internal static string ResourceName(string icon)
        {
            return RESOURCE_PREFIX + icon.ToLowerInvariant();
        }

        internal static string IconReference(string icon)
        {
            return "@mipmap/" + ResourceName(icon);
        }

        /// <summary>
        ///     Writes the resources of the icons into a resource folder, replacing those of a previous build.
        /// </summary>
        internal static void Write(string resources, IReadOnlyList<IconFiles> icons)
        {
            Clean(resources);
            if (icons.Count == 0) return;

            var layers = Directory.CreateDirectory(Path.Combine(resources, LAYERS_FOLDER)).FullName;
            var adaptiveIcons = Directory.CreateDirectory(Path.Combine(resources, ADAPTIVE_ICONS_FOLDER)).FullName;
            var values = new StringBuilder();
            foreach (var icon in icons)
            {
                var name = ResourceName(icon.Name);
                File.WriteAllBytes(Path.Combine(layers, $"{name}_foreground.png"), icon.AndroidForeground);
                if (icon.UsesBackgroundImage) File.WriteAllBytes(Path.Combine(layers, $"{name}_background.png"), icon.AndroidBackground);
                else values.Append($"    <color name=\"{name}_background\">{Colour(icon.AndroidBackgroundColour)}</color>\n");

                if (icon.AndroidMonochrome != null) File.WriteAllBytes(Path.Combine(layers, $"{name}_monochrome.png"), icon.AndroidMonochrome);

                values.Append($"    <item name=\"{name}\" type=\"mipmap\">@mipmap/{name}_foreground</item>\n");
                File.WriteAllText(Path.Combine(adaptiveIcons, $"{name}.xml"), AdaptiveIcon(name, icon.UsesBackgroundImage, icon.AndroidMonochrome != null));
            }

            var valuesFolder = Directory.CreateDirectory(Path.Combine(resources, "values")).FullName;
            File.WriteAllText(Path.Combine(valuesFolder, VALUES_FILE), $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n{values}</resources>\n");
        }

        /// <summary>
        ///     Adaptive icon of an icon, from its layers.
        /// </summary>
        internal static string AdaptiveIcon(string name, bool usesBackgroundImage, bool hasMonochrome)
        {
            var background = usesBackgroundImage ? $"@mipmap/{name}_background" : $"@color/{name}_background";
            var monochrome = hasMonochrome ? $"    <monochrome android:drawable=\"@mipmap/{name}_monochrome\" />\n" : string.Empty;
            return $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<adaptive-icon xmlns:android=\"http://schemas.android.com/apk/res/android\">\n    <background android:drawable=\"{background}\" />\n    <foreground android:drawable=\"@mipmap/{name}_foreground\" />\n{monochrome}</adaptive-icon>\n";
        }

        /// <summary>
        ///     Opaque colour of the background layer, as Android reads it: the background is opaque.
        /// </summary>
        internal static string Colour(Color colour)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(colour);
        }

        private static void Clean(string resources)
        {
            if (!Directory.Exists(resources)) return;

            foreach (var folder in Directory.GetDirectories(resources, "mipmap*"))
            {
                foreach (var file in Directory.GetFiles(folder, RESOURCE_PREFIX + "*"))
                {
                    File.Delete(file);
                }
            }

            var valuesFile = Path.Combine(resources, "values", VALUES_FILE);
            if (File.Exists(valuesFile)) File.Delete(valuesFile);
        }
    }
}