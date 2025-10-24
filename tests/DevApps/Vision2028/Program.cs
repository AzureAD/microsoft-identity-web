// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Vision2028;

string agentBluePrintAppId = "c4b2d4d9-9257-4c1a-a5c0-0a4907c83411";
string agentIdentity = "44250d7d-2362-4fba-9ba0-49c19ae270e0";
string tenantId = "31a58c3b-ae9c-4448-9e8f-e9e143e800df";

Mise mise = new();

// Agent blueprint credentials with SN/I Cert
Credential agentBlueprintCredentials = mise.NewCredential(new()
{
    ClientId = agentBluePrintAppId,
    TenantId = tenantId,

    // A lot of possiblilities here: see https://aka.ms/mise/client-credentials
    ClientCredentials = [
         CertificateDescription.FromStoreWithDistinguishedName(
                            "CN=LabAuth.MSIDLab.com", StoreLocation.LocalMachine, StoreName.My)
         ]
});

#if !AgentIdentities
// Token exchange to get FIC token for agent blueprint with Fmi path for agent identity (azp = AB)
Credential agentBlueprintFic = await mise.ExchangeCredentialAsync(agentBlueprintCredentials, new()
{
    FmiPath = agentIdentity
});

// Get the Agent identity FIC credentials
Credential agentIdentityCredentials = mise.NewCredential(agentBlueprintFic, new()
{
    ClientId = agentIdentity,
    TenantId = tenantId
});

// Token for Agent identity to call Graph
string tokenForAgentIdentityToCallGraph = await mise.GetToken(agentIdentityCredentials, new()
{
    Scopes = ["https://graph.microsoft.com/.default"]
});

// Or get authorization header for Agent identity to call Graph
var authorizationHeader = await mise.GetAuthorizationHeaderAsync(agentIdentityCredentials, new DownstreamApiOptions()
{
    Scopes = ["https://graph.microsoft.com/.default"]
});
#endif


#if !CrossCloudFic for GGC-M
Credential appInFairFaxCredentials = mise.NewCredential(new()
{
    Instance = "https://login.microsoftonline.us/",
    ClientId = "FairFax AppID",
    TenantId = "TenantId in public FairFax",

    // Not using client credentials. They are guessed from the platform.
    // For instance managed certificate of System Assigned Identity.
});


// Token exchange to get FIC token for FairFax App (tokne exchange determined automatically based on clouds instance)
Credential FicForAppInFairFax = await mise.ExchangeCredentialAsync(agentIdentityCredentials);

Credential CredentialInPublicCloud = mise.NewCredential(appInFairFaxCredentials, new()
{
    Instance = "https://login.microsoftonline.us/",
    ClientId = "Public cloud AppId",
    TenantId = "TenantId in public cloud"
});

var authorizationHeaderGccM = await mise.GetAuthorizationHeaderAsync(CredentialInPublicCloud, new DownstreamApiOptions()
{
    Scopes = ["https://graph.microsoft.com/.default"]
});

#endif
