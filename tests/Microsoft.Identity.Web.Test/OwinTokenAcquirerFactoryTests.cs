// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using Microsoft.Identity.Web.OWIN;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class OwinTokenAcquirerFactoryTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("false")]
        [InlineData("False")]
        public void ResolveConfigurationBasePath_MissingOrFalseSettingUsesBin(string? setting)
        {
            string? mappedVirtualPath = null;

            string result = ResolveConfigurationBasePath(
                setting,
                virtualPath =>
                {
                    mappedVirtualPath = virtualPath;
                    return @"C:\site\bin";
                });

            Assert.Equal("~/bin", mappedVirtualPath);
            Assert.Equal(@"C:\site\bin", result);
        }

        [Fact]
        public void ResolveConfigurationBasePath_TrueSettingUsesRootAndWarns()
        {
            using StringWriter traceOutput = new StringWriter();
            using TextWriterTraceListener listener = new TextWriterTraceListener(traceOutput);
            Trace.Listeners.Add(listener);

            try
            {
                string? mappedVirtualPath = null;

                string result = ResolveConfigurationBasePath(
                    "TRUE",
                    virtualPath =>
                    {
                        mappedVirtualPath = virtualPath;
                        return @"C:\site";
                    });
                Trace.Flush();

                Assert.Equal("~/", mappedVirtualPath);
                Assert.Equal(@"C:\site", result);
                Assert.Contains(
                    "'ida:UseLegacyWebRootAppSettings' compatibility setting is enabled",
                    traceOutput.ToString(),
                    StringComparison.Ordinal);
            }
            finally
            {
                Trace.Listeners.Remove(listener);
            }
        }

        [Fact]
        public void ResolveConfigurationBasePath_InvalidSettingUsesBinAndWarns()
        {
            using StringWriter traceOutput = new StringWriter();
            using TextWriterTraceListener listener = new TextWriterTraceListener(traceOutput);
            Trace.Listeners.Add(listener);

            try
            {
                string? mappedVirtualPath = null;

                string result = ResolveConfigurationBasePath(
                    "not-a-boolean",
                    virtualPath =>
                    {
                        mappedVirtualPath = virtualPath;
                        return @"C:\site\bin";
                    });
                Trace.Flush();

                Assert.Equal("~/bin", mappedVirtualPath);
                Assert.Equal(@"C:\site\bin", result);
                Assert.Contains(
                    "'ida:UseLegacyWebRootAppSettings' appSetting must be 'true' or 'false'",
                    traceOutput.ToString(),
                    StringComparison.Ordinal);
            }
            finally
            {
                Trace.Listeners.Remove(listener);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void ResolveConfigurationBasePath_BinMapPathFailureThrows(string? mappedPath)
        {
            ConfigurationErrorsException exception = Assert.Throws<ConfigurationErrorsException>(
                () => ResolveConfigurationBasePath(null, _ => mappedPath));

            Assert.Contains("'~/bin'", exception.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ResolveConfigurationBasePath_RootFilePresenceControlsWarning(bool rootFileExists)
        {
            using StringWriter traceOutput = new StringWriter();
            using TextWriterTraceListener listener = new TextWriterTraceListener(traceOutput);
            Trace.Listeners.Add(listener);

            try
            {
                var mappedVirtualPaths = new List<string>();

                string result = ResolveConfigurationBasePath(
                    null,
                    virtualPath =>
                    {
                        mappedVirtualPaths.Add(virtualPath);
                        return virtualPath == "~/appsettings.json"
                            ? @"C:\site\appsettings.json"
                            : @"C:\site\bin";
                    },
                    path =>
                    {
                        Assert.Equal(@"C:\site\appsettings.json", path);
                        return rootFileExists;
                    });
                Trace.Flush();

                Assert.Equal(@"C:\site\bin", result);
                Assert.Equal(new[] { "~/appsettings.json", "~/bin" }, mappedVirtualPaths);
                Assert.Equal(
                    rootFileExists ? 1 : 0,
                    CountOccurrences(traceOutput.ToString(), "was detected in the application root"));
                Assert.DoesNotContain(@"C:\site", traceOutput.ToString(), StringComparison.Ordinal);
            }
            finally
            {
                Trace.Listeners.Remove(listener);
            }
        }

        [Theory]
        [InlineData("missing")]
        [InlineData("mapping-throws")]
        [InlineData("probe-throws")]
        public void ResolveConfigurationBasePath_UnavailableRootProbeDoesNotFailStartup(string scenario)
        {
            string result = ResolveConfigurationBasePath(
                null,
                virtualPath =>
                {
                    if (virtualPath == "~/appsettings.json")
                    {
                        if (scenario == "mapping-throws")
                        {
                            throw new InvalidOperationException("host unavailable");
                        }

                        return scenario == "missing" ? null : @"C:\site\appsettings.json";
                    }

                    return @"C:\site\bin";
                },
                _ => scenario == "probe-throws"
                    ? throw new UnauthorizedAccessException("probe unavailable")
                    : false);

            Assert.Equal(@"C:\site\bin", result);
        }

        private static string ResolveConfigurationBasePath(
            string? setting,
            Func<string, string?> mapPath,
            Func<string, bool>? fileExists = null)
        {
            return OwinTokenAcquirerFactory.ResolveConfigurationBasePath(
                setting,
                mapPath,
                fileExists ?? (_ => false));
        }

        private static int CountOccurrences(string value, string searchValue)
        {
            int count = 0;
            int index = 0;

            while ((index = value.IndexOf(searchValue, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += searchValue.Length;
            }

            return count;
        }
    }
}
