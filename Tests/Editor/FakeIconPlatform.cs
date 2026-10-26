using System;
using System.Collections.Generic;

namespace Javienne.AlternateAppIcons.Tests
{
    /// <summary>
    ///     Platform of the tests: records the icons it is asked to apply, and can fail or call back later.
    /// </summary>
    internal class FakeIconPlatform : IIconPlatform
    {
        private Action _completion;

        public bool IsSupported { get; set; } = true;
        public IReadOnlyList<string> Icons { get; set; } = new[] { "Hellhound", "Scifi", "Chocolate" };
        public string CurrentIcon { get; set; } = string.Empty;
        public bool DefersChange { get; set; }

        /// <summary>
        ///     Error the next changes fail with, null for success.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        ///     Whether changes call back only on <see cref="Complete" />, as the iOS alert does.
        /// </summary>
        public bool CompletesLater { get; set; }

        public List<string> Applied { get; } = new List<string>();

        public void Apply(string icon, Action<string> completed)
        {
            Applied.Add(icon);
            var error = Error;
            _completion = () =>
            {
                if (error == null) CurrentIcon = icon;

                completed(error);
            };
            if (!CompletesLater) Complete();
        }

        public void Complete()
        {
            var completion = _completion;
            _completion = null;
            completion?.Invoke();
        }
    }
}