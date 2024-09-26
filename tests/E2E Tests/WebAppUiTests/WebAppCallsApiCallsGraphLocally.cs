// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Lab.Api;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using Process = System.Diagnostics.Process;
using TC = Microsoft.Identity.Web.Test.Common.TestConstants;
using UITH = WebAppUiTests.UiTestHelpers;

namespace WebAppUiTests
#if !FROM_GITHUB_ACTION
{
    // since these tests change environment variables we'd prefer it not run at the same time as other tests
    [CollectionDefinition(nameof(UiTestNoParallelization), DisableParallelization = true)]
    public class WebAppCallsApiCallsGraphLocally : IClassFixture<InstallPlaywrightBrowserFixture>
    {
        private const uint GrpcPort = 5001;
        private const string SignOutPageUriPath = @"/MicrosoftIdentity/Account/SignedOut";
        private const uint TodoListClientPort = 44321;
        private const uint TodoListServicePort = 44350;
        private const uint WebAppCiamPort = 7096;
        private const uint WebApiCiamPort = 5299;
        private const string TraceFileClassName = "WebAppCallsApiCallsGraphLocally";
        private const uint NumProcessRetries = 3;
        private readonly LocatorAssertionsToBeVisibleOptions _assertVisibleOptions = new() { Timeout = 25000 };
        private readonly string _devAppPath = "DevApps" + Path.DirectorySeparatorChar.ToString() + "WebAppCallsWebApiCallsGraph";
        private readonly string _devAppPathCiam = Path.Join("DevApps", "ciam");
        private readonly string _grpcExecutable = Path.DirectorySeparatorChar.ToString() + "grpc.exe";
        private readonly string _grpcPath = Path.DirectorySeparatorChar.ToString() + "gRPC";
        private readonly string _testAssemblyLocation = typeof(WebAppCallsApiCallsGraphLocally).Assembly.Location;
        private readonly ITestOutputHelper _output;

        public WebAppCallsApiCallsGraphLocally(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public async Task ChallengeUser_MicrosoftIdFlow_LocalApp_ValidEmailPasswordCreds_TodoAppFunctionsCorrectly()
        {
            // Setup web app and api environmental variables.
            var grpcEnvVars = new Dictionary<string, string>
            {
                {"ASPNETCORE_ENVIRONMENT", "Development"},
                {TC.KestrelEndpointEnvVar, TC.HttpsStarColon + GrpcPort}
            };
            var serviceEnvVars = new Dictionary<string, string>
            {
                {"ASPNETCORE_ENVIRONMENT", "Development" },
                {TC.KestrelEndpointEnvVar, TC.HttpStarColon + TodoListServicePort}
            };
            var clientEnvVars = new Dictionary<string, string>
            {
                {"ASPNETCORE_ENVIRONMENT", "Development"},
                {TC.KestrelEndpointEnvVar, TC.HttpsStarColon + TodoListClientPort}
            };

            Dictionary<string, Process>? processes = null;

            // Arrange Playwright setup, to see the browser UI set Headless = false.
            const string TraceFileName = TraceFileClassName + "_TodoAppFunctionsCorrectly";
            using IPlaywright playwright = await Playwright.CreateAsync();
            IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
            await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
            IPage page = await context.NewPageAsync();

            try
            {
                // Start the web app and api processes.
                // The delay before starting client prevents transient devbox issue where the client fails to load the first time after rebuilding.
                var grpcProcessOptions = new ProcessStartOptions(_testAssemblyLocation, _devAppPath + _grpcPath, _grpcExecutable, grpcEnvVars);
                var serviceProcessOptions = new ProcessStartOptions(_testAssemblyLocation, _devAppPath + TC.s_todoListServicePath, TC.s_todoListServiceExe, serviceEnvVars);
                var clientProcessOptions = new ProcessStartOptions(_testAssemblyLocation, _devAppPath + TC.s_todoListClientPath, TC.s_todoListClientExe, clientEnvVars);

                bool areProcessesRunning = UITH.StartAndVerifyProcessesAreRunning(new List<ProcessStartOptions> { /*grpcProcessOptions,*/ serviceProcessOptions, clientProcessOptions }, out processes, NumProcessRetries);

                if (!areProcessesRunning)
                {
                    _output.WriteLine($"Process not started after {NumProcessRetries} attempts.");
                    StringBuilder runningProcesses = new StringBuilder();
                    foreach (var process in processes)
                    {
                        runningProcesses.AppendLine(CultureInfo.InvariantCulture, $"Is {process.Key} running: {UITH.ProcessIsAlive(process.Value)}");
                    }
                    Assert.Fail(TC.WebAppCrashedString + " " + runningProcesses.ToString());
                }

                await UITH.NavigateToWebApp(TC.LocalhostUrl + TodoListClientPort, page);
                LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync();

                // Initial sign in
                _output.WriteLine("Starting web app sign-in flow.");
                string email = labResponse.User.Upn;
                await UITH.FirstLogin_MicrosoftIdFlow_ValidEmailPassword(page, email, labResponse.User.GetOrFetchPassword(), _output);
                await Assertions.Expect(page.GetByText("TodoList")).ToBeVisibleAsync(_assertVisibleOptions);
                await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app sign-in flow successful.");

                // Sign out
                _output.WriteLine("Starting web app sign-out flow.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Sign out" }).ClickAsync();
                await UITH.PerformSignOut_MicrosoftIdFlow(page, email, TC.LocalhostUrl + TodoListClientPort + SignOutPageUriPath, _output);
                _output.WriteLine("Web app sign out successful.");

                // Sign in again using Todo List button
                _output.WriteLine("Starting web app sign-in flow using Todo List button after sign out.");
                await page.GetByRole(AriaRole.Link, new() { Name = "TodoList" }).ClickAsync();
                await UITH.SuccessiveLogin_MicrosoftIdFlow_ValidEmailPassword(page, email, labResponse.User.GetOrFetchPassword(), _output);
                var todoLink = page.GetByRole(AriaRole.Link, new() { Name = "Create New" });
                await Assertions.Expect(todoLink).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app sign-in flow successful using Todo List button after sign out.");

                // Create new todo item
                _output.WriteLine("Starting web app create new todo flow.");
                await todoLink.ClickAsync();
                var titleEntryBox = page.GetByLabel("Title");
                await UITH.FillEntryBox(titleEntryBox, TC.TodoTitle1);
                await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TC.TodoTitle1 })).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app create new todo flow successful.");

                // Edit todo item
                _output.WriteLine("Starting web app edit todo flow.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync();
                await UITH.FillEntryBox(titleEntryBox, TC.TodoTitle2);
                await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TC.TodoTitle2 })).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app edit todo flow successful.");

                // Delete todo item
                _output.WriteLine("Starting web app delete todo flow.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
                await page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
                await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TC.TodoTitle2 })).Not.ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app delete todo flow successful.");
            }
            catch (Exception ex)
            {
                //Adding guid incase of multiple test runs. This will allow screenshots to be matched to their appropriet test runs.
                var guid = Guid.NewGuid().ToString();
                try
                {
                    if (page != null)
                    {
                        await page.ScreenshotAsync(new PageScreenshotOptions() { Path = $"ChallengeUser_MicrosoftIdFlow_LocalApp_ValidEmailPasswordCreds_TodoAppFunctionsCorrectlyScreenshotFail{guid}.png", FullPage = true });
                    }
                }
                catch
                {
                    _output.WriteLine("No Screenshot.");
                }

                string runningProcesses = UITH.GetRunningProcessAsString(processes);

                Assert.Fail($"the UI automation failed: {ex} output: {ex.Message}.\n{runningProcesses}\nTest run: {guid}");
            }
            finally
            {
                // Make sure all application processes and their children are stopped.
                UITH.EndProcesses(processes);

                // Stop tracing and export it into a zip archive.
                string path = UITH.GetTracePath(_testAssemblyLocation, TraceFileName);
                await context.Tracing.StopAsync(new() { Path = path });
                _output.WriteLine($"Trace data for {TraceFileName} recorded to {path}.");

                // Close the browser and stop Playwright.
                await browser.CloseAsync();
                playwright.Dispose();
            }
        }

        [Theory]
        [InlineData("https://MSIDLABCIAM6.ciamlogin.com")] // CIAM authority
        [InlineData("https://login.msidlabsciam.com/fe362aec-5d43-45d1-b730-9755e60dc3b9/v2.0/")] // CIAM CUD Authority
        [SupportedOSPlatform("windows")]
        public async Task ChallengeUser_MicrosoftIdFlow_LocalApp_ValidEmailPasswordCreds_CallsDownStreamApiWithCiam(string authority)
        {
            // Setup web app and api environmental variables.
            var serviceEnvVars = new Dictionary<string, string>
            {
                {"ASPNETCORE_ENVIRONMENT", "Development"},
                {"AzureAd__ClientId", "634de702-3173-4a71-b336-a4fab786a479"},
                {"AzureAd__Authority", authority},
                {TC.KestrelEndpointEnvVar, TC.HttpStarColon + WebApiCiamPort}
            };
            var clientEnvVars = new Dictionary<string, string>
            {
                {"ASPNETCORE_ENVIRONMENT", "Development"},
                {"AzureAd__ClientId", "b244c86f-ed88-45bf-abda-6b37aa482c79"},
                {"AzureAd__Authority", authority},
                {"DownstreamApi__Scopes__0", "api://634de702-3173-4a71-b336-a4fab786a479/.default"},
                {TC.KestrelEndpointEnvVar, TC.HttpsStarColon + WebAppCiamPort}
            };

            Dictionary<string, Process>? processes = null;

            // Arrange Playwright setup, to see the browser UI set Headless = false.
            const string TraceFileName = TraceFileClassName + "_CiamWebApp_WebApiFunctionsCorrectly";
            using IPlaywright playwright = await Playwright.CreateAsync();
            IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
            await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
            IPage page = await context.NewPageAsync();

            try
            {
                // Start the web app and api processes.
                // The delay before starting client prevents transient devbox issue where the client fails to load the first time after rebuilding.
                var serviceProcessOptions = new ProcessStartOptions(_testAssemblyLocation, _devAppPathCiam + TC.s_myWebApiPath, TC.s_myWebApiExe, serviceEnvVars);
                var clientProcessOptions = new ProcessStartOptions(_testAssemblyLocation, _devAppPathCiam + TC.s_myWebAppPath, TC.s_myWebAppExe, clientEnvVars);
                bool areProcessesRunning = UITH.StartAndVerifyProcessesAreRunning(new List<ProcessStartOptions> { serviceProcessOptions, clientProcessOptions }, out processes, NumProcessRetries);

                if (!areProcessesRunning)
                {
                    _output.WriteLine($"Process not started after {NumProcessRetries} attempts.");
                    StringBuilder runningProcesses = new StringBuilder();
                    foreach (var process in processes)
                    {
#pragma warning disable CA1305 // Specify IFormatProvider
                        runningProcesses.AppendLine($"Is {process.Key} running: {UITH.ProcessIsAlive(process.Value)}");
#pragma warning restore CA1305 // Specify IFormatProvider
                    }
                    Assert.Fail(TC.WebAppCrashedString + " " + runningProcesses.ToString());
                }

                await UITH.NavigateToWebApp(TC.LocalhostUrl + WebAppCiamPort, page);

                // Initial sign in
                _output.WriteLine("Starting web app sign-in flow.");
                string email = "idlab@msidlabciam6.onmicrosoft.com";
                await UITH.FirstLogin_MicrosoftIdFlow_ValidEmailPassword(page, email, LabUserHelper.FetchUserPassword("msidlabciam6"), _output);
                await Assertions.Expect(page.GetByText("Welcome")).ToBeVisibleAsync(_assertVisibleOptions);
                await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app sign-in flow successful.");

                // Sign out
                _output.WriteLine("Starting web app sign-out flow.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Sign out" }).ClickAsync();
                await UITH.PerformSignOut_MicrosoftIdFlow(page, email, TC.LocalhostUrl + WebAppCiamPort + SignOutPageUriPath, _output);
                _output.WriteLine("Web app sign out successful.");

                // Sign in again using Todo List button
                _output.WriteLine("Starting web app sign-in flow using sign in button after sign out.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Sign in" }).ClickAsync();
                await UITH.FirstLogin_MicrosoftIdFlow_ValidEmailPassword(page, email, LabUserHelper.FetchUserPassword("msidlabciam6"), _output);
                await Assertions.Expect(page.GetByText("Welcome")).ToBeVisibleAsync(_assertVisibleOptions);
                await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app sign-in flow successful using Sign in button after sign out.");
            }
            catch (Exception ex)
            {
                //Adding guid in case of multiple test runs. This will allow screenshots to be matched to their appropriet test runs.
                var guid = Guid.NewGuid().ToString();
                try
                {
                    if (page != null)
                    {
                        await page.ScreenshotAsync(new PageScreenshotOptions() { Path = $"ChallengeUser_MicrosoftIdFlow_LocalApp_ValidEmailPasswordCreds_CallsDownStreamApiWithCiamScreenshotFail{guid}.png", FullPage = true });
                    }
                }
                catch
                {
                    _output.WriteLine("No Screenshot.");
                }

                string runningProcesses = UITH.GetRunningProcessAsString(processes);

                Assert.Fail($"the UI automation failed: {ex} output: {ex.Message}.\n{runningProcesses}\nTest run: {guid}");
            }
            finally
            {
                // Add the following to make sure all processes and their children are stopped.
                UITH.EndProcesses(processes);

                // Stop tracing and export it into a zip archive.
                string path = UITH.GetTracePath(_testAssemblyLocation, TraceFileName);
                await context.Tracing.StopAsync(new() { Path = path });
                _output.WriteLine($"Trace data for {TraceFileName} recorded to {path}.");

                // Close the browser and stop Playwright.
                await browser.CloseAsync();
                playwright.Dispose();
            }
        }
    }
}
#endif // !FROM_GITHUB_ACTION
