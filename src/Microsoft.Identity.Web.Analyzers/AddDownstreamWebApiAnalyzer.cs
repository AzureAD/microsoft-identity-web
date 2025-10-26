// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Identity.Web.Analyzers;

/// <summary>
/// Analyzer that detects usage of obsolete AddDownstreamWebApi extension method.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AddDownstreamWebApiAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.AddDownstreamWebApiObsolete,
        title: "AddDownstreamWebApi is obsolete",
        messageFormat: "AddDownstreamWebApi is obsolete. Use AddDownstreamApi instead. See https://aka.ms/id-web-downstream-api-v2 for migration details.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "AddDownstreamWebApi has been replaced by AddDownstreamApi from Microsoft.Identity.Abstractions. Update your code to use the new API.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        
        if (methodSymbol is null)
        {
            return;
        }

        if (IsAddDownstreamWebApiMethod(methodSymbol))
        {
            var diagnostic = Diagnostic.Create(Rule, memberAccess.Name.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsAddDownstreamWebApiMethod(IMethodSymbol methodSymbol)
    {
        return methodSymbol.Name == "AddDownstreamWebApi" &&
               methodSymbol.ContainingType?.Name == "DownstreamWebApiExtensions" &&
               methodSymbol.ContainingNamespace?.ToDisplayString() == "Microsoft.Identity.Web";
    }
}
