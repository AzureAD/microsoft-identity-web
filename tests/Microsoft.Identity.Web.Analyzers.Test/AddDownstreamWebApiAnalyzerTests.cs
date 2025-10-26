// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Identity.Web.Analyzers.Test;

/// <summary>
/// Tests for AddDownstreamWebApiAnalyzer.
/// </summary>
public class AddDownstreamWebApiAnalyzerTests : AnalyzerTestBase
{
    [Fact]
    public async Task DetectsAddDownstreamWebApiUsage()
    {
        // Arrange
        var source = @"
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Identity.Web
{
    public static class DownstreamWebApiExtensions
    {
        public static object AddDownstreamWebApi(this object builder, string serviceName, object configuration)
        {
            return builder;
        }
    }
}

namespace TestNamespace
{
    using Microsoft.Identity.Web;
    
    public class Startup
    {
        public void ConfigureServices(object services)
        {
            var builder = new object();
            builder.AddDownstreamWebApi(""MyApi"", null);
        }
    }
}";

        var analyzer = new AddDownstreamWebApiAnalyzer();

        // Act
        var diagnostics = await GetDiagnosticsAsync(analyzer, source);

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticIds.AddDownstreamWebApiObsolete, diagnostics[0].Id);
        Assert.Contains("AddDownstreamWebApi is obsolete", diagnostics[0].GetMessage(null), StringComparison.Ordinal);
    }

    [Fact]
    public async Task NoDiagnosticForUnrelatedMethods()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void SomeOtherMethod()
        {
            var x = 42;
        }
    }
}";

        var analyzer = new AddDownstreamWebApiAnalyzer();

        // Act
        var diagnostics = await GetDiagnosticsAsync(analyzer, source);

        // Assert
        Assert.Empty(diagnostics);
    }
}
