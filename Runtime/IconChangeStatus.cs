namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Outcome of a request to change the app icon.
    /// </summary>
    public enum IconChangeStatus
    {
        /// <summary>
        ///     The icon is changed. On iOS, the system has shown its alert.
        /// </summary>
        Changed,

        /// <summary>
        ///     The change waits for the app to go to the background (Android), so that a restart of the app goes unseen.
        /// </summary>
        Pending,

        /// <summary>
        ///     The icon is already in place: nothing was done, and a change that was pending is cancelled.
        /// </summary>
        AlreadySet,

        /// <summary>
        ///     This platform or device cannot change the app icon: nothing was done.
        /// </summary>
        NotSupported,

        /// <summary>
        ///     The icon was not changed: the error says why.
        /// </summary>
        Failed
    }
}