// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
using Microsoft.Identity.Web;

namespace TestNamespace
{
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
        VerifyDiagnostic(diagnostics[0], DiagnosticIds.TokenAcquirerTokenCredentialObsolete, 10, 34);
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
}
