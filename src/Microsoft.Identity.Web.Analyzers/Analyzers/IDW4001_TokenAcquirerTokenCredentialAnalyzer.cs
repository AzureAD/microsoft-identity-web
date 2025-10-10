// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Identity.Web.Analyzers
{
    /// <summary>
    /// Analyzer for IDW4001: Detects usage of obsolete TokenAcquirerTokenCredential.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class IDW4001_TokenAcquirerTokenCredentialAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => 
            ImmutableArray.Create(DiagnosticDescriptors.IDW4001);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Register syntax node actions to detect TokenAcquirerTokenCredential usage
            // This is a stub implementation for Phase 1 scaffolding
        }
    }
}