using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Checks of the alternate icons, run live by the settings page and again by the build. They read the image files
    ///     themselves, which the build copies as they are, not the imported textures.
    /// </summary>
    internal static class IconChecks
    {
        /// <summary>
        ///     Size of the single iOS image, from which iOS scales the icon down for every place it shows it.
        /// </summary>
        internal const int IOS_IMAGE_SIZE = 1024;

        /// <summary>
        ///     Size of the 108 dp adaptive icon layers at the xxxhdpi density, the only one the build writes.
        /// </summary>
        internal const int ANDROID_LAYER_SIZE = 432;

        /// <summary>
        ///     Problems of each icon, errors first.
        /// </summary>
        internal static List<IconIssue>[] Check(IReadOnlyList<IconFiles> icons)
        {
            var issues = CheckNames(icons.Select(icon => icon.Name).ToList());
            for (var i = 0; i < icons.Count; i++)
            {
                var icon = icons[i];
                issues[i].AddRange(CheckImage(icon.IosImage, IconField.IosImage));
                issues[i].AddRange(CheckImage(icon.AndroidForeground, IconField.AndroidForeground));
                if (icon.UsesBackgroundImage) issues[i].AddRange(CheckImage(icon.AndroidBackground, IconField.AndroidBackground));

                issues[i].AddRange(CheckImage(icon.AndroidMonochrome, IconField.AndroidMonochrome));
                issues[i] = ErrorsFirst(issues[i]);
            }

            return issues;
        }

        /// <summary>
        ///     Problems of the name of each icon: empty, invalid, or the same as another one regardless of case.
        /// </summary>
        internal static List<IconIssue>[] CheckNames(IReadOnlyList<string> names)
        {
            var issues = new List<IconIssue>[names.Count];
            for (var i = 0; i < names.Count; i++)
            {
                issues[i] = new List<IconIssue>();
                var name = names[i];
                if (string.IsNullOrWhiteSpace(name))
                {
                    issues[i].Add(new IconIssue(IconField.Name, IconIssueKind.EmptyName, "The name is empty: the game passes it to the API to show this icon."));
                    continue;
                }

                if (!IconNames.IsValid(name)) issues[i].Add(new IconIssue(IconField.Name, IconIssueKind.InvalidName, $"\"{name}\" is not a valid name: use an ASCII letter, then ASCII letters, digits or underscores."));

                for (var j = 0; j < names.Count; j++)
                {
                    if (j == i || !IconNames.AreSame(name, names[j])) continue;

                    issues[i].Add(new IconIssue(IconField.Name, IconIssueKind.DuplicateName, $"Another icon is also named \"{names[j]}\": names must differ, regardless of case."));
                    break;
                }
            }

            return issues;
        }

        /// <summary>
        ///     Problems of an image file of an icon, errors first, from its bytes: null when the image is not set, which
        ///     only the monochrome layer may be.
        /// </summary>
        internal static List<IconIssue> CheckImage(byte[] file, IconField field)
        {
            var issues = new List<IconIssue>();
            var label = Label(field);
            if (file == null)
            {
                if (field != IconField.AndroidMonochrome) issues.Add(new IconIssue(field, IconIssueKind.MissingImage, $"No {label} is set."));

                return issues;
            }

            if (!PngFile.IsPng(file) || !PngFile.TryReadPixels(file, out var width, out var height, out var pixels))
            {
                issues.Add(new IconIssue(field, IconIssueKind.NotPng, $"The {label} is not a PNG file: the build copies the file as it is."));
                return issues;
            }

            var size = field == IconField.IosImage ? IOS_IMAGE_SIZE : ANDROID_LAYER_SIZE;
            if (width != size || height != size) issues.Add(new IconIssue(field, IconIssueKind.WrongSize, $"The {label} is {width} × {height} px: it must be {size} × {size} px."));

            switch (field)
            {
                case IconField.IosImage:
                    var transparent = CountTransparentPixels(pixels);
                    if (transparent > 0) issues.Add(new IconIssue(field, IconIssueKind.TransparentIosImage, $"{Pixels(transparent)} transparent in the iOS image: iOS icons must be opaque."));
                    else if (PngFile.HasAlphaChannel(file)) issues.Add(new IconIssue(field, IconIssueKind.IosAlphaChannel, "The iOS image has an alpha channel, although it is opaque: save it without one, as Apple asks for app icons."));

                    break;
                case IconField.AndroidBackground:
                    var transparentBackground = CountTransparentPixels(pixels);
                    if (transparentBackground > 0) issues.Add(new IconIssue(field, IconIssueKind.TransparentBackground, $"{Pixels(transparentBackground)} transparent in the Android background: Android expects an opaque background layer."));

                    break;
                case IconField.AndroidMonochrome:
                    var nonWhite = CountNonWhitePixels(pixels);
                    if (nonWhite > 0) issues.Add(new IconIssue(field, IconIssueKind.NotWhiteMonochrome, $"{Pixels(nonWhite)} visible but not pure white in the Android monochrome layer: Android only keeps its opacity."));

                    break;
            }

            return issues;
        }

        /// <summary>
        ///     Issues with the errors first, in their order otherwise.
        /// </summary>
        internal static List<IconIssue> ErrorsFirst(IEnumerable<IconIssue> issues)
        {
            return issues.OrderBy(issue => issue.IsError ? 0 : 1).ToList();
        }

        /// <summary>
        ///     Number of pixels that are not fully opaque.
        /// </summary>
        internal static int CountTransparentPixels(Color32[] pixels)
        {
            var count = 0;
            foreach (var pixel in pixels)
            {
                if (pixel.a < 255) count++;
            }

            return count;
        }

        /// <summary>
        ///     Number of visible pixels, with any opacity above zero, that are not pure white.
        /// </summary>
        internal static int CountNonWhitePixels(Color32[] pixels)
        {
            var count = 0;
            foreach (var pixel in pixels)
            {
                if (pixel.a > 0 && (pixel.r != 255 || pixel.g != 255 || pixel.b != 255)) count++;
            }

            return count;
        }

        /// <summary>
        ///     Name of a field in the messages.
        /// </summary>
        internal static string Label(IconField field)
        {
            switch (field)
            {
                case IconField.Name:
                    return "name";
                case IconField.IosImage:
                    return "iOS image";
                case IconField.AndroidForeground:
                    return "Android foreground";
                case IconField.AndroidBackground:
                    return "Android background";
                default:
                    return "Android monochrome layer";
            }
        }

        private static string Pixels(int count)
        {
            return count == 1 ? "1 pixel is" : $"{count} pixels are";
        }
    }
}