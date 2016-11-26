﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using TypeForwardedToPathUtility = Microsoft.DocAsCode.Common.PathUtility;
    using TypeForwardedToStringExtension = Microsoft.DocAsCode.Common.StringExtension;

    public sealed class BuildMessageInfo
    {
        private Listener _listener;
        private readonly Dictionary<string, List<LogItem>> _logs;

        public BuildMessageInfo()
        {
            _logs = new Dictionary<string, List<LogItem>>();
        }

        private BuildMessageInfo(IDictionary<string, List<LogItem>> logs)
        {
            if (logs == null)
            {
                throw new ArgumentNullException(nameof(logs));
            }
            _logs = new Dictionary<string, List<LogItem>>(logs);
        }

        /// <summary>
        /// Get messages logged for file
        /// </summary>
        /// <param name="file">file path from working directory</param>
        /// <returns>logged messages</returns>
        public IEnumerable<ILogItem> GetMessages(string file)
        {
            List<LogItem> messages;
            if (_logs.TryGetValue(file, out messages))
            {
                return messages;
            }
            return Enumerable.Empty<ILogItem>();
        }

        public ILoggerListener GetListener()
        {
            if (_listener == null)
            {
                _listener = new Listener(this);
            }
            return _listener;
        }

        /// <summary>
        /// relay messages for file
        /// </summary>
        /// <param name="file">file path from working directory</param>
        public void Replay(string file)
        {
            foreach (var item in GetMessages(file))
            {
                Logger.Log(item);
            }
        }

        private void WriteLine(ILogItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (item.File == null)
            {
                return;
            }
            string fileFromWorkingDir = TypeForwardedToStringExtension.BackSlashToForwardSlash(item.File);
            if (!TypeForwardedToPathUtility.IsRelativePath(item.File))
            {
                fileFromWorkingDir = TypeForwardedToPathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, item.File);
            }
            List<LogItem> logsPerFile;
            if (!_logs.TryGetValue(fileFromWorkingDir, out logsPerFile))
            {
                logsPerFile = _logs[fileFromWorkingDir] = new List<LogItem>();
            }
            logsPerFile.Add(new LogItem
            {
                File = TypeForwardedToStringExtension.BackSlashToForwardSlash(item.File),
                Line = item.Line,
                LogLevel = item.LogLevel,
                Message = item.Message,
                Phase = item.Phase,
            });
        }

        public static BuildMessageInfo Load(TextReader reader)
        {
            var logs = JsonUtility.Deserialize<IDictionary<string, List<LogItem>>>(reader);
            return new BuildMessageInfo(logs);
        }

        public void Save(TextWriter writer)
        {
            JsonUtility.Serialize(writer, _logs);
        }

        private sealed class Listener : ILoggerListener
        {
            private readonly BuildMessageInfo _bmi;

            public Listener(BuildMessageInfo bmi)
            {
                if (bmi == null)
                {
                    throw new ArgumentNullException(nameof(bmi));
                }
                _bmi = bmi;
            }

            public void Dispose()
            {
            }

            public void Flush()
            {
            }

            public void WriteLine(ILogItem item)
            {
                if (item.LogLevel >= LogLevel.Warning)
                {
                    _bmi.WriteLine(item);
                }
            }
        }

        [Serializable]
        private sealed class LogItem : ILogItem
        {
            public string File { get; set; }

            public string Line { get; set; }

            public LogLevel LogLevel { get; set; }

            public string Message { get; set; }

            public string Phase { get; set; }
        }
    }
}