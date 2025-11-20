// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.Identity.Web.Analyzers.Test;

/// <summary>
/// Base class for analyzer tests.
/// </summary>
public abstract class AnalyzerTestBase
{
    protected static async Task<Diagnostic[]> GetDiagnosticsAsync(DiagnosticAnalyzer analyzer, string source)
    {
        var compilation = CreateCompilation(source);
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None);
        return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
    }

    protected static void VerifyDiagnostic(Diagnostic diagnostic, string expectedId, int expectedLine, int expectedColumn)
    {
        Assert.Equal(expectedId, diagnostic.Id);
        var lineSpan = diagnostic.Location.GetLineSpan();
        Assert.Equal(expectedLine, lineSpan.StartLinePosition.Line + 1);
        Assert.Equal(expectedColumn, lineSpan.StartLinePosition.Character + 1);
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
        };

        // Add reference to netstandard if available
        var netstandardAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "netstandard");
        if (netstandardAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(netstandardAssembly.Location));
        }

        // Add System.Runtime reference
        var systemRuntime = typeof(System.Runtime.GCSettings).Assembly;
        references.Add(MetadataReference.CreateFromFile(systemRuntime.Location));

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
