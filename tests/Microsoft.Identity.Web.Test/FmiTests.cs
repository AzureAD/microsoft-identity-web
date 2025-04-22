// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Test.Common.Mocks;
using Microsoft.Identity.Web.TestOnly;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class FmiTests
    {
        [Fact]
        public async Task FmiPathSentToEndpointTest()
        {
            await RunHappyPathTestAsync();
        }

        private async Task RunHappyPathTestAsync()
        {
            // Arrange
            var tokenAcquirerFactory = InitTokenAcquirerFactoryForFmi();
            IServiceProvider serviceProvider = tokenAcquirerFactory.Build();
            var mockHttpClient = serviceProvider.GetRequiredService<IMsalHttpClientFactory>() as MockHttpClientFactory;

            mockHttpClient?.AddMockHandler(MockHttpCreator.CreateHandlerToValidatePostData(
                System.Net.Http.HttpMethod.Post,
                new Dictionary<string, string>() { { "fmi_path", "somePath" } }));

            IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();

            var options = new Microsoft.Identity.Abstractions.AcquireTokenOptions() { FmiPath = "somePath" };

            // Act
            string result = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("https://graph.microsoft.com/.default",
                new AuthorizationHeaderProviderOptions() { AcquireTokenOptions = options });

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Bearer header.payload.signature", result);
        }

        private TokenAcquirerFactory InitTokenAcquirerFactoryForFmi()
        {
            TokenAcquirerFactoryTesting.ResetTokenAcquirerFactoryInTest();
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            tokenAcquirerFactory.Services.Configure<MicrosoftIdentityApplicationOptions>(options =>
            {
                options.Instance = "https://login.microsoftonline.com/";
                options.TenantId = "f645ad92-e38d-4d1a-b510-d1b09a74a8ca";
                options.ClientId = "urn:microsoft:identity:fmi";
                options.ExtraQueryParameters = new Dictionary<string, string>
                        {
                                { "dc", "ESTS-PUB-SCUS-LZ1-FD000-TEST1" }
                        };
                options.ClientCredentials = [ new CredentialDescription() {
                    SourceType = CredentialSource.ClientSecret,
                    ClientSecret = "someSecret"
                    }];
            });

            // Add MockedHttpClientFactory
            tokenAcquirerFactory.Services.AddSingleton<IMsalHttpClientFactory, MockHttpClientFactory>();

            return tokenAcquirerFactory;
        }
    }
}
