namespace Javienne.AlternateAppIcons
{
    /// <summary>
    ///     A problem of an alternate icon and its message. An error leaves every alternate icon out of the builds it
    ///     concerns, or fails them if the settings ask for it; warnings never stop a build.
    /// </summary>
    internal readonly struct IconIssue
    {
        public IconIssue(IconField field, IconIssueKind kind, string message)
        {
            Field = field;
            Kind = kind;
            Message = message;
        }

        public IconField Field { get; }
        public IconIssueKind Kind { get; }
        public string Message { get; }
        public bool IsError => Kind != IconIssueKind.IosAlphaChannel && Kind != IconIssueKind.TransparentBackground && Kind != IconIssueKind.NotWhiteMonochrome;
        public bool ConcernsIos => Field == IconField.Name || Field == IconField.IosImage;
        public bool ConcernsAndroid => Field != IconField.IosImage;
    }
}