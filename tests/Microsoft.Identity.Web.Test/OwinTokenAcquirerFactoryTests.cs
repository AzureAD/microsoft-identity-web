// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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

        private static string ResolveConfigurationBasePath(
            string? setting,
            Func<string, string?> mapPath)
        {
            return OwinTokenAcquirerFactory.ResolveConfigurationBasePath(setting, mapPath);
        }
    }
}
