using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Declares the activity aliases of the alternate icons in the Android manifest of the Unity library module. The
    ///     launcher category moves from the launcher activity to the alias of the default icon, enabled, and each
    ///     alternate icon gets an alias of its own, disabled, with its adaptive icon. The aliases follow their activity, as
    ///     the manifest merger requires.
    /// </summary>
    internal static class AndroidManifestAliases
    {
        private const string ANDROID_NAMESPACE = "http://schemas.android.com/apk/res/android";
        private const string ANDROID_PREFIX = "android";
        private const string MAIN_ACTION = "android.intent.action.MAIN";
        private const string LAUNCHER_CATEGORY = "android.intent.category.LAUNCHER";
        private const string INDENT = "  ";

        /// <summary>
        ///     Manifest with the aliases of the icons, replacing those of a previous build: without icons, the manifest as
        ///     it was before any alias. Null when it has no launcher activity for the aliases to open.
        /// </summary>
        internal static string AddAliases(string manifest, IReadOnlyList<string> icons)
        {
            var document = new XmlDocument { PreserveWhitespace = true };
            document.LoadXml(manifest);
            var application = document.DocumentElement?["application"];
            if (application == null) return icons.Count == 0 ? manifest : null;

            var hadAliases = RemoveAliases(application);
            if (icons.Count == 0) return hadAliases ? Serialize(document) : manifest;

            var activity = Elements(application, "activity").FirstOrDefault(element => FindFilter(element, true) != null);
            if (activity == null) return null;

            Remove(FindCategory(FindFilter(activity, true)));
            var indent = IndentOf(activity);
            var target = GetAndroidAttribute(activity, "name");
            var previous = InsertAfter(activity, CreateAlias(document, AndroidAliases.DEFAULT_ALIAS, null, true, target, indent), indent);
            foreach (var icon in icons)
            {
                previous = InsertAfter(previous, CreateAlias(document, AndroidAliases.AliasName(icon), AndroidIconResources.IconReference(icon), false, target, indent), indent);
            }

            return Serialize(document);
        }

        /// <summary>
        ///     Removes the aliases of a previous build, and gives the launcher category back to their activity.
        /// </summary>
        private static bool RemoveAliases(XmlElement application)
        {
            var aliases = Elements(application, "activity-alias").Where(alias => GetAndroidAttribute(alias, "name").StartsWith(AndroidAliases.PREFIX)).ToList();
            foreach (var alias in aliases)
            {
                if (GetAndroidAttribute(alias, "name") == AndroidAliases.DEFAULT_ALIAS) RestoreLauncherCategory(application, GetAndroidAttribute(alias, "targetActivity"));

                Remove(alias);
            }

            return aliases.Count > 0;
        }

        private static void RestoreLauncherCategory(XmlElement application, string activityName)
        {
            var activity = Elements(application, "activity").FirstOrDefault(element => GetAndroidAttribute(element, "name") == activityName);
            var filter = activity == null ? null : FindFilter(activity, false);
            if (filter == null || FindCategory(filter) != null) return;

            var first = Elements(filter, null).FirstOrDefault();
            var category = CreateElement(filter.OwnerDocument, "category", ("name", LAUNCHER_CATEGORY));
            filter.PrependChild(category);
            filter.PrependChild(filter.OwnerDocument.CreateWhitespace("\n" + (first == null ? IndentOf(filter) + INDENT : IndentOf(first))));
        }

        private static XmlElement CreateAlias(XmlDocument document, string name, string icon, bool isEnabled, string targetActivity, string indent)
        {
            var alias = CreateElement(document, "activity-alias", ("name", name), ("enabled", isEnabled ? "true" : "false"), ("exported", "true"));
            if (icon != null)
            {
                SetAndroidAttribute(alias, "icon", icon);
                SetAndroidAttribute(alias, "roundIcon", icon);
            }

            SetAndroidAttribute(alias, "targetActivity", targetActivity);
            var filter = CreateElement(document, "intent-filter");
            AppendChild(filter, CreateElement(document, "action", ("name", MAIN_ACTION)), indent + INDENT + INDENT);
            AppendChild(filter, CreateElement(document, "category", ("name", LAUNCHER_CATEGORY)), indent + INDENT + INDENT);
            filter.AppendChild(document.CreateWhitespace("\n" + indent + INDENT));
            AppendChild(alias, filter, indent + INDENT);
            alias.AppendChild(document.CreateWhitespace("\n" + indent));
            return alias;
        }

        /// <summary>
        ///     Intent filter of an activity with the main action, and with the launcher category or without it.
        /// </summary>
        private static XmlElement FindFilter(XmlElement activity, bool hasLauncherCategory)
        {
            return Elements(activity, "intent-filter").FirstOrDefault(filter => Elements(filter, "action").Any(action => GetAndroidAttribute(action, "name") == MAIN_ACTION) && (FindCategory(filter) != null) == hasLauncherCategory);
        }

        private static XmlElement FindCategory(XmlElement filter)
        {
            return Elements(filter, "category").FirstOrDefault(category => GetAndroidAttribute(category, "name") == LAUNCHER_CATEGORY);
        }

        private static IEnumerable<XmlElement> Elements(XmlElement parent, string name)
        {
            return parent.ChildNodes.OfType<XmlElement>().Where(element => name == null || element.Name == name);
        }

        private static XmlElement CreateElement(XmlDocument document, string name, params (string name, string value)[] attributes)
        {
            var element = document.CreateElement(name);
            foreach (var (attribute, value) in attributes)
            {
                SetAndroidAttribute(element, attribute, value);
            }

            return element;
        }

        private static string GetAndroidAttribute(XmlElement element, string name)
        {
            return element.GetAttribute(name, ANDROID_NAMESPACE);
        }

        /// <summary>
        ///     Sets an attribute with the android prefix, which the manifest declares.
        /// </summary>
        private static void SetAndroidAttribute(XmlElement element, string name, string value)
        {
            var attribute = element.OwnerDocument.CreateAttribute(ANDROID_PREFIX, name, ANDROID_NAMESPACE);
            attribute.Value = value;
            element.Attributes.Append(attribute);
        }

        private static void AppendChild(XmlElement parent, XmlNode child, string indent)
        {
            parent.AppendChild(parent.OwnerDocument.CreateWhitespace("\n" + indent));
            parent.AppendChild(child);
        }

        private static XmlNode InsertAfter(XmlNode previous, XmlNode node, string indent)
        {
            var parent = previous.ParentNode;
            var whitespace = parent.InsertAfter(previous.OwnerDocument.CreateWhitespace("\n" + indent), previous);
            return parent.InsertAfter(node, whitespace);
        }

        /// <summary>
        ///     Removes a node with the whitespace before it, which holds its line break and indentation.
        /// </summary>
        private static void Remove(XmlNode node)
        {
            if (node.PreviousSibling is XmlWhitespace whitespace) node.ParentNode.RemoveChild(whitespace);

            node.ParentNode.RemoveChild(node);
        }

        /// <summary>
        ///     Indentation of a node, from the whitespace before it.
        /// </summary>
        private static string IndentOf(XmlNode node)
        {
            var whitespace = node.PreviousSibling is XmlWhitespace previous ? previous.Value : string.Empty;
            return whitespace.Substring(whitespace.LastIndexOf('\n') + 1);
        }

        private static string Serialize(XmlDocument document)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false), NewLineHandling = NewLineHandling.None }))
                {
                    document.Save(writer);
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}