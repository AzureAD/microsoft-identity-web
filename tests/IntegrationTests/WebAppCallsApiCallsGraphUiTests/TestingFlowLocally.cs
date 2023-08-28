// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Identity.Lab.Api;
using Microsoft.Playwright;
using WebAppUiTests;
using Xunit;

namespace WebAppCallsApiCallsGraphUiTests
{
    public class TestingFlowLocally
    {
        private const string UrlString = "https://localhost:44351/MicrosoftIdentity/Account/signin";
        private const string DevAppPath = @"DevApps\WebAppsCallsWebApiCallsGraph";
        private const string TodoListServicePath = @"\TodoListService";
        private const string TodoListClientPath = @"\TodoListClient";
        private const string GrpcPath = @"\grpc";
        private const string TodoListServiceExecutable = "TodoListService.exe";
        private const string TodoListClientExecutable = "TodoListClient.exe";
        private const string GrpcExecutable = "grpc.exe";
        private string UiTestAssemblyLocation = typeof(TestingFlowLocally).Assembly.Location;


        [Fact]
        public async Task ChallengeUser_MicrosoftIdentityFlow_LocalApp_ValidEmailPasswordCreds_TodoAppFunctionsCorrectly()
        {
            Process? clientProcess = UiTestHelpers.StartWebAppLocally(UiTestAssemblyLocation, DevAppPath + TodoListClientPath, TodoListClientExecutable);
            Process? serviceProcess = UiTestHelpers.StartWebAppLocally(UiTestAssemblyLocation, DevAppPath + TodoListServicePath, TodoListServiceExecutable);
            Process? grpcProcess = UiTestHelpers.StartWebAppLocally(UiTestAssemblyLocation, DevAppPath + GrpcPath, GrpcExecutable);

            if (clientProcess != null && serviceProcess != null && grpcProcess != null)
            {
                if (clientProcess.HasExited || serviceProcess.HasExited || grpcProcess.HasExited)
                {
                    Assert.Fail($"Could not run web app locally.");
                }

                using var playwright = Playwright.CreateAsync();
                IBrowser browser;
                browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
                IPage page = browser.NewPageAsync();

                try
                {
                    // Act
                    Trace.WriteLine("Starting Playwright automation: web app sign-in & call Graph");
                    string email = ""
                }
            }
        }
    }
}
