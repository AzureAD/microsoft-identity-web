// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Lab.Api;
using TC = Microsoft.Identity.Web.Test.Common.TestConstants;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using Process = System.Diagnostics.Process;

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
        private const uint WebAppCiamPort = 7082;
        private const uint WebApiCiamPort = 44332;
        private const string TraceFileClassName = "WebAppCallsApiCallsGraphLocally";
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

        [Fact(Skip = "https://github.com/AzureAD/microsoft-identity-web/issues/2716")]
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

            Process? grpcProcess = null;
            Process? serviceProcess = null;
            Process? clientProcess = null;

            // Arrange Playwright setup, to see the browser UI set Headless = false.
            const string TraceFileName = TraceFileClassName + "_TodoAppFunctionsCorrectly";
            using IPlaywright playwright = await Playwright.CreateAsync();
            IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
            await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });

            try
            {
                // Start the web app and api processes.
                // The delay before starting client prevents transient devbox issue where the client fails to load the first time after rebuilding.
                grpcProcess = UiTestHelpers.StartProcessLocally(_testAssemblyLocation, _devAppPath + _grpcPath, _grpcExecutable, grpcEnvVars);
                serviceProcess = UiTestHelpers.StartProcessLocally(_testAssemblyLocation, _devAppPath + TC.s_todoListServicePath, TC.s_todoListServiceExe, serviceEnvVars);
                await Task.Delay(3000);
                clientProcess = UiTestHelpers.StartProcessLocally(_testAssemblyLocation, _devAppPath + TC.s_todoListClientPath, TC.s_todoListClientExe, clientEnvVars);

                if ( !UiTestHelpers.ProcessesAreAlive(new List<Process>() { clientProcess, serviceProcess, grpcProcess }))
                    {
                        Assert.Fail(TC.WebAppCrashedString);
                    }

                var page = await NavigateToWebApp(context, TodoListClientPort).ConfigureAwait(false);
                LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

                // Initial sign in
                _output.WriteLine("Starting web app sign-in flow.");
                string email = labResponse.User.Upn;
                await UiTestHelpers.FirstLogin_MicrosoftIdFlow_ValidEmailPassword(page, email, labResponse.User.GetOrFetchPassword(), _output);
                await Assertions.Expect(page.GetByText("TodoList")).ToBeVisibleAsync(_assertVisibleOptions);
                await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app sign-in flow successful.");

                // Sign out
                _output.WriteLine("Starting web app sign-out flow.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Sign out" }).ClickAsync();
                await UiTestHelpers.PerformSignOut_MicrosoftIdFlow(page, email, TC.LocalhostUrl + TodoListClientPort + SignOutPageUriPath, _output);
                _output.WriteLine("Web app sign out successful.");

                // Sign in again using Todo List button
                _output.WriteLine("Starting web app sign-in flow using Todo List button after sign out.");
                await page.GetByRole(AriaRole.Link, new() { Name = "TodoList" }).ClickAsync();
                await UiTestHelpers.SuccessiveLogin_MicrosoftIdFlow_ValidEmailPassword(page, email, labResponse.User.GetOrFetchPassword(), _output);
                var todoLink =  page.GetByRole(AriaRole.Link, new() { Name = "Create New" });
                await Assertions.Expect(todoLink).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app sign-in flow successful using Todo List button after sign out.");

                // Create new todo item
                _output.WriteLine("Starting web app create new todo flow.");
                await todoLink.ClickAsync();
                var titleEntryBox = page.GetByLabel("Title");
                await UiTestHelpers.FillEntryBox(titleEntryBox, TC.TodoTitle1);
                await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TC.TodoTitle1 })).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app create new todo flow successful.");

                // Edit todo item
                _output.WriteLine("Starting web app edit todo flow.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync();
                await UiTestHelpers.FillEntryBox(titleEntryBox, TC.TodoTitle2);
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
                Assert.Fail($"the UI automation failed: {ex} output: {ex.Message}.");
            }
            finally
            {
                // Add the following to make sure all processes and their children are stopped.
                Queue<Process> processes = new();
                if (serviceProcess != null) { processes.Enqueue(serviceProcess); }
                if (clientProcess != null) { processes.Enqueue(clientProcess); }
                if (grpcProcess != null) { processes.Enqueue(grpcProcess); }
                UiTestHelpers.KillProcessTrees(processes);

                // Stop tracing and export it into a zip archive.
                string path = UiTestHelpers.GetTracePath(_testAssemblyLocation, TraceFileName);
                await context.Tracing.StopAsync(new() { Path = path });
                _output.WriteLine($"Trace data for {TraceFileName} recorded to {path}.");

                // Close the browser and stop Playwright.
                await browser.CloseAsync();
                playwright.Dispose();
            }
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public async Task ChallengeUser_MicrosoftIdFlow_LocalApp_ValidEmailPasswordCreds_CallsDownStreamApiWithCiam()
        {
            // Setup web app and api environmental variables.
            var serviceEnvVars = new Dictionary<string, string>
            {
                {"ASPNETCORE_ENVIRONMENT", "Development"},
                {"AzureAd__ClientId", "634de702-3173-4a71-b336-a4fab786a479"},
                {"AzureAd__Authority", "https://MSIDLABCIAM6.ciamlogin.com"},
                {TC.KestrelEndpointEnvVar, TC.HttpStarColon + WebApiCiamPort}
            };
            var clientEnvVars = new Dictionary<string, string>
            {
                {"ASPNETCORE_ENVIRONMENT", "Development"},
                {"AzureAd__ClientId", "b244c86f-ed88-45bf-abda-6b37aa482c79"},
                {"AzureAd__Authority", "https://MSIDLABCIAM6.ciamlogin.com"},
                {"DownstreamApi__Scopes__0", "api://634de702-3173-4a71-b336-a4fab786a479/.default"},
                {TC.KestrelEndpointEnvVar, TC.HttpsStarColon + WebAppCiamPort}
            };

            Process? serviceProcess = null;
            Process? clientProcess = null;

            // Arrange Playwright setup, to see the browser UI set Headless = false.
            const string TraceFileName = TraceFileClassName + "_CiamWebApp_WebApiFunctionsCorrectly";
            using IPlaywright playwright = await Playwright.CreateAsync();
            IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
            await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });

            try
            {
                // Start the web app and api processes.
                // The delay before starting client prevents transient devbox issue where the client fails to load the first time after rebuilding.
                serviceProcess = UiTestHelpers.StartProcessLocally(_testAssemblyLocation, _devAppPathCiam + TC.s_myWebApiPath, TC.s_myWebApiExe, serviceEnvVars);
                await Task.Delay(3000);
                clientProcess = UiTestHelpers.StartProcessLocally(_testAssemblyLocation, _devAppPathCiam + TC.s_myWebAppPath, TC.s_myWebAppExe, clientEnvVars);

                if (!UiTestHelpers.ProcessesAreAlive(new List<Process>() { clientProcess, serviceProcess }))
                {
                    Assert.Fail(TC.WebAppCrashedString);
                }

                var page = await NavigateToWebApp(context, WebAppCiamPort);

                // Initial sign in
                _output.WriteLine("Starting web app sign-in flow.");
                string email = "idlab@msidlabciam6.onmicrosoft.com";
                await UiTestHelpers.FirstLogin_MicrosoftIdFlow_ValidEmailPassword(page, email, LabUserHelper.FetchUserPassword("msidlabciam6"), _output);
                await Assertions.Expect(page.GetByText("Welcome")).ToBeVisibleAsync(_assertVisibleOptions);
                await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app sign-in flow successful.");

                // Sign out
                _output.WriteLine("Starting web app sign-out flow.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Sign out" }).ClickAsync();
                await UiTestHelpers.PerformSignOut_MicrosoftIdFlow(page, email, TC.LocalhostUrl + WebAppCiamPort + SignOutPageUriPath, _output);
                _output.WriteLine("Web app sign out successful.");

                // Sign in again using Todo List button
                _output.WriteLine("Starting web app sign-in flow using sign in button after sign out.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Sign in" }).ClickAsync();
                await UiTestHelpers.FirstLogin_MicrosoftIdFlow_ValidEmailPassword(page, email, LabUserHelper.FetchUserPassword("msidlabciam6"), _output);
                await Assertions.Expect(page.GetByText("Welcome")).ToBeVisibleAsync(_assertVisibleOptions);
                await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app sign-in flow successful using Sign in button after sign out.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"the UI automation failed: {ex} output: {ex.Message}.");
            }
            finally
            {
                // Add the following to make sure all processes and their children are stopped.
                Queue<Process> processes = new();
                if (serviceProcess != null)
                { processes.Enqueue(serviceProcess); }
                if (clientProcess != null)
                { processes.Enqueue(clientProcess); }
                UiTestHelpers.KillProcessTrees(processes);

                // Stop tracing and export it into a zip archive.
                string path = UiTestHelpers.GetTracePath(_testAssemblyLocation, TraceFileName);
                await context.Tracing.StopAsync(new() { Path = path });
                _output.WriteLine($"Trace data for {TraceFileName} recorded to {path}.");

                // Close the browser and stop Playwright.
                await browser.CloseAsync();
                playwright.Dispose();
            }
        }

        private async Task<IPage> NavigateToWebApp(IBrowserContext context, uint port)
        {
            // Navigate to web app
            IPage page = await context.NewPageAsync();

            // The retry logic ensures the web app has time to start up to establish a connection.
            uint InitialConnectionRetryCount = 5;
            while (InitialConnectionRetryCount > 0)
            {
                try
                {
                    await page.GotoAsync(TC.LocalhostUrl + port);
                    break;
                }
                catch (PlaywrightException ex)
                {
                    await Task.Delay(1000);
                    InitialConnectionRetryCount--;
                    if (InitialConnectionRetryCount == 0)
                    { throw ex; }
                }
            }

            return page;
        }
    }
}
#endif // !FROM_GITHUB_ACTION
