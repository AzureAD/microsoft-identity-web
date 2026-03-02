// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CrossPlatformValidation;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.LabInfrastructure;

Console.WriteLine("Hello, World!");

RequestValidator requestValidator = new();

requestValidator.Initialize("https://login.microsoftonline.com/organizations", "8837cde9-4029-4bfc-9259-e9e70ce670f7");
string authorizationHeader = AcquireTokenForLabUserAsync().Result.CreateAuthorizationHeader();
var result = requestValidator.Validate(authorizationHeader);
//string token = "Bearer ";
//IDictionary<string, object> claims = requestValidator.Validate(token);
//foreach (var claim in claims)
//{
//    Console.WriteLine(claim);
//}
Console.ReadLine();


static async Task<AuthenticationResult> AcquireTokenForLabUserAsync()
{
    string Organizations = "organizations";
    string OBOUser = "MSAL-User-Default@id4slab1.onmicrosoft.com";
    string OBOClientSideClientId = "9c0e534b-879c-4dce-b0e2-0e1be873ba14";
    string[] s_oBOApiScope = new string[] { "api://8837cde9-4029-4bfc-9259-e9e70ce670f7/.default" };

    var userConfig = await LabResponseHelper.GetUserConfigAsync("MSAL-User-Default-JSON");
    var msalPublicClient = PublicClientApplicationBuilder
       .Create(OBOClientSideClientId)
       .WithAuthority($"{userConfig.Authority}{userConfig.TenantId}", Organizations)
       .Build();

    AuthenticationResult authResult = await msalPublicClient
        .AcquireTokenByUsernamePassword(
        s_oBOApiScope,
        OBOUser,
        LabResponseHelper.FetchUserPassword(userConfig.LabName))
        .ExecuteAsync(CancellationToken.None)
        .ConfigureAwait(false);

    return authResult;
}
