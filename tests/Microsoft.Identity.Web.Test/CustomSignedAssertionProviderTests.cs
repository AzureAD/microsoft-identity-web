// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Abstractions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class CustomSignedAssertionProviderTests
    {

        [Theory]
        [MemberData(nameof(CustomSignedAssertionTestData))]
        public async Task ProcessCustomSignedAssertionAsync_Tests(DefaultCredentialsLoader loader, CredentialDescription credentialDescription, Exception? expectedException = null)
        {
            try
            {
                await loader.LoadCredentialsIfNeededAsync(credentialDescription, null);
            }
            catch (Exception ex)
            {
                Assert.Equal(expectedException?.Message, ex.Message);
                return;
            }

            Assert.Null(expectedException);
        }

        public static IEnumerable<object[]> CustomSignedAssertionTestData()
        {
            // No source loaders
            yield return new object[]
            {
                new DefaultCredentialsLoader(NullLogger<DefaultCredentialsLoader>.Instance, new List<ICustomSignedAssertionProvider>()),
                new CredentialDescription {
                    CustomSignedAssertionProviderName = "Provider1",
                    SourceType = CredentialSource.CustomSignedAssertion
                },
                CustomSignedAssertionProviderNotFoundException.SourceLoadersNullOrEmpty()
            };

            // No provider name
            yield return new object[]
            {
                new DefaultCredentialsLoader(NullLogger<DefaultCredentialsLoader>.Instance, new List<ICustomSignedAssertionProvider> { new CustomSignedAssertionProvider("Provider1") }),
                new CredentialDescription
                {
                    CustomSignedAssertionProviderName = null,
                    SourceType = CredentialSource.CustomSignedAssertion
                },
                CustomSignedAssertionProviderNotFoundException.ProviderNameNullOrEmpty()
            };

            // Provider name not found
            yield return new object[]
            {
                new DefaultCredentialsLoader(NullLogger<DefaultCredentialsLoader>.Instance, new List<ICustomSignedAssertionProvider> { new CustomSignedAssertionProvider("OtherProvider") }),
                new CredentialDescription
                {
                    CustomSignedAssertionProviderName = "Provider2",
                    SourceType = CredentialSource.CustomSignedAssertion
                },
                CustomSignedAssertionProviderNotFoundException.ProviderNameNotFound("Provider2")
            };

            // Happy path
            yield return new object[]
            {
                new DefaultCredentialsLoader(NullLogger<DefaultCredentialsLoader>.Instance, new List<ICustomSignedAssertionProvider> { new CustomSignedAssertionProvider("Provider3") }),
                new CredentialDescription
                {
                    CustomSignedAssertionProviderName = "Provider3",
                    SourceType = CredentialSource.CustomSignedAssertion
                }
            };
        }

    }

    public class CustomSignedAssertionProvider : ICustomSignedAssertionProvider
    {
        public string Name { get; }

        public CredentialSource CredentialSource => CredentialSource.CustomSignedAssertion;

        public CustomSignedAssertionProvider(string name)
        {
            Name = name;
        }

        public Task LoadIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? parameters)
        {
            return Task.CompletedTask;
        }
    }
}
