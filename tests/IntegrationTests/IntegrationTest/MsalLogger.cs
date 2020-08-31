// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace IntegrationTest
{
    /// <summary>
    /// MSAL Logger class to bridge <see cref="ILogger"/> to MSAL Logging.
    /// </summary>
    public class MsalLogger
    {
        private static StreamWriter loggingFile = new StreamWriter(new FileStream("MsalLogs.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = true,
        };

        private readonly ILogger<IConfidentialClientApplication> _logger;

        public MsalLogger(ILogger<IConfidentialClientApplication> logger)
        {
            _logger = logger;
        }

        public void Log(
            Microsoft.Identity.Client.LogLevel level,
            string message)
        {
            switch (level)
            {
                case Microsoft.Identity.Client.LogLevel.Verbose:
                    _logger.LogDebug(message);
                    return;
                case Microsoft.Identity.Client.LogLevel.Error:
                    _logger.LogError(message);
                    loggingFile.WriteLine("Error: " + message);
                    return;
                case Microsoft.Identity.Client.LogLevel.Info:
                    _logger.LogInformation(message);
                    loggingFile.WriteLine("Info: " + message);
                    return;
                case Microsoft.Identity.Client.LogLevel.Warning:
                    _logger.LogWarning(message);
                    loggingFile.WriteLine("Warning: " + message);
                    return;
            }
        }
    }
}
