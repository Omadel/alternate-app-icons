using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Javienne.AlternateAppIcons.Tests
{
    /// <summary>
    ///     Log handler of the tests: records the messages instead of sending them to the console, which keeps showing
    ///     messages a test expects.
    /// </summary>
    internal class LogRecorder : ILogHandler
    {
        public List<(LogType type, string message)> Logs { get; } = new List<(LogType type, string message)>();

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            Logs.Add((logType, string.Format(format, args)));
        }

        public void LogException(Exception exception, Object context)
        {
            Logs.Add((LogType.Exception, exception.Message));
        }
    }
}