// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class TestTokenAcquisitionHost
    {
        [Fact]
        public void TestTokenAcquisitionHostNotAspNetCore()
        {
            // When not adding Services.AddAuthentication, the host should be 
            TokenAcquirerFactory tokenAcquirerFactory = TokenAcquirerFactory.GetDefaultInstance();
            var host = tokenAcquirerFactory.Services.First(s => s.ServiceType.Name == "Microsoft.Identity.Web.ITokenAcquisitionHost");
            Assert.True(host.ImplementationType!.FullName == "Microsoft.Identity.Web.Hosts.DefaultTokenAcquisitionHost");
        }

        [Fact]
        public void TestTokenAcquisitionAspNetCoreBuilderNoAuth()
        {
            // When not adding Services.AddAuthentication, the host should be 
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddTokenAcquisition();
            var host = builder.Services.First(s => s.ServiceType.Name == "ITokenAcquisitionHost");
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

            var host = builder.Services.First(s => s.ServiceType.Name == "ITokenAcquisitionHost");
            Assert.True(host.ImplementationType!.FullName == "Microsoft.Identity.Web.TokenAcquisitionAspnetCoreHost");
        }
    }
}
