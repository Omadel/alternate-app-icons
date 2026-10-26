namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Result of a request to change the app icon.
    /// </summary>
    public readonly struct IconChangeResult
    {
        internal IconChangeResult(IconChangeStatus status, string icon, string error = null)
        {
            Status = status;
            Icon = icon;
            Error = error;
        }

        public IconChangeStatus Status { get; }

        /// <summary>
        ///     Name of the requested icon, empty for the default icon.
        /// </summary>
        public string Icon { get; }

        /// <summary>
        ///     Why the change failed, null otherwise.
        /// </summary>
        public string Error { get; }

        /// <summary>
        ///     Whether the requested icon is, or will be, the app icon.
        /// </summary>
        public bool Succeeded => Status == IconChangeStatus.Changed || Status == IconChangeStatus.Pending || Status == IconChangeStatus.AlreadySet;

        public override string ToString()
        {
            var icon = string.IsNullOrEmpty(Icon) ? "default icon" : Icon;
            return Error == null ? $"{Status} ({icon})" : $"{Status} ({icon}): {Error}";
        }
    }
}