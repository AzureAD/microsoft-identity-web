// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.LabInfrastructure;

namespace CrossPlatformValidatorTests
{
    public class InitializeAndValidateTests
    {
        public const string Organizations = "organizations";
        public const string OBOUser = "MSAL-User-Default@id4slab1.onmicrosoft.com";
        public const string OBOClientSideClientId = "9c0e534b-879c-4dce-b0e2-0e1be873ba14";
        public static string[] s_oBOApiScope = new string[] { "api://8837cde9-4029-4bfc-9259-e9e70ce670f7/.default" };
        public int numberValidations = 1000000;

        [DllImport("CrossPlatformValidation.dll")]
        static extern void Initialize(string authority, string audience);

        [DllImport("CrossPlatformValidation.dll")]
        static extern string Validate(string authorizationHeader);

        [Fact]
        public void InitializeTestSucceeds()
        {
            Initialize("https://login.microsoftonline.com/organizations", OBOClientSideClientId);

        }

        [Fact]
        public void ValidateTestSucceeds()
        {
            Initialize("https://login.microsoftonline.com/organizations", "8837cde9-4029-4bfc-9259-e9e70ce670f7");
            string authorizationHeader = AcquireTokenForLabUserAsync().Result.CreateAuthorizationHeader();
            for (int i = 0; i < numberValidations; i++)
            {
                var result = Validate(authorizationHeader);
                Assert.NotNull(result);
            }

        }

        private static async Task<AuthenticationResult> AcquireTokenForLabUserAsync()
        {
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

    }
}
