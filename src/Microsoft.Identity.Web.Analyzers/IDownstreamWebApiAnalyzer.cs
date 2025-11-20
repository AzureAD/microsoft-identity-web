// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Identity.Web.Analyzers;

/// <summary>
/// Analyzer that detects usage of obsolete IDownstreamWebApi interface.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class IDownstreamWebApiAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.IDownstreamWebApiObsolete,
        title: "IDownstreamWebApi is obsolete",
        messageFormat: "IDownstreamWebApi is obsolete. Use IDownstreamApi instead. See https://aka.ms/id-web-downstream-api-v2 for migration details.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "IDownstreamWebApi has been replaced by IDownstreamApi from Microsoft.Identity.Abstractions. Update your code to use the new interface.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        
        context.RegisterSyntaxNodeAction(AnalyzeIdentifierName, SyntaxKind.IdentifierName);
    }

    private static void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context)
    {
        var identifierName = (IdentifierNameSyntax)context.Node;
        
        var symbolInfo = context.SemanticModel.GetSymbolInfo(identifierName, context.CancellationToken);
        
        if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol && IsIDownstreamWebApi(typeSymbol))
        {
            var diagnostic = Diagnostic.Create(Rule, identifierName.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsIDownstreamWebApi(ITypeSymbol typeSymbol)
    {
        return typeSymbol.Name == "IDownstreamWebApi" &&
               typeSymbol.ContainingNamespace?.ToDisplayString() == "Microsoft.Identity.Web";
    }
}
