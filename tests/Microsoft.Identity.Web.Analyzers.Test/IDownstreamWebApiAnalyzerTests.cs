// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Identity.Web.Analyzers.Test;

/// <summary>
/// Tests for IDownstreamWebApiAnalyzer.
/// </summary>
public class IDownstreamWebApiAnalyzerTests : AnalyzerTestBase
{
    [Fact]
    public async Task DetectsIDownstreamWebApiUsage()
    {
        // Arrange
        var source = @"
namespace Microsoft.Identity.Web
{
    public interface IDownstreamWebApi
    {
    }
}

namespace TestNamespace
{
    using Microsoft.Identity.Web;
    
    public class MyController
    {
        private readonly IDownstreamWebApi _api;
        
        public MyController(IDownstreamWebApi api)
        {
            _api = api;
        }
    }
}";

        var analyzer = new IDownstreamWebApiAnalyzer();

        // Act
        var diagnostics = await GetDiagnosticsAsync(analyzer, source);

        // Assert
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal(DiagnosticIds.IDownstreamWebApiObsolete, d.Id));
        Assert.All(diagnostics, d => Assert.Contains("IDownstreamWebApi is obsolete", d.GetMessage(null), StringComparison.Ordinal));
    }

    [Fact]
    public async Task NoDiagnosticForUnrelatedInterfaces()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public interface ISomeOtherInterface
    {
    }
    
    public class TestClass
    {
        private readonly ISomeOtherInterface _service;
    }
}";

        var analyzer = new IDownstreamWebApiAnalyzer();

        // Act
        var diagnostics = await GetDiagnosticsAsync(analyzer, source);

        // Assert
        Assert.Empty(diagnostics);
    }
}
