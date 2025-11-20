// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Identity.Web.Analyzers.Test;

/// <summary>
/// Tests for TokenAcquirerAppTokenCredentialAnalyzer.
/// </summary>
public class TokenAcquirerAppTokenCredentialAnalyzerTests : AnalyzerTestBase
{
    [Fact]
    public async Task DetectsTokenAcquirerAppTokenCredentialUsage()
    {
        // Arrange
        var source = @"
namespace Microsoft.Identity.Web
{
    public interface ITokenAcquirer { }
    
    public class TokenAcquirerAppTokenCredential
    {
        public TokenAcquirerAppTokenCredential(ITokenAcquirer tokenAcquirer) { }
    }
}

namespace TestNamespace
{
    using Microsoft.Identity.Web;
    
    public class TestClass
    {
        public void TestMethod()
        {
            var credential = new TokenAcquirerAppTokenCredential(null);
        }
    }
}";

        var analyzer = new TokenAcquirerAppTokenCredentialAnalyzer();

        // Act
        var diagnostics = await GetDiagnosticsAsync(analyzer, source);

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticIds.TokenAcquirerAppTokenCredentialObsolete, diagnostics[0].Id);
        Assert.Contains("TokenAcquirerAppTokenCredential is obsolete", diagnostics[0].GetMessage(null), StringComparison.Ordinal);
    }

    [Fact]
    public async Task NoDiagnosticForUnrelatedCode()
    {
        // Arrange
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var x = 42;
        }
    }
}";

        var analyzer = new TokenAcquirerAppTokenCredentialAnalyzer();

        // Act
        var diagnostics = await GetDiagnosticsAsync(analyzer, source);

        // Assert
        Assert.Empty(diagnostics);
    }
}
