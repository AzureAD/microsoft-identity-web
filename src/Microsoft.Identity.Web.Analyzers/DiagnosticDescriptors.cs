// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace Microsoft.Identity.Web.Analyzers
{
    /// <summary>
    /// Diagnostic descriptors for Microsoft Identity Web analyzers.
    /// </summary>
    internal static class DiagnosticDescriptors
    {
        private const string Category = "Usage";
        private const string HelpLinkUri = "https://aka.ms/ms-id-web/v3-to-v4";

        /// <summary>
        /// IDW4001: Use MicrosoftIdentityTokenCredential instead of TokenAcquirerTokenCredential.
        /// </summary>
        public static readonly DiagnosticDescriptor IDW4001 = new DiagnosticDescriptor(
            id: "IDW4001",
            title: "Use MicrosoftIdentityTokenCredential instead of TokenAcquirerTokenCredential",
            messageFormat: "'{0}' is obsolete. Use 'MicrosoftIdentityTokenCredential' instead.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLinkUri);

        /// <summary>
        /// IDW4002: Use MicrosoftIdentityTokenCredential with RequestAppToken instead of TokenAcquirerAppTokenCredential.
        /// </summary>
        public static readonly DiagnosticDescriptor IDW4002 = new DiagnosticDescriptor(
            id: "IDW4002",
            title: "Use MicrosoftIdentityTokenCredential with RequestAppToken instead of TokenAcquirerAppTokenCredential",
            messageFormat: "'{0}' is obsolete. Use 'MicrosoftIdentityTokenCredential' with Options.RequestAppToken = true instead.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLinkUri);

        /// <summary>
        /// IDW4003: Use AddDownstreamApi instead of AddDownstreamWebApi.
        /// </summary>
        public static readonly DiagnosticDescriptor IDW4003 = new DiagnosticDescriptor(
            id: "IDW4003",
            title: "Use AddDownstreamApi instead of AddDownstreamWebApi",
            messageFormat: "'{0}' is obsolete. Use 'AddDownstreamApi' instead.",
            category: "Naming",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLinkUri);
    }
}