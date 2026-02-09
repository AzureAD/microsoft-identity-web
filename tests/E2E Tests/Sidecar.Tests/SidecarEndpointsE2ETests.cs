// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !FROM_GITHUB_ACTION

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Sidecar.Models;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;
using Xunit;

namespace Sidecar.Tests;

public class SidecarEndpointsE2ETests : IClassFixture<SidecarApiFactory>
{
    private readonly SidecarApiFactory _factory;

    public SidecarEndpointsE2ETests(SidecarApiFactory factory) => _factory = factory;

    const string TenantId = "10c419d4-4a50-45b2-aa4e-919fb84df24f";         // Replace with your tenant ID
    const string AgentApplication = "aab5089d-e764-47e3-9f28-cc11c2513821"; // Replace with the actual agent application client ID
    const string TestClientApplication = "825940df-c1fb-4604-8104-02965f55b1ee"; // Replace with the client application used for app-only calls
    const string AgentIdentity = "ab18ca07-d139-4840-8b3b-4be9610c6ed5";    // Replace with the actual agent identity
    const string UserUpn = "agentuser1@id4slab1.onmicrosoft.com";       // Replace with the actual user upn.
    const string UserOid = "a02b9a5b-ea57-40c9-bf00-8aa631b549ad";           // Replace with the actual user OID.
    const string Instance = "https://login.microsoftonline.com/";     // Replace with the Entra ID authority instance
    const string UserReadScope = "user.read";                         // Replace with the scope used for user calls
    const string CertificateStorePath = "LocalMachine/My";            // Replace with the certificate store path
    const string CertificateDistinguishedName = "CN=LabAuth.MSIDLab.com"; // Replace with the certificate subject name
    static readonly string AgentApplicationScope = $"api://{AgentApplication}/.default"; // Replace with the API scope for the agent application

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
            $"/AuthorizationHeaderUnauthenticated/AgentUserIdentityCallsGraph?AgentIdentity={AgentIdentity}&AgentUsername={UserUpn}&OptionsOverride.Tenant={TenantId}&OptionsOverride.Scopes={UserReadScope}");

        Assert.True(result.IsSuccessStatusCode);

        var response = await result.Content.ReadFromJsonAsync<Microsoft.Identity.Web.Sidecar.Models.AuthorizationHeaderResult>();

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
            $"/AuthorizationHeaderUnauthenticated/AgentUserIdentityCallsGraph?AgentIdentity={AgentIdentity}&AgentUsername={UserUpn}&OptionsOverride.AcquireTokenOptions.Tenant=invalid-tenant&OptionsOverride.Scopes={UserReadScope}");

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }


    private static async Task<string> GetAuthorizationHeaderToCallTheSideCarAsync()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        services.AddSingleton<IConfiguration>(configuration);
        configuration["Instance"] = Instance;
        configuration["TenantId"] = TenantId;
        configuration["ClientId"] = TestClientApplication;
        configuration["SendX5C"] = "true";
        configuration["ClientCredentials:0:SourceType"] = "StoreWithDistinguishedName";
        configuration["ClientCredentials:0:CertificateStorePath"] = CertificateStorePath;
        configuration["ClientCredentials:0:CertificateDistinguishedName"] = CertificateDistinguishedName;

        services.AddTokenAcquisition().AddHttpClient().AddInMemoryTokenCaches();
        services.Configure<MicrosoftIdentityApplicationOptions>(configuration);
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        IAuthorizationHeaderProvider authorizationHeaderProvider = serviceProvider.GetRequiredService<IAuthorizationHeaderProvider>();
        string authorizationHeader = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(AgentApplicationScope,
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

#endif
