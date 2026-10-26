using System;
using NUnit.Framework;
using UnityEngine;

namespace Javienne.AlternateAppIcons.Tests
{
    public class IconSwitcherTests
    {
        private FakeIconPlatform _platform;
        private LogRecorder _logs;
        private IconSwitcher _switcher;

        [SetUp]
        public void SetUp()
        {
            _platform = new FakeIconPlatform();
            _logs = new LogRecorder();
            _switcher = new IconSwitcher(_platform, new Logger(_logs));
        }

        [Test]
        public void Request_NotSupported_ReturnsNotSupportedWithoutApplying()
        {
            _platform.IsSupported = false;
            Assert.That(Request("Hellhound").Status, Is.EqualTo(IconChangeStatus.NotSupported));
            Assert.That(_platform.Applied, Is.Empty);
        }

        [Test]
        public void IconsAndCurrentIcon_NotSupported_ReturnNone()
        {
            _platform.IsSupported = false;
            _platform.CurrentIcon = "Scifi";
            Assert.That(_switcher.Icons, Is.Empty);
            Assert.That(_switcher.CurrentIcon, Is.Empty);
        }

        [Test]
        public void Icons_ReturnsIconsInAlphabeticalOrder()
        {
            _platform.Icons = new[] { "Scifi", "hellhound", "Chocolate" };
            Assert.That(_switcher.Icons, Is.EqualTo(new[] { "Chocolate", "hellhound", "Scifi" }));
        }

        [Test]
        public void Request_UnknownName_ReturnsFailedAndLogsError()
        {
            var result = Request("Robot");
            Assert.That(_logs.Logs, Is.EqualTo(new[] { (LogType.Error, "Alternate App Icons: No alternate icon is named \"Robot\". The app has these alternate icons: Chocolate, Hellhound, Scifi.") }));
            Assert.That(result.Status, Is.EqualTo(IconChangeStatus.Failed));
            Assert.That(result.Error, Does.Contain("Robot"));
            Assert.That(_platform.Applied, Is.Empty);
        }

        [Test]
        public void Request_NewIcon_AppliesItAndReturnsChanged()
        {
            var result = Request("Hellhound");
            Assert.That(result.Status, Is.EqualTo(IconChangeStatus.Changed));
            Assert.That(result.Icon, Is.EqualTo("Hellhound"));
            Assert.That(_platform.Applied, Is.EqualTo(new[] { "Hellhound" }));
            Assert.That(_switcher.CurrentIcon, Is.EqualTo("Hellhound"));
        }

        [Test]
        public void Request_NameInAnotherCase_AppliesDeclaredName()
        {
            Assert.That(Request("hellHOUND").Icon, Is.EqualTo("Hellhound"));
            Assert.That(_platform.Applied, Is.EqualTo(new[] { "Hellhound" }));
        }

        [Test]
        public void Request_IconInPlace_ReturnsAlreadySetWithoutApplying()
        {
            _platform.CurrentIcon = "Scifi";
            Assert.That(Request("Scifi").Status, Is.EqualTo(IconChangeStatus.AlreadySet));
            Assert.That(_platform.Applied, Is.Empty);
        }

        [Test]
        public void Request_NullName_AppliesDefaultIcon()
        {
            _platform.CurrentIcon = "Scifi";
            Assert.That(Request(null).Status, Is.EqualTo(IconChangeStatus.Changed));
            Assert.That(_platform.Applied, Is.EqualTo(new[] { string.Empty }));
        }

        [Test]
        public void Request_EmptyNameWithDefaultInPlace_ReturnsAlreadySet()
        {
            Assert.That(Request(string.Empty).Status, Is.EqualTo(IconChangeStatus.AlreadySet));
            Assert.That(_platform.Applied, Is.Empty);
        }

        [Test]
        public void Request_PlatformError_ReturnsFailedAndLogsWarning()
        {
            _platform.Error = "Resource temporarily unavailable";
            var result = Request("Hellhound");
            Assert.That(_logs.Logs, Is.EqualTo(new[] { (LogType.Warning, "Alternate App Icons: The Hellhound icon could not be set: Resource temporarily unavailable") }));
            Assert.That(result.Status, Is.EqualTo(IconChangeStatus.Failed));
            Assert.That(result.Error, Is.EqualTo("Resource temporarily unavailable"));
            Assert.That(_switcher.CurrentIcon, Is.Empty);
        }

        [Test]
        public void Request_WhileAnotherChangeIsApplied_ReturnsFailedWithoutApplying()
        {
            _platform.CompletesLater = true;
            IconChangeResult? first = null;
            _switcher.Request("Hellhound", result => first = result);
            Assert.That(first, Is.Null);
            Assert.That(Request("Scifi").Status, Is.EqualTo(IconChangeStatus.Failed));
            _platform.Complete();
            Assert.That(first?.Status, Is.EqualTo(IconChangeStatus.Changed));
            Assert.That(_platform.Applied, Is.EqualTo(new[] { "Hellhound" }));
        }

