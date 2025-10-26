// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Identity.Web.Analyzers;

/// <summary>
/// Analyzer that detects usage of obsolete TokenAcquirerAppTokenCredential class.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TokenAcquirerAppTokenCredentialAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticIds.TokenAcquirerAppTokenCredentialObsolete,
        title: "TokenAcquirerAppTokenCredential is obsolete",
        messageFormat: "TokenAcquirerAppTokenCredential is obsolete. Use MicrosoftIdentityTokenCredential instead. See https://aka.ms/id-web-v4-migration for details.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "TokenAcquirerAppTokenCredential has been superseded by MicrosoftIdentityTokenCredential. Update your code to use the new credential type.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeIdentifierName, SyntaxKind.IdentifierName);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation.Type, context.CancellationToken);
        
        if (typeInfo.Type is null)
        {
            return;
        }

        if (IsTokenAcquirerAppTokenCredential(typeInfo.Type))
        {
            var diagnostic = Diagnostic.Create(Rule, objectCreation.Type.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context)
    {
        var identifierName = (IdentifierNameSyntax)context.Node;
        
        // Skip if this is part of an object creation (already handled)
        if (identifierName.Parent is ObjectCreationExpressionSyntax)
        {
            return;
        }

        var symbolInfo = context.SemanticModel.GetSymbolInfo(identifierName, context.CancellationToken);
        
        if (symbolInfo.Symbol is INamedTypeSymbol typeSymbol && IsTokenAcquirerAppTokenCredential(typeSymbol))
        {
            var diagnostic = Diagnostic.Create(Rule, identifierName.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsTokenAcquirerAppTokenCredential(ITypeSymbol typeSymbol)
    {
        return typeSymbol.Name == "TokenAcquirerAppTokenCredential" &&
               typeSymbol.ContainingNamespace?.ToDisplayString() == "Microsoft.Identity.Web";
    }
}
