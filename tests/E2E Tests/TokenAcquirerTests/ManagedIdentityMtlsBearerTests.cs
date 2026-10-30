// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.TestOnly;
using Xunit;

namespace TokenAcquirerTests
{
#if !FROM_GITHUB_ACTION
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    [Trait("Category", TestCategories.ManagedIdentity)]
    // Shares the IMDSv2/Credential Guard pool requirements of the managed identity FIC tests.
    [Trait("Category", TestCategories.ManagedIdentityFic)]
    public class ManagedIdentityMtlsBearerTests
    {
        [OnlyOnAzureDevopsFact]
        public Task RequestOverMtls_SystemAssigned_ReturnsUnboundBearerAsync()
            => AcquireBearerOverMtlsAsync(managedIdentityClientId: null);

        [OnlyOnAzureDevopsFact]
        public Task RequestOverMtls_UserAssigned_ReturnsUnboundBearerAsync()
            => AcquireBearerOverMtlsAsync(
                Environment.GetEnvironmentVariable("IDWEB_MI_UAMI_CLIENTID") ?? "6325cd32-9911-41f3-819c-416cdf9104e7");

        private static async Task AcquireBearerOverMtlsAsync(string? managedIdentityClientId)
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory factory = TokenAcquirerFactory.GetDefaultInstance();
            factory.Services.AddMicrosoftIdentityWebKeyAttestation();
            _ = factory.Build();

            ITokenAcquirer tokenAcquirer = factory.GetTokenAcquirer();
            var options = new AcquireTokenOptions
            {
                ManagedIdentity = new ManagedIdentityOptions { UserAssignedClientId = managedIdentityClientId },
                ExtraParameters = new Dictionary<string, object> { ["RequestOverMtls"] = true },
            };

            AcquireTokenResult result = await tokenAcquirer.GetTokenForAppAsync(
                "https://graph.microsoft.com/.default", options);

            Assert.False(string.IsNullOrEmpty(result.AccessToken));
            Assert.Equal("Bearer", result.TokenType);
            Assert.Null(result.BindingCertificate);
            Assert.Contains(
                "mtlsauth.microsoft.com",
                result.Metadata?.TokenEndpoint ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }
    }
#endif
}
