// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class BuiltInAssertionCredentialRecoveryTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AssertionFile_AppearsAfterFailure_LoaderRunsAgain(bool useSelection)
        {
            // Arrange
            string path = Path.Combine(Path.GetTempPath(), $"idweb-assertion-{Guid.NewGuid():N}.jwt");
            var credential = new CredentialDescription
            {
                SourceType = CredentialSource.SignedAssertionFilePath,
                SignedAssertionFileDiskPath = path,
            };
            var loader = new DefaultCredentialsLoader();
            var provider = new CredentialsProvider(NullLogger<CredentialsProvider>.Instance, loader, [], null);
            var options = new MergedOptions { ClientCredentials = [credential] };
            try
            {
                // Act
                if (useSelection)
                {
                    var exception = await Assert.ThrowsAsync<ArgumentException>(() => provider.GetCredentialAsync(options, null));
                    Assert.IsType<FileNotFoundException>(exception.InnerException);
                }
                else
                {
                    await Assert.ThrowsAsync<FileNotFoundException>(() => loader.LoadCredentialsIfNeededAsync(credential));
                }

                // Assert
                Assert.False(credential.Skip);
                Assert.Null(credential.CachedValue);

                string assertion = new JsonWebTokenHandler().CreateToken("{}");
                await File.WriteAllTextAsync(path, assertion);
                if (useSelection)
                {
                    Assert.Same(credential, await provider.GetCredentialAsync(options, null));
                }
                else
                {
                    await loader.LoadCredentialsIfNeededAsync(credential);
                }

                var cachedProvider = Assert.IsType<AzureIdentityForKubernetesClientAssertion>(credential.CachedValue);
                Assert.Equal(assertion, await cachedProvider.GetSignedAssertionAsync(null));
                await loader.LoadCredentialsIfNeededAsync(credential);
                Assert.Same(cachedProvider, credential.CachedValue);
                Assert.False(credential.Skip);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task CustomProvider_SkipPolicy_RemainsSupportedUntilReset()
        {
            // Arrange
            var customProvider = Substitute.For<ICustomSignedAssertionProvider>();
            customProvider.Name.Returns("ApplicationPolicy");
            customProvider.LoadIfNeededAsync(Arg.Any<CredentialDescription>(), Arg.Any<CredentialSourceLoaderParameters>())
                .Returns(args =>
                {
                    ((CredentialDescription)args[0]).Skip = true;
                    return Task.CompletedTask;
                });
            var loader = new DefaultCredentialsLoader(customSignedAssertionProviders: [customProvider], logger: null);
            var provider = new CredentialsProvider(NullLogger<CredentialsProvider>.Instance, loader, [], null);
            var custom = new CredentialDescription
            {
                SourceType = CredentialSource.CustomSignedAssertion,
                CustomSignedAssertionProviderName = "ApplicationPolicy",
            };
            var fallback = new CredentialDescription { SourceType = CredentialSource.ClientSecret, ClientSecret = "fallback" };
            var options = new MergedOptions { ClientCredentials = [custom, fallback] };

            // Act
            CredentialDescription? first = await provider.GetCredentialAsync(options, null);
            CredentialDescription? second = await provider.GetCredentialAsync(options, null);

            // Assert
            Assert.Same(fallback, first);
            Assert.Same(fallback, second);
            Assert.True(custom.Skip);
            await customProvider.Received(1).LoadIfNeededAsync(custom, null);

            customProvider.LoadIfNeededAsync(custom, null).Returns(args =>
            {
                ((CredentialDescription)args[0]).CachedValue = new object();
                return Task.CompletedTask;
            });
            loader.ResetCredentials([custom]);
            Assert.Same(custom, await provider.GetCredentialAsync(options, null));
            Assert.False(custom.Skip);
        }
    }
}
