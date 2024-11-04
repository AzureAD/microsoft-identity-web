// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class TestTokenAcquisitionHost
    {
        [Fact]
        public void TestTokenAcquisitionHostNotAspNetCore()
        {
            TokenAcquirerFactory.ResetDefaultInstance();

            // When not adding Services.AddAuthentication, the host should be 
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            var host = tokenAcquirerFactory.Services.First(s => s.ServiceType.FullName == "Microsoft.Identity.Web.ITokenAcquisitionHost");
            Assert.True(host.ImplementationType!.FullName == "Microsoft.Identity.Web.Hosts.DefaultTokenAcquisitionHost");
        }

        [Fact]
        public void TestTokenAcquisitionAspNetCoreBuilderNoAuth()
        {
            // When not adding Services.AddAuthentication, the host should be 
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddTokenAcquisition();
            var host = builder.Services.First(s => s.ServiceType.FullName == "Microsoft.Identity.Web.ITokenAcquisitionHost");
            Assert.True(host.ImplementationType!.FullName == "Microsoft.Identity.Web.Hosts.DefaultTokenAcquisitionHost");
        }


        [Fact]
        public void TestTokenAcquisitionAspNetCoreBuilderAuth()
        {
            // When not adding Services.AddAuthentication, the host should be 
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddAuthentication()
                .AddMicrosoftIdentityWebApi(builder.Configuration)
                 .EnableTokenAcquisitionToCallDownstreamApi();

            var host = builder.Services.First(s => s.ServiceType.FullName == "Microsoft.Identity.Web.ITokenAcquisitionHost");
            Assert.True(host.ImplementationType!.FullName == "Microsoft.Identity.Web.TokenAcquisitionAspnetCoreHost");
        }
    }
}
