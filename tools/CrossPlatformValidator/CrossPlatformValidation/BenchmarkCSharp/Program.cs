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
using Microsoft.Identity.Test.LabInfrastructure;

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
        private static LabUserConfig userConfig;
        private static string authorizationHeader;
        private static RequestValidator _requestValidator;


        static ValidateBenchmark()
        {
            _requestValidator = new RequestValidator();
            _requestValidator.Initialize("https://login.microsoftonline.com/organizations", "8837cde9-4029-4bfc-9259-e9e70ce670f7");
            userConfig = LabResponseHelper.GetUserConfigAsync("MSAL-User-Default-JSON").GetAwaiter().GetResult();
            msalPublicClient = PublicClientApplicationBuilder
               .Create(OBOClientSideClientId)
               .WithAuthority($"{userConfig.Authority}{userConfig.TenantId}", Organizations)
               .Build();
            authorizationHeader = AcquireTokenForLabUserAsync().Result.CreateAuthorizationHeader();
        }

        public const string Organizations = "organizations";
        public const string OBOUser = "MSAL-User-Default@id4slab1.onmicrosoft.com";
        public const string OBOClientSideClientId = "9c0e534b-879c-4dce-b0e2-0e1be873ba14";
        public static string[] s_oBOApiScope = new string[] { "api://8837cde9-4029-4bfc-9259-e9e70ce670f7/.default" };
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
                    LabResponseHelper.FetchUserPassword(userConfig.LabName))
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

