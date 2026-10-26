using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Project settings of the alternate icons, saved in the ProjectSettings folder: versioned with the project,
    ///     without an asset in Assets. The default icon stays the one of the Player Settings.
    /// </summary>
    [FilePath("ProjectSettings/AlternateAppIconsSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class AlternateAppIconsSettings : ScriptableSingleton<AlternateAppIconsSettings>
    {
        [SerializeField] private List<AlternateIcon> _icons = new List<AlternateIcon>();
        [SerializeField] private bool _failBuildOnError;

        public IReadOnlyList<AlternateIcon> Icons => _icons;
        public bool FailBuildOnError => _failBuildOnError;

        /// <summary>
        ///     Lets the settings page edit the singleton, which Unity marks as not editable.
        /// </summary>
        private void OnEnable()
        {
            hideFlags &= ~HideFlags.NotEditable;
        }

        /// <summary>
        ///     Writes the settings to their file in the ProjectSettings folder, as text.
        /// </summary>
        internal void Save()
        {
            Save(true);
        }
    }
}