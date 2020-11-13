// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Web.Perf.Client
{
    public class Logger
    {
        private static readonly string s_logsFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Logs");
        private static string s_exceptionsFile = s_logsFolder + $"\\{DateTime.Now:yyyyMMdd}.exceptions.log";
        private static string s_msalLogsFile = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalLogs.txt";

        private static StringBuilder s_msalLog = new StringBuilder();
        private static volatile bool s_isMsalLogging = false;

        private static object s_exceptionsLock = new object();
        private static object s_msalLogLock = new object();

        public static void PersistExceptions(StringBuilder stringBuilderContent)
        {
            if (stringBuilderContent.Length == 0)
            {
                return;
            }

            lock (s_exceptionsLock)
            {
                Directory.CreateDirectory(s_logsFolder);
                File.AppendAllText(s_exceptionsFile, stringBuilderContent.ToString());
            }
        }

        internal static void Log(LogLevel level, string message, bool containsPii)
        {
            StringBuilder tempBuilder = new StringBuilder();
            bool writeToDisk = false;
            lock (s_msalLogLock)
            {
                string logs = ($"{level} {message}");
                if (!s_isMsalLogging)
                {
                    s_isMsalLogging = true;
                    writeToDisk = true;
                    tempBuilder.Append(s_msalLog);
                    tempBuilder.Append(logs);
                    s_msalLog.Clear();
                }
                else
                {
                    s_msalLog.Append(logs);
                }
            }

            if (!writeToDisk)
            {
                return;
            }

            s_isMsalLogging = true;
            try
            {
                File.AppendAllText(s_msalLogsFile, tempBuilder.ToString());
                tempBuilder.Clear();
            }
            finally
            {
                s_isMsalLogging = false;
            }
        }
    }
}
