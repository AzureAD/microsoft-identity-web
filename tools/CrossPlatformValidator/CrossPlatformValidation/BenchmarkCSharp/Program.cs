// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using CrossPlatformValidation;
using Microsoft.Identity.Client;
using Microsoft.Identity.Lab.Api;

namespace BenchmarkCSharp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            DisplayTestSubject();
            BenchmarkRunner.Run<ValidateBenchmark>();
            DisplayTestSubject();
        }

        private static void DisplayTestSubject()
        {
#if JWT_TOKEN
            Console.WriteLine($"JwtSecurityToken for {typeof(Program).Assembly.Location}");
#else
            Console.WriteLine($"JsonWebToken for {typeof(Program).Assembly.Location}");
#endif
        }
    }

    [Config(typeof(AntiVirusFriendlyConfig))]
    [MemoryDiagnoser]
    public class ValidateBenchmark
    {
        private static IPublicClientApplication msalPublicClient;
        private static LabResponse labResponse;
        private static string authorizationHeader;
        private static RequestValidator _requestValidator;


        static ValidateBenchmark()
        {
            _requestValidator = new RequestValidator();
            _requestValidator.Initialize("https://login.microsoftonline.com/organizations", "f4aa5217-e87c-42b2-82af-5624dd14ee72");
            labResponse = LabUserHelper.GetSpecificUserAsync(OBOUser).GetAwaiter().GetResult();
            msalPublicClient = PublicClientApplicationBuilder
               .Create(OBOClientSideClientId)
               .WithAuthority(labResponse.Lab.Authority, Organizations)
               .Build();
            authorizationHeader = AcquireTokenForLabUserAsync().Result.CreateAuthorizationHeader();
        }

        public const string Organizations = "organizations";
        public const string OBOUser = "idlab1@msidlab4.onmicrosoft.com";
        public const string OBOClientSideClientId = "c0485386-1e9a-4663-bc96-7ab30656de7f";
        public static string[] s_oBOApiScope = new string[] { "api://f4aa5217-e87c-42b2-82af-5624dd14ee72/.default" };
        public int numberValidations = 1000000;

        [Benchmark]
        public void ValidateAuthRequestCSharpBenchmark()
        {
            if (_requestValidator.Validate(authorizationHeader) == null)
            {
                string authorizationHeader = ValidateBenchmark.AcquireTokenForLabUserAsync().Result.CreateAuthorizationHeader();
                if (_requestValidator.Validate(authorizationHeader) == null)
                {
                    throw new ArgumentException("Validation failed");
                }
            }
        }

        private static async Task<AuthenticationResult> AcquireTokenForLabUserAsync()
        {
            AuthenticationResult authResult;
            try
            {
                var accounts = await msalPublicClient.GetAccountsAsync()
                    .ConfigureAwait(false);
                authResult = await msalPublicClient.AcquireTokenSilent(s_oBOApiScope, accounts.FirstOrDefault())
                    .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                authResult = await msalPublicClient
                    .AcquireTokenByUsernamePassword(
                    s_oBOApiScope,
                    OBOUser,
                    labResponse.User.GetOrFetchPassword())
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            return authResult;
        }
    }

    public class AntiVirusFriendlyConfig : ManualConfig
    {
        public AntiVirusFriendlyConfig()
        {
            AddJob(Job.MediumRun
                .WithToolchain(InProcessNoEmitToolchain.Instance));
        }
    }
}

