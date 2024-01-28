// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddTokenAcquisition_Sdk_AddsWithCorrectLifetime()
        {
            var services = new ServiceCollection();

            services.AddTokenAcquisition();
            ServiceDescriptor[] orderedServices = services.OrderBy(s => s.ServiceType.FullName).ToArray();

            Assert.Collection(
                orderedServices,
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IPostConfigureOptions<MicrosoftIdentityApplicationOptions>), actual.ServiceType);
                    Assert.Equal(typeof(MicrosoftIdentityApplicationOptionsMerger), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                }, actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IPostConfigureOptions<ConfidentialClientApplicationOptions>), actual.ServiceType);
                    Assert.Equal(typeof(ConfidentialClientApplicationOptionsMerger), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IPostConfigureOptions<MicrosoftIdentityOptions>), actual.ServiceType);
                    Assert.Equal(typeof(MicrosoftIdentityOptionsMerger), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Scoped, actual.Lifetime);
                    Assert.Equal(typeof(IAuthorizationHeaderProvider), actual.ServiceType);
                    Assert.Equal(typeof(DefaultAuthorizationHeaderProvider), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(typeof(ICredentialsLoader), actual.ServiceType);
                    Assert.Equal(typeof(DefaultCertificateLoader), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Scoped, actual.Lifetime);
                    Assert.Equal(typeof(ITokenAcquirerFactory), actual.ServiceType);
                    Assert.Equal(typeof(DefaultTokenAcquirerFactoryImplementation), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Singleton, actual.Lifetime);
                    Assert.Equal(typeof(IMergedOptionsStore), actual.ServiceType);
                    Assert.Equal(typeof(MergedOptionsStore), actual.ImplementationType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
               {
                   Assert.Equal(ServiceLifetime.Scoped, actual.Lifetime);
                   Assert.Equal(typeof(ITokenAcquisition), actual.ServiceType);
                   Assert.Equal(typeof(TokenAcquisitionAspNetCore), actual.ImplementationType);
                   Assert.Null(actual.ImplementationInstance);
                   Assert.Null(actual.ImplementationFactory);
               },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Scoped, actual.Lifetime);
                    Assert.Equal(typeof(ITokenAcquisitionHost), actual.ServiceType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.Null(actual.ImplementationFactory);
                },
                actual =>
                {
                    Assert.Equal(ServiceLifetime.Scoped, actual.Lifetime);
                    Assert.Equal(typeof(ITokenAcquisitionInternal), actual.ServiceType);
                    Assert.Null(actual.ImplementationInstance);
                    Assert.NotNull(actual.ImplementationFactory);
                });
        }

        [Fact]
        public void AddTokenAcquisition_AbleToOverrideICredentialsLoader()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ICredentialsLoader, MockCredentialsLoader>();

            services.AddTokenAcquisition();

            ServiceDescriptor[] orderedServices = services.OrderBy(s => s.ServiceType.FullName).ToArray();

            Assert.Single(orderedServices.Where(s => s.ServiceType == typeof(ICredentialsLoader)));
        }

        [Fact]
        public void AddHttpContextAccessor_ThrowsWithoutServices()
        {
            Assert.Throws<ArgumentNullException>("services", () => ServiceCollectionExtensions.AddTokenAcquisition(null!));
        }
    }

    internal class MockCredentialsLoader : ICredentialsLoader
    {
        public IDictionary<CredentialSource, ICredentialSourceLoader> CredentialSourceLoaders => throw new NotImplementedException();

        public Task LoadCredentialsIfNeededAsync(CredentialDescription credentialDescription, CredentialSourceLoaderParameters? parameters = null)
        {
            throw new NotImplementedException();
        }

        public Task<CredentialDescription?> LoadFirstValidCredentialsAsync(IEnumerable<CredentialDescription> credentialDescriptions, CredentialSourceLoaderParameters? parameters = null)
        {
            throw new NotImplementedException();
        }

        public void ResetCredentials(IEnumerable<CredentialDescription> credentialDescriptions)
        {
            throw new NotImplementedException();
        }
    }
}