        [Test]
        public void Request_AfterAnotherChangeEnded_AppliesIt()
        {
            _platform.CompletesLater = true;
            _switcher.Request("Hellhound", _ => { });
            _platform.Complete();
            _switcher.Request("Scifi", _ => { });
            _platform.Complete();
            Assert.That(_platform.Applied, Is.EqualTo(new[] { "Hellhound", "Scifi" }));
        }

        [Test]
        public void Request_Deferred_ReturnsPendingWithoutApplying()
        {
            _platform.DefersChange = true;
            Assert.That(Request("Hellhound").Status, Is.EqualTo(IconChangeStatus.Pending));
            Assert.That(_switcher.PendingIcon, Is.EqualTo("Hellhound"));
            Assert.That(_switcher.CurrentIcon, Is.Empty);
            Assert.That(_platform.Applied, Is.Empty);
        }

        [Test]
        public void Request_DeferredDefaultIcon_KeepsEmptyPendingIcon()
        {
            _platform.DefersChange = true;
            _platform.CurrentIcon = "Scifi";
            Assert.That(Request(null).Status, Is.EqualTo(IconChangeStatus.Pending));
            Assert.That(_switcher.PendingIcon, Is.Empty);
        }

        [Test]
        public void Request_DeferredTwice_KeepsLastIcon()
        {
            _platform.DefersChange = true;
            Request("Hellhound");
            Request("Scifi");
            Assert.That(_switcher.PendingIcon, Is.EqualTo("Scifi"));
        }

        [Test]
        public void Request_DeferredIconInPlace_ReturnsAlreadySetAndCancelsPendingIcon()
        {
            _platform.DefersChange = true;
            Request("Hellhound");
            Assert.That(Request(null).Status, Is.EqualTo(IconChangeStatus.AlreadySet));
            Assert.That(_switcher.PendingIcon, Is.Null);
        }

        [Test]
        public void ApplyPending_PendingIcon_AppliesItOnce()
        {
            _platform.DefersChange = true;
            Request("Hellhound");
            _switcher.ApplyPending();
            _switcher.ApplyPending();
            Assert.That(_platform.Applied, Is.EqualTo(new[] { "Hellhound" }));
            Assert.That(_switcher.PendingIcon, Is.Null);
            Assert.That(_switcher.CurrentIcon, Is.EqualTo("Hellhound"));
        }

        [Test]
        public void ApplyPending_NothingPending_DoesNothing()
        {
            _switcher.ApplyPending();
            Assert.That(_platform.Applied, Is.Empty);
        }

        [Test]
        public void ApplyPending_PlatformError_LogsWarning()
        {
            _platform.DefersChange = true;
            _platform.Error = "Component not found";
            Request("Scifi");
            _switcher.ApplyPending();
            Assert.That(_logs.Logs, Is.EqualTo(new[] { (LogType.Warning, "Alternate App Icons: The Scifi icon could not be set: Component not found") }));
        }

        [Test]
        public void SetIcon_ChangeDone_AwaitableReturnsResult()
        {
            var awaiter = _switcher.SetIcon("Chocolate").GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True);
            Assert.That(awaiter.GetResult().Status, Is.EqualTo(IconChangeStatus.Changed));
        }

        [Test]
        public void Succeeded_EachStatus_ReturnsWhetherTheIconIsOrWillBeShown()
        {
            var statuses = new[] { IconChangeStatus.Changed, IconChangeStatus.Pending, IconChangeStatus.AlreadySet, IconChangeStatus.NotSupported, IconChangeStatus.Failed };
            Assert.That(statuses, Has.Length.EqualTo(Enum.GetValues(typeof(IconChangeStatus)).Length));
            foreach (var status in statuses)
            {
                var expected = status != IconChangeStatus.NotSupported && status != IconChangeStatus.Failed;
                Assert.That(new IconChangeResult(status, "Scifi").Succeeded, Is.EqualTo(expected), status.ToString());
            }
        }

        [Test]
        public void ToString_DefaultIconAndError_NamesThem()
        {
            Assert.That(new IconChangeResult(IconChangeStatus.Failed, string.Empty, "Busy").ToString(), Is.EqualTo("Failed (default icon): Busy"));
        }

        /// <summary>
        ///     Result of a request that the platform completes right away.
        /// </summary>
        private IconChangeResult Request(string name)
        {
            IconChangeResult? result = null;
            _switcher.Request(name, completed => result = completed);
            Assert.That(result, Is.Not.Null, "The request did not complete.");
            return result.Value;
        }
    }
}