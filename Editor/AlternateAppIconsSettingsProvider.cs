using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Project Settings page of the alternate icons: the list of icons with their images, the number of icons and the
    ///     size of their image files, and the checks of each icon, run again whenever a name or an image file changes.
    /// </summary>
    internal class AlternateAppIconsSettingsProvider : SettingsProvider
    {
        private const string PATH = "Project/Alternate App Icons";
        private const int ADAPTIVE_ICONS_API_LEVEL = 26;

        private static readonly GUIContent _failBuildLabel = new GUIContent("Fail Build On Error", "Fails the build when an icon has an error that concerns its platform. Otherwise the error is logged as a warning and the build has no alternate icon. Warnings never fail the build.");
        private static readonly GUIContent _nameLabel = new GUIContent("Name", "Identifier the game passes to the API to show this icon: an ASCII letter, then ASCII letters, digits or underscores.");
        private static readonly GUIContent _iosLabel = new GUIContent("iOS");
        private static readonly GUIContent _iosImageLabel = new GUIContent("Image", "PNG of 1024 × 1024 px without transparency, copied as it is into the build: iOS scales it down for every place it shows the icon.");
        private static readonly GUIContent _androidLabel = new GUIContent("Android Adaptive Icon");
        private static readonly GUIContent _foregroundLabel = new GUIContent("Foreground", "PNG of 432 × 432 px, the 108 dp layer at the xxxhdpi density, copied as it is into the build. Launchers mask it to their shape and show its central 72 dp.");
        private static readonly GUIContent _backgroundLabel = new GUIContent("Background", "Layer under the foreground: a colour, or an image.");
        private static readonly GUIContent _backgroundColourLabel = new GUIContent("Colour", "Opaque colour filling the background layer.");
        private static readonly GUIContent _backgroundImageLabel = new GUIContent("Image", "Opaque PNG of 432 × 432 px, copied as it is into the build.");
        private static readonly GUIContent _monochromeLabel = new GUIContent("Monochrome", "Optional PNG of 432 × 432 px, drawn in white on a transparent background: Android 13 and later tint it to the theme colours when themed icons are on. Without it, the icon keeps its colours.");
        private static readonly GUIContent _addLabel = new GUIContent("Add Icon");
        private static readonly GUIContent _removeLabel = new GUIContent("Remove", "Removes this icon from the settings. Its images stay in the project.");

        private readonly Dictionary<string, List<IconIssue>> _imageIssues = new Dictionary<string, List<IconIssue>>();
        private SerializedObject _settings;
        private GUIStyle _pageStyle;

        private AlternateAppIconsSettingsProvider() : base(PATH, SettingsScope.Project, new[] { "icon", "app icon", "alternate", "ios", "android", "adaptive" })
        {
        }

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new AlternateAppIconsSettingsProvider();
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _settings = new SerializedObject(AlternateAppIconsSettings.instance);
            _imageIssues.Clear();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public override void OnDeactivate()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        public override void OnGUI(string searchContext)
        {
            _pageStyle ??= new GUIStyle { margin = new RectOffset(10, 10, 10, 10) };
            _settings.Update();
            var icons = _settings.FindProperty("_icons");
            using (new EditorGUILayout.VerticalScope(_pageStyle))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_settings.FindProperty("_failBuildOnError"), _failBuildLabel);
                DrawSummary();
                var nameIssues = IconChecks.CheckNames(Names(icons));
                var removed = -1;
                for (var i = 0; i < icons.arraySize; i++)
                {
                    if (DrawIcon(icons.GetArrayElementAtIndex(i), i, nameIssues[i])) removed = i;
                }

                EditorGUILayout.Space();
                var added = GUILayout.Button(_addLabel);
                if (removed >= 0) icons.DeleteArrayElementAtIndex(removed);

                if (added) AddIcon(icons);

                if (EditorGUI.EndChangeCheck() || removed >= 0 || added)
                {
                    _settings.ApplyModifiedProperties();
                    AlternateAppIconsSettings.instance.Save();
                }
            }
        }

        /// <summary>
        ///     What the page is about, the number of icons with the size of their image files for each platform, and the
        ///     Android versions that keep the default icon.
        /// </summary>
        private static void DrawSummary()
        {
            var icons = AlternateAppIconsSettings.instance.Icons;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("The default icon is the one of the Player Settings. The game switches to one of these icons by its name; their images are copied as they are into the iOS and Android builds.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            if (icons.Count == 0)
            {
                EditorGUILayout.HelpBox("No alternate icon: the builds only have the default icon.", MessageType.None);
            }
            else
            {
                var iosSize = TotalSize(icons.Select(icon => icon.IosImage));
                var androidSize = TotalSize(icons.SelectMany(icon => new[] { icon.AndroidForeground, icon.AndroidBackgroundKind == AndroidBackgroundKind.Image ? icon.AndroidBackgroundImage : null, icon.AndroidMonochrome }));
                EditorGUILayout.HelpBox($"{icons.Count} {(icons.Count == 1 ? "icon" : "icons")} · iOS images: {EditorUtility.FormatBytes(iosSize)} · Android images: {EditorUtility.FormatBytes(androidSize)}\nSizes of the image files: the builds compress them again, so an icon adds a different size to the download.", MessageType.None);
            }

            if ((int)PlayerSettings.Android.minSdkVersion < ADAPTIVE_ICONS_API_LEVEL) EditorGUILayout.HelpBox($"Android 7.1 and earlier keep the default icon: alternate icons need adaptive icons, from Android 8.0 (API level {ADAPTIVE_ICONS_API_LEVEL}).", MessageType.Info);
        }

        /// <summary>
        ///     Fields of an icon in a box, with its issues, errors first. Returns whether its Remove button was clicked.
        /// </summary>
        private bool DrawIcon(SerializedProperty icon, int index, List<IconIssue> nameIssues)
        {
            var name = icon.FindPropertyRelative("_name");
            var issues = new List<IconIssue>(nameIssues);
            bool remove;
            EditorGUILayout.Space();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(name.stringValue) ? $"Icon {index + 1}" : name.stringValue, EditorStyles.boldLabel);
                    remove = GUILayout.Button(_removeLabel, EditorStyles.miniButton, GUILayout.ExpandWidth(false));
                }

                EditorGUILayout.PropertyField(name, _nameLabel);
                EditorGUILayout.LabelField(_iosLabel, EditorStyles.miniBoldLabel);
                DrawImage(icon, "_iosImage", _iosImageLabel, IconField.IosImage, issues);
                EditorGUILayout.LabelField(_androidLabel, EditorStyles.miniBoldLabel);
                DrawImage(icon, "_androidForeground", _foregroundLabel, IconField.AndroidForeground, issues);
                var backgroundKind = icon.FindPropertyRelative("_androidBackgroundKind");
                EditorGUILayout.PropertyField(backgroundKind, _backgroundLabel);
                EditorGUI.indentLevel++;
                if (backgroundKind.enumValueIndex == (int)AndroidBackgroundKind.Image) DrawImage(icon, "_androidBackgroundImage", _backgroundImageLabel, IconField.AndroidBackground, issues);
                else DrawOpaqueColour(icon.FindPropertyRelative("_androidBackgroundColour"), _backgroundColourLabel);

                EditorGUI.indentLevel--;
                DrawImage(icon, "_androidMonochrome", _monochromeLabel, IconField.AndroidMonochrome, issues);
                foreach (var issue in IconChecks.ErrorsFirst(issues))
                {
                    EditorGUILayout.HelpBox(issue.Message, issue.IsError ? MessageType.Error : MessageType.Warning);
                }
            }

            return remove;
        }

        /// <summary>
        ///     Image field of an icon, adding the issues of its file.
        /// </summary>
        private void DrawImage(SerializedProperty icon, string fieldName, GUIContent label, IconField field, List<IconIssue> issues)
        {
            var image = icon.FindPropertyRelative(fieldName);
            EditorGUILayout.PropertyField(image, label);
            issues.AddRange(ImageIssues((Texture2D)image.objectReferenceValue, field));
        }

        /// <summary>
        ///     Colour field without alpha: the background layer of an adaptive icon is opaque.
        /// </summary>
        private static void DrawOpaqueColour(SerializedProperty colour, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            var value = EditorGUILayout.ColorField(label, colour.colorValue, true, false, false);
            if (EditorGUI.EndChangeCheck()) colour.colorValue = new Color(value.r, value.g, value.b, 1);
        }

        /// <summary>
        ///     Issues of an image file, checked again only when the image or the date of its file changed.
        /// </summary>
        private List<IconIssue> ImageIssues(Texture2D image, IconField field)
        {
            var key = $"{field}|{ImageState(image)}";
            if (_imageIssues.TryGetValue(key, out var issues)) return issues;

            issues = IconChecks.CheckImage(IconFiles.ReadFile(image), field);
            _imageIssues[key] = issues;
            return issues;
        }

        /// <summary>
        ///     What the checks of an image depend on: the image and the date of its file.
        /// </summary>
        private static string ImageState(Texture2D image)
        {
            if (image == null) return string.Empty;

            var path = AssetDatabase.GetAssetPath(image);
            if (string.IsNullOrEmpty(path)) return image.GetInstanceID().ToString();

            var fullPath = Path.GetFullPath(path);
            var date = File.Exists(fullPath) ? File.GetLastWriteTimeUtc(fullPath).Ticks : 0;
            return $"{path}|{date}";
        }

        /// <summary>
        ///     Total size of the files of images, each file counted once.
        /// </summary>
        private static long TotalSize(IEnumerable<Texture2D> images)
        {
            var paths = new HashSet<string>();
            var total = 0L;
            foreach (var image in images)
            {
                var path = image == null ? null : AssetDatabase.GetAssetPath(image);
                if (string.IsNullOrEmpty(path) || !paths.Add(path)) continue;

                var file = new FileInfo(Path.GetFullPath(path));
                if (file.Exists) total += file.Length;
            }

            return total;
        }

        private static List<string> Names(SerializedProperty icons)
        {
            var names = new List<string>();
            for (var i = 0; i < icons.arraySize; i++)
            {
                names.Add(icons.GetArrayElementAtIndex(i).FindPropertyRelative("_name").stringValue);
            }

            return names;
        }

        /// <summary>
        ///     Adds an empty icon at the end of the list; Unity would otherwise copy the last one.
        /// </summary>
        private static void AddIcon(SerializedProperty icons)
        {
            icons.arraySize++;
            var icon = icons.GetArrayElementAtIndex(icons.arraySize - 1);
            icon.FindPropertyRelative("_name").stringValue = string.Empty;
            icon.FindPropertyRelative("_iosImage").objectReferenceValue = null;
            icon.FindPropertyRelative("_androidForeground").objectReferenceValue = null;
            icon.FindPropertyRelative("_androidBackgroundKind").enumValueIndex = (int)AndroidBackgroundKind.Colour;
            icon.FindPropertyRelative("_androidBackgroundColour").colorValue = Color.white;
            icon.FindPropertyRelative("_androidBackgroundImage").objectReferenceValue = null;
            icon.FindPropertyRelative("_androidMonochrome").objectReferenceValue = null;
        }

        private void OnUndoRedo()
        {
            AlternateAppIconsSettings.instance.Save();
            _settings.Update();
            Repaint();
        }
    }
}