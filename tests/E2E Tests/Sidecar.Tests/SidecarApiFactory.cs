// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Sidecar;

namespace Sidecar.Tests;

public class SidecarApiFactory : WebApplicationFactory<Program>
{
    readonly Action<IConfigurationBuilder> _configureOptions;

    public SidecarApiFactory() : this(null)
    {
    }

    internal SidecarApiFactory(Action<IConfigurationBuilder>? configureOptions)
    {
        _configureOptions = configureOptions ?? (builder =>
        {
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AzureAd:Instance", "https://login.microsoftonline.com/" },
                { "AzureAd:TenantId", "31a58c3b-ae9c-4448-9e8f-e9e143e800df" },
                { "AzureAd:ClientId", "d05619c9-dbf2-4e60-95fd-cc75dd0db451" },
                { "AzureAd:Audience", "d05619c9-dbf2-4e60-95fd-cc75dd0db451" },
                { "AzureAd:ClientCredentials:0:SourceType", "StoreWithDistinguishedName" },
                { "AzureAd:ClientCredentials:0:CertificateStorePath", "LocalMachine/My" },
                { "AzureAd:ClientCredentials:0:CertificateDistinguishedName", "CN=LabAuth.MSIDLab.com" }, // Replace with the subject name of your certificate
                { "DownstreamApis:MsGraph:BaseUrl", "https://graph.microsoft.com/v1.0/" },
                { "DownstreamApis:MsGraph:RelativePath", "/me" },
                { "DownstreamApis:MsGraph:Scopes:0", "User.Read" }
            });
        });
    }


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(_configureOptions);
        builder.ConfigureServices(services =>
        {
            // Given we add the Json file after the initial configuration, and that
            // downstream APIs are added to a IOptions, we need to re-add the downstream APIs
            // with the new config
            IConfiguration? configuration = services!
            .First(s => s.ServiceType == typeof(IConfiguration))
            ?.ImplementationFactory
            ?.Invoke(null!) as IConfiguration;

            services!.AddDownstreamApis(configuration!.GetSection("DownstreamApis"));
        });
    }
}
