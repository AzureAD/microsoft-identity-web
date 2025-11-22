// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.TestOnly;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;

namespace TokenAcquirerTests
{
    /// <summary>
    /// E2E tests for authority matrix scenarios.
    /// Issue #3610: E2E tests for complex authority scenarios.
    /// These tests validate real token acquisition with various authority configurations.
    /// </summary>
#if !FROM_GITHUB_ACTION
    public partial class TokenAcquirer
    {
        [IgnoreOnAzureDevopsFact]
        public async Task AcquireToken_AuthorityOnly_AAD_NoV2Suffix_Succeeds()
        {
            // Issue #3610: AAD authority without /v2.0 suffix should work
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityApplicationOptions>(s_optionName, option =>
            {
                option.Authority = "https://login.microsoftonline.com/msidlab4.onmicrosoft.com"; // No /v2.0 suffix
                option.ClientId = "f6b698c0-140c-448f-8155-4aa9bf77ceba";
                option.ClientCredentials = s_clientCredentials;
            });

            // Act & Assert
            await CreateGraphClientAndAssertAsync(tokenAcquirerFactory, services);
        }

        [IgnoreOnAzureDevopsFact]
        public async Task AcquireToken_AuthorityOnly_AAD_WithV2Suffix_Succeeds()
        {
            // Issue #3610: AAD authority with /v2.0 suffix should work
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityApplicationOptions>(s_optionName, option =>
            {
                option.Authority = "https://login.microsoftonline.com/msidlab4.onmicrosoft.com/v2.0";
                option.ClientId = "f6b698c0-140c-448f-8155-4aa9bf77ceba";
                option.ClientCredentials = s_clientCredentials;
            });

            // Act & Assert
            await CreateGraphClientAndAssertAsync(tokenAcquirerFactory, services);
        }

        [IgnoreOnAzureDevopsFact]
        public async Task AcquireToken_AuthorityOnly_AAD_CommonTenant_Succeeds()
        {
            // Issue #3610: AAD authority with common tenant should work
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityApplicationOptions>(s_optionName, option =>
            {
                option.Authority = "https://login.microsoftonline.com/common/v2.0";
                option.ClientId = "f6b698c0-140c-448f-8155-4aa9bf77ceba";
                option.ClientCredentials = s_clientCredentials;
            });

            // Act & Assert
            await CreateGraphClientAndAssertAsync(tokenAcquirerFactory, services);
        }

        [IgnoreOnAzureDevopsFact]
        public async Task AcquireToken_InstanceAndTenantId_AAD_Succeeds()
        {
            // Issue #3610: Instance + TenantId configuration should work
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityApplicationOptions>(s_optionName, option =>
            {
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidlab4.onmicrosoft.com";
                option.ClientId = "f6b698c0-140c-448f-8155-4aa9bf77ceba";
                option.ClientCredentials = s_clientCredentials;
            });

            // Act & Assert
            await CreateGraphClientAndAssertAsync(tokenAcquirerFactory, services);
        }

        [IgnoreOnAzureDevopsFact]
        public async Task AcquireToken_ConflictConfig_AAD_AuthorityIgnored_Succeeds()
        {
            // Issue #3610: When both Authority and Instance are set, Instance takes precedence
            // Arrange
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            IServiceCollection services = tokenAcquirerFactory.Services;

            services.Configure<MicrosoftIdentityApplicationOptions>(s_optionName, option =>
            {
                option.Authority = "https://login.microsoftonline.com/common/v2.0"; // Will be ignored
                option.Instance = "https://login.microsoftonline.com/";
                option.TenantId = "msidlab4.onmicrosoft.com";
                option.ClientId = "f6b698c0-140c-448f-8155-4aa9bf77ceba";
                option.ClientCredentials = s_clientCredentials;
            });

            // Act & Assert - Should use Instance+TenantId, ignore Authority
            await CreateGraphClientAndAssertAsync(tokenAcquirerFactory, services);
        }
    }
#else
    public partial class TokenAcquirer
    {
    }
#endif
}
