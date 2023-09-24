﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Identity.Client;
using Microsoft.Identity.Lab.Api;

namespace Brenchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ValidateBenchmark>();
        }
    }


    public class ValidateBenchmark
    {
        public ValidateBenchmark()
        {
            Initialize("https://login.microsoftonline.com/organizations", "f4aa5217-e87c-42b2-82af-5624dd14ee72");
        }

        public const string Organizations = "organizations";
        public const string OBOUser = "fIDLAB@msidlab4.com";
        public const string OBOClientSideClientId = "c0485386-1e9a-4663-bc96-7ab30656de7f";
        public static string[] s_oBOApiScope = new string[] { "api://f4aa5217-e87c-42b2-82af-5624dd14ee72/.default" };
        public int numberValidations = 1000000;

        [DllImport("CrossPlatformValidation.dll")]
        static extern void Initialize(string authority, string audience);

        [DllImport("CrossPlatformValidation.dll")]
        static extern string Validate(string authorizationHeader);

        [Benchmark]
        public void ValidateAuthRequestBenchmark()
        {
            string authorizationHeader = AcquireTokenForLabUserAsync().Result.CreateAuthorizationHeader();
            var result = Validate(authorizationHeader);
        }

        private static async Task<AuthenticationResult> AcquireTokenForLabUserAsync()
        {
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
    }
}
