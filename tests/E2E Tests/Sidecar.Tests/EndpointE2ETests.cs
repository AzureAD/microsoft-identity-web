// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Sidecar;
using Microsoft.Identity.Web.Sidecar.Models;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;

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
            builder.AddJsonFile(
                path: Path.Combine(Directory.GetCurrentDirectory().ToString(), "appsettings.agentids.json"),
                optional: false,
                reloadOnChange: true);
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

public class EndpointsE2ETests : IClassFixture<SidecarApiFactory>
{
    private readonly SidecarApiFactory _factory;

    public EndpointsE2ETests(SidecarApiFactory factory) => _factory = factory;

    const string TenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";         // Replace with your tenant ID
    const string AgentApplication = "d15884b6-a447-4dd5-a5a5-a668c49f6300"; // Replace with the actual agent application client ID
    const string AgentIdentity = "d84da24a-2ea2-42b8-b5ab-8637ec208024";    // Replace with the actual agent identity
    const string UserUpn = "aui1@msidlabtoint.onmicrosoft.com";             // Replace with the actual user upn.
    string UserOid = "51c1aa1c-f6d0-4a92-936c-cadb27b717f2";           // Replace with the actual user OID.

    [Fact]
    public async Task Validate_WhenBadTokenAsync()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "dummy-token");
        var response = await client.GetAsync("/Validate");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid_token", response.Headers.WwwAuthenticate.ToString(), StringComparison.CurrentCultureIgnoreCase);
    }

    [Fact]
    public async Task Validate_WhenGoodTokenAsync()
    {
        // Getting a token to call the API.
        string authorizationHeader = await GetAuthorizationHeaderToCallTheSideCarAsync();

        // Calling the API
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);
        var response = await client.GetAsync("/Validate");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();

        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task GetAuthorizationHeaderForAgentUserIdentityAuthenticatedAsync()
    {
        // Getting a token to call the API.
        string authorizationHeader = await GetAuthorizationHeaderToCallTheSideCarAsync();

        // Calling the API
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);
        var response = await client.GetAsync($"/AuthorizationHeader/MsGraph?agentidentity={AgentIdentity}&agentUsername={UserUpn}");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DownstreamApiForAgentUserIdentityAuthenticated()
    {
        // Getting a token to call the API.
        string authorizationHeader = await GetAuthorizationHeaderToCallTheSideCarAsync();

        // Calling the API
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);
        var response = await client.GetAsync($"/AuthorizationHeader/MsGraph?agentidentity={AgentIdentity}&agentUsername={UserUpn}");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DownstreamApiForAgentUserIdentityAuthenticatedUsingOid()
    {
        // Getting a token to call the API.
        string authorizationHeader = await GetAuthorizationHeaderToCallTheSideCarAsync();

        // Calling the API
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);
        var response = await client.GetAsync($"/AuthorizationHeader/MsGraph?agentidentity={AgentIdentity}&agentUserId={UserOid}");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAuthorizationHeaderForAgentUserIdentityUnauthenticatedAsync()
    {
        // Calling the API
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/AuthorizationHeaderUnauthenticated/MsGraph?agentidentity={AgentIdentity}&agentUsername={UserUpn}");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAuthorizationHeaderForAgentUserIdentityUnauthenticatedAsyncUseUpn()
    {
        // Calling the API
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/AuthorizationHeaderUnauthenticated/MsGraph?agentidentity={AgentIdentity}&agentUserId={UserOid}");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAuthorizationHeaderForAgentUserIdentityUnauthenticated_WithOptionsOverride()
    {
        var client = _factory.CreateClient();

        var result = await client.GetAsync(
            $"/AuthorizationHeaderUnauthenticated/AgentUserIdentityCallsGraph?AgentIdentity={AgentIdentity}&AgentUsername={UserUpn}&OptionsOverride.Tenant={TenantId}&OptionsOverride.Scopes=user.read");

        Assert.True(result.IsSuccessStatusCode);

        var response = await result.Content.ReadFromJsonAsync<AuthorizationHeaderResult>();

        Assert.NotNull(response?.AuthorizationHeader);
        Assert.StartsWith("Bearer ey", response.AuthorizationHeader, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDownstreamApiForAgentUserIdentityUnauthenticated()
    {
        // Calling the API
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/DownstreamApi/MsGraph?agentidentity={AgentIdentity}&agentUsername={UserUpn}", null);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDownstreamApiForAgentUserIdentityUnauthenticatedUseOid()
    {
        // Calling the API
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/DownstreamApi/MsGraph?agentidentity={AgentIdentity}&agentUserId={UserOid}", null);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TestAgentIdentityConfiguration_InvalidTenant()
    {
        var client = _factory.CreateClient();

        var result = await client.GetAsync(
            $"/AuthorizationHeaderUnauthenticated/AgentUserIdentityCallsGraph?AgentIdentity={AgentIdentity}&AgentUsername={UserUpn}&OptionsOverride.AcquireTokenOptions.Tenant=invalid-tenant&OptionsOverride.Scopes=user.read");

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }


    private static async Task<string> GetAuthorizationHeaderToCallTheSideCarAsync()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        services.AddSingleton<IConfiguration>(configuration);
        configuration["Instance"] = "https://login.microsoftonline.com/";
        configuration["TenantId"] = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";
        configuration["ClientId"] = "5cbcd9ff-c994-49ac-87e7-08a93a9c0794";
        configuration["SendX5C"] = "true";
        configuration["ClientCredentials:0:SourceType"] = "StoreWithDistinguishedName";
        configuration["ClientCredentials:0:CertificateStorePath"] = "LocalMachine/My";
        configuration["ClientCredentials:0:CertificateDistinguishedName"] = "CN=LabAuth.MSIDLab.com";

        services.AddTokenAcquisition().AddHttpClient().AddInMemoryTokenCaches();
        services.Configure<MicrosoftIdentityApplicationOptions>(configuration);
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
        string authorizationHeader = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync("api://d15884b6-a447-4dd5-a5a5-a668c49f6300/.default",
            new AuthorizationHeaderProviderOptions()
            {
                AcquireTokenOptions = new AcquireTokenOptions()
                {
                    AuthenticationOptionsName = ""
                }
            });
        return authorizationHeader;
    }
}
