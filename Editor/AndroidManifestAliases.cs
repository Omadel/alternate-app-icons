using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Declares the activity aliases of the alternate icons in the Android manifests of the Gradle project. In the
    ///     manifest of the Unity library module, the alias of the default icon, enabled, and an alias per alternate icon,
    ///     disabled, with its adaptive icon, follow the launcher activity, as the manifest merger requires. This activity
    ///     keeps its launcher filter there, for the other build steps that look for it: the manifest of the launcher
    ///     module, which has priority at the merge, removes this filter from the merged manifest and gives the activity a
    ///     copy of it without the launcher category.
    /// </summary>
    internal static class AndroidManifestAliases
    {
        private const string ANDROID_NAMESPACE = "http://schemas.android.com/apk/res/android";
        private const string ANDROID_PREFIX = "android";
        private const string TOOLS_NAMESPACE = "http://schemas.android.com/tools";
        private const string TOOLS_PREFIX = "tools";
        private const string MAIN_ACTION = "android.intent.action.MAIN";
        private const string LAUNCHER_CATEGORY = "android.intent.category.LAUNCHER";
        private const string INDENT = "  ";

        /// <summary>
        ///     Manifests of the Unity library and launcher modules with the aliases of the icons, replacing what a previous
        ///     build wrote: without icons, the manifests as they were before. Null when the manifest of the Unity library
        ///     has no launcher activity for the aliases to open.
        /// </summary>
        internal static (string Library, string Launcher)? AddAliases(string library, string launcher, IReadOnlyList<string> icons)
        {
            var libraryDocument = Load(library);
            var launcherDocument = Load(launcher);
            var application = libraryDocument.DocumentElement?["application"];
            var launcherApplication = launcherDocument.DocumentElement?["application"];
            var hadAliases = application != null && RemoveAliases(application);
            var hadFilterRemovals = launcherApplication != null && RemoveFilterRemovals(launcherApplication);
            if (icons.Count == 0) return (hadAliases ? Serialize(libraryDocument) : library, hadFilterRemovals ? Serialize(launcherDocument) : launcher);

            var activity = application == null ? null : Elements(application, "activity").FirstOrDefault(element => FindLauncherFilter(element) != null);
            if (activity == null) return null;

            var indent = IndentOf(activity);
            var target = GetAndroidAttribute(activity, "name");
            var previous = InsertAfter(activity, CreateAlias(libraryDocument, AndroidAliases.DEFAULT_ALIAS, null, true, target, indent), indent);
            foreach (var icon in icons)
            {
                previous = InsertAfter(previous, CreateAlias(libraryDocument, AndroidAliases.AliasName(icon), AndroidIconResources.IconReference(icon), false, target, indent), indent);
            }

            AddFilterRemoval(launcherApplication ?? AppendElement(launcherDocument.DocumentElement, launcherDocument.CreateElement("application")), target, FindLauncherFilter(activity));
            return (Serialize(libraryDocument), Serialize(launcherDocument));
        }

        /// <summary>
        ///     Removes the aliases of a previous build.
        /// </summary>
        private static bool RemoveAliases(XmlElement application)
        {
            var aliases = Elements(application, "activity-alias").Where(alias => GetAndroidAttribute(alias, "name").StartsWith(AndroidAliases.PREFIX)).ToList();
            foreach (var alias in aliases)
            {
                Remove(alias);
            }

            return aliases.Count > 0;
        }

        /// <summary>
        ///     In the manifest of the launcher module, removes the launcher filter of an activity from the merged manifest,
        ///     and gives the activity a copy of the filter without the launcher category. The merger finds the filter to
        ///     remove by its actions and categories, so the removal copies all its elements.
        /// </summary>
        private static void AddFilterRemoval(XmlElement application, string activityName, XmlElement filter)
        {
            var document = application.OwnerDocument;
            if (document.DocumentElement.GetNamespaceOfPrefix(TOOLS_PREFIX) != TOOLS_NAMESPACE) document.DocumentElement.SetAttribute("xmlns:" + TOOLS_PREFIX, TOOLS_NAMESPACE);

            var activity = Elements(application, "activity").FirstOrDefault(element => GetAndroidAttribute(element, "name") == activityName) ?? AppendElement(application, CreateElement(document, "activity", ("name", activityName)));
            SetAttribute(AppendCopy(activity, filter), TOOLS_PREFIX, TOOLS_NAMESPACE, "node", "remove");
            Remove(FindCategory(AppendCopy(activity, filter)));
        }

        /// <summary>
        ///     Removes the filter removals of a previous build with the copies that follow them, and the activities they
        ///     leave with nothing but their name.
        /// </summary>
        private static bool RemoveFilterRemovals(XmlElement application)
        {
            var removals = Elements(application, "activity").SelectMany(activity => Elements(activity, "intent-filter")).Where(IsFilterRemoval).ToList();
            foreach (var removal in removals)
            {
                var activity = (XmlElement)removal.ParentNode;
                var copy = Elements(activity, null).SkipWhile(element => element != removal).Skip(1).FirstOrDefault();
                if (copy != null && IsCopyWithoutLauncher(copy, removal)) Remove(copy);

                Remove(removal);
                Collapse(activity);
                if (!activity.IsEmpty || activity.Attributes.Count > 1) continue;

                Remove(activity);
                Collapse(application);
            }

            return removals.Count > 0;
        }

        private static bool IsFilterRemoval(XmlElement filter)
        {
            return filter.GetAttribute("node", TOOLS_NAMESPACE) == "remove" && FindCategory(filter) != null;
        }

        /// <summary>
        ///     Whether a filter is the copy of a removed filter that the build gives the activity: its elements, without
        ///     the launcher category.
        /// </summary>
        private static bool IsCopyWithoutLauncher(XmlElement filter, XmlElement removal)
        {
            var category = FindCategory(removal);
            return filter.Name == "intent-filter" && !filter.HasAttribute("node", TOOLS_NAMESPACE) && Elements(filter, null).Select(element => element.OuterXml).SequenceEqual(Elements(removal, null).Where(element => element != category).Select(element => element.OuterXml));
        }

        /// <summary>
        ///     Appends to an activity a copy of a filter of another manifest, with its attributes and its elements.
        /// </summary>
        private static XmlElement AppendCopy(XmlElement activity, XmlElement filter)
        {
            var document = activity.OwnerDocument;
            var copy = AppendElement(activity, document.CreateElement(filter.Name));
            foreach (var attribute in filter.Attributes.OfType<XmlAttribute>())
            {
                copy.Attributes.Append((XmlAttribute)document.ImportNode(attribute, true));
            }

            foreach (var element in Elements(filter, null))
            {
                AppendElement(copy, (XmlElement)document.ImportNode(element, true));
            }

            return copy;
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
        ///     Intent filter of an activity with the main action and the launcher category.
        /// </summary>
        private static XmlElement FindLauncherFilter(XmlElement activity)
        {
            return Elements(activity, "intent-filter").FirstOrDefault(filter => Elements(filter, "action").Any(action => GetAndroidAttribute(action, "name") == MAIN_ACTION) && FindCategory(filter) != null);
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
            SetAttribute(element, ANDROID_PREFIX, ANDROID_NAMESPACE, name, value);
        }

        private static void SetAttribute(XmlElement element, string prefix, string namespaceUri, string name, string value)
        {
            var attribute = element.OwnerDocument.CreateAttribute(prefix, name, namespaceUri);
            attribute.Value = value;
            element.Attributes.Append(attribute);
        }

        private static void AppendChild(XmlElement parent, XmlNode child, string indent)
        {
            parent.AppendChild(parent.OwnerDocument.CreateWhitespace("\n" + indent));
            parent.AppendChild(child);
        }

        /// <summary>
        ///     Appends an element after the last element of a parent, on a line of its own.
        /// </summary>
        private static XmlElement AppendElement(XmlElement parent, XmlElement element)
        {
            var last = Elements(parent, null).LastOrDefault();
            if (last != null) return (XmlElement)InsertAfter(last, element, IndentOf(last));

            var indent = IndentOf(parent);
            foreach (var whitespace in parent.ChildNodes.OfType<XmlWhitespace>().ToList())
            {
                parent.RemoveChild(whitespace);
            }

            AppendChild(parent, element, indent + INDENT);
            parent.AppendChild(parent.OwnerDocument.CreateWhitespace("\n" + indent));
            return element;
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
        ///     Writes an element left with nothing but whitespace as an empty element, as Unity writes them.
        /// </summary>
        private static void Collapse(XmlElement element)
        {
            if (element.ChildNodes.OfType<XmlNode>().All(node => node is XmlWhitespace)) element.IsEmpty = true;
        }

        /// <summary>
        ///     Indentation of a node, from the whitespace before it.
        /// </summary>
        private static string IndentOf(XmlNode node)
        {
            var whitespace = node.PreviousSibling is XmlWhitespace previous ? previous.Value : string.Empty;
            return whitespace.Substring(whitespace.LastIndexOf('\n') + 1);
        }

        private static XmlDocument Load(string manifest)
        {
            var document = new XmlDocument { PreserveWhitespace = true };
            document.LoadXml(manifest);
            return document;
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