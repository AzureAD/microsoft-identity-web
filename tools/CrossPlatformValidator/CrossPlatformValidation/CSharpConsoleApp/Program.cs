// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CrossPlatformValidation;
using Microsoft.Identity.Client;
using Microsoft.Identity.Lab.Api;

Console.WriteLine("Hello, World!");

RequestValidator requestValidator = new();

requestValidator.Initialize("https://login.microsoftonline.com/organizations", "f4aa5217-e87c-42b2-82af-5624dd14ee72");
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
 string OBOUser = "idlab1@msidlab4.onmicrosoft.com";
 string OBOClientSideClientId = "c0485386-1e9a-4663-bc96-7ab30656de7f";
 string[] s_oBOApiScope = new string[] { "api://f4aa5217-e87c-42b2-82af-5624dd14ee72/.default" };

var labResponse = await LabUserHelper.GetSpecificUserAsync(OBOUser).ConfigureAwait(false);
    var msalPublicClient = PublicClientApplicationBuilder
       .Create(OBOClientSideClientId)
       .WithAuthority(labResponse.Lab.Authority, Organizations)
       .Build();

    AuthenticationResult authResult = await msalPublicClient
        .AcquireTokenByUsernamePassword(
        s_oBOApiScope,
        OBOUser,
        labResponse.User.GetOrFetchPassword())
        .ExecuteAsync(CancellationToken.None)
        .ConfigureAwait(false);

    return authResult;
}
