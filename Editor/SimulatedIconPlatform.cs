using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Platform of the editor: the change is simulated, logged, and kept until the editor quits, as a device keeps the
    ///     icon when the app restarts. The icons are those of the settings whose name has no error.
    /// </summary>
    [InitializeOnLoad]
    internal class SimulatedIconPlatform : IIconPlatform
    {
        private const string CURRENT_ICON_KEY = "Javienne.AlternateAppIcons.SimulatedIcon";

        static SimulatedIconPlatform()
        {
            AppIcon.EditorPlatform = new SimulatedIconPlatform();
        }

        public bool IsSupported => true;
        public IReadOnlyList<string> Icons => ValidNames();
        public bool DefersChange => false;

        /// <summary>
        ///     Simulated icon, or the default icon once that icon is no longer in the settings, as a new build without it
        ///     would show.
        /// </summary>
        public string CurrentIcon
        {
            get
            {
                var icon = SessionState.GetString(CURRENT_ICON_KEY, string.Empty);
                return ValidNames().Contains(icon) ? icon : string.Empty;
            }
        }

        public void Apply(string icon, Action<string> completed)
        {
            SessionState.SetString(CURRENT_ICON_KEY, icon);
            Debug.Log($"Alternate App Icons: the app icon is now the {(icon.Length == 0 ? "default icon" : $"{icon} icon")} (simulated in the editor).");
            completed(null);
        }

        private static List<string> ValidNames()
        {
            var names = AlternateAppIconsSettings.instance.Icons.Select(icon => icon.Name).ToList();
            var issues = IconChecks.CheckNames(names);
            return names.Where((name, i) => issues[i].Count == 0).ToList();
        }
    }
}