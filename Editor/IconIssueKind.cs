namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     Problems an alternate icon can have, from the settings page to the build.
    /// </summary>
    internal enum IconIssueKind
    {
        EmptyName,
        InvalidName,
        DuplicateName,
        MissingImage,
        NotPng,
        WrongSize,
        TransparentIosImage,
        IosAlphaChannel,
        TransparentBackground,
        NotWhiteMonochrome
    }
}