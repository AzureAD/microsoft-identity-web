// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Identity.Web.Analyzers.Test;

/// <summary>
/// Tests for TokenAcquirerTokenCredentialAnalyzer.
/// </summary>
public class TokenAcquirerTokenCredentialAnalyzerTests : AnalyzerTestBase
{
    [Fact]
    public async Task DetectsTokenAcquirerTokenCredentialUsage()
    {
        // Arrange
        var source = @"
namespace Microsoft.Identity.Web
{
    public interface ITokenAcquirer { }
    
    public class TokenAcquirerTokenCredential
    {
        public TokenAcquirerTokenCredential(ITokenAcquirer tokenAcquirer) { }
    }
}

namespace TestNamespace
{
    using Microsoft.Identity.Web;
    
    public class TestClass
    {
        public void TestMethod()
        {
            var credential = new TokenAcquirerTokenCredential(null);
        }
    }
}";

        var analyzer = new TokenAcquirerTokenCredentialAnalyzer();

        // Act
        var diagnostics = await GetDiagnosticsAsync(analyzer, source);

        // Assert
        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticIds.TokenAcquirerTokenCredentialObsolete, diagnostics[0].Id);
        Assert.Contains("TokenAcquirerTokenCredential is obsolete", diagnostics[0].GetMessage(null), StringComparison.Ordinal);
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

        var analyzer = new TokenAcquirerTokenCredentialAnalyzer();

        // Act
        var diagnostics = await GetDiagnosticsAsync(analyzer, source);

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DetectsTokenAcquirerTokenCredentialTypeReference()
    {
        // Arrange
        var source = @"
namespace Microsoft.Identity.Web
{
    public interface ITokenAcquirer { }
    
    public class TokenAcquirerTokenCredential
    {
        public TokenAcquirerTokenCredential(ITokenAcquirer tokenAcquirer) { }
    }
}

namespace TestNamespace
{
    using Microsoft.Identity.Web;
    
    public class TestClass
    {
        private TokenAcquirerTokenCredential _credential;
        
        public void TestMethod(TokenAcquirerTokenCredential param)
        {
        }
    }
}";

        var analyzer = new TokenAcquirerTokenCredentialAnalyzer();

        // Act
        var diagnostics = await GetDiagnosticsAsync(analyzer, source);

        // Assert
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal(DiagnosticIds.TokenAcquirerTokenCredentialObsolete, d.Id));
    }
}
