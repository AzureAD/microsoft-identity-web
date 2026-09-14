// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Configuration;
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
        public void ResolveConfigurationBasePath_BinMapPathFailureSkipsAutomaticJson(string? mappedPath)
        {
            using StringWriter traceOutput = new StringWriter();
            using TextWriterTraceListener listener = new TextWriterTraceListener(traceOutput);
            Trace.Listeners.Add(listener);

            try
            {
                string result = ResolveConfigurationBasePath(null, _ => mappedPath);
                Trace.Flush();

                Assert.Empty(result);
                Assert.Contains(
                    "Automatic JSON configuration was skipped",
                    traceOutput.ToString(),
                    StringComparison.Ordinal);
                Assert.Contains(
                    "Web.config and environment-variable settings remain available",
                    traceOutput.ToString(),
                    StringComparison.Ordinal);
                Assert.DoesNotContain(@"C:\site", traceOutput.ToString(), StringComparison.Ordinal);
            }
            finally
            {
                Trace.Listeners.Remove(listener);
            }
        }

        [Fact]
        public void ResolveConfigurationBasePath_LegacyMapPathFailureSkipsAutomaticJson()
        {
            using StringWriter traceOutput = new StringWriter();
            using TextWriterTraceListener listener = new TextWriterTraceListener(traceOutput);
            Trace.Listeners.Add(listener);

            try
            {
                string result = ResolveConfigurationBasePath("true", _ => null);
                Trace.Flush();

                Assert.Empty(result);
                Assert.Contains(
                    "Legacy JSON loading was requested by 'ida:UseLegacyWebRootAppSettings=true'",
                    traceOutput.ToString(),
                    StringComparison.Ordinal);
                Assert.Contains(
                    "Automatic JSON configuration was skipped",
                    traceOutput.ToString(),
                    StringComparison.Ordinal);
                Assert.Contains(
                    "disable the legacy setting",
                    traceOutput.ToString(),
                    StringComparison.Ordinal);
            }
            finally
            {
                Trace.Listeners.Remove(listener);
            }
        }

        [Theory]
        [InlineData("argument")]
        [InlineData("http")]
        [InlineData("invalid-operation")]
        public void ResolveConfigurationBasePath_SelectedMapPathFailureSkipsAutomaticJson(string scenario)
        {
            string result = ResolveConfigurationBasePath(
                null,
                _ => scenario switch
                {
                    "argument" => throw new ArgumentException("mapping unavailable"),
                    "http" => throw new System.Web.HttpException("mapping unavailable"),
                    _ => throw new InvalidOperationException("mapping unavailable"),
                });

            Assert.Empty(result);
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

        [Fact]
        public void AddDefaultJsonConfiguration_MissingBasePathPreservesExistingConfiguration()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AzureAd:ClientId"] = "web-config-client"
                });
            var factory = new TestOwinTokenAcquirerFactory();

            factory.AddDefaultJsonConfigurationForTest(builder, string.Empty);
            IConfiguration configuration = builder.Build();

            Assert.Equal("web-config-client", configuration["AzureAd:ClientId"]);
        }

        [Fact]
        public void AddDefaultJsonConfiguration_MissingDirectorySkipsAutomaticJson()
        {
            string missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            IConfigurationBuilder builder = new ConfigurationBuilder();
            var factory = new TestOwinTokenAcquirerFactory();

            factory.AddDefaultJsonConfigurationForTest(builder, missingDirectory);

            Assert.Empty(builder.Sources);
        }

        [Fact]
        public void AddDefaultJsonConfiguration_SelectedInvalidJsonStillFails()
        {
            string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                File.WriteAllText(Path.Combine(directory, "appsettings.json"), "{ invalid json");
                IConfigurationBuilder builder = new ConfigurationBuilder();
                var factory = new TestOwinTokenAcquirerFactory();

                factory.AddDefaultJsonConfigurationForTest(builder, directory);

                _ = Assert.Throws<FormatException>(() => builder.Build());
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private static string ResolveConfigurationBasePath(
            string? setting,
            Func<string, string?> mapPath,
            Func<string, bool>? fileExists = null)
        {
            return OwinTokenAcquirerFactory.ResolveConfigurationBasePath(
                setting,
                mapPath,
                fileExists ?? (_ => false),
                out _);
        }

        private sealed class TestOwinTokenAcquirerFactory : OwinTokenAcquirerFactory
        {
            public void AddDefaultJsonConfigurationForTest(
                IConfigurationBuilder builder,
                string basePath)
            {
                AddDefaultJsonConfiguration(builder, basePath);
            }
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
