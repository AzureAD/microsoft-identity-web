// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Lab.Api;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using Process = System.Diagnostics.Process;

namespace WebAppUiTests
#if !FROM_GITHUB_ACTION
{
    [CollectionDefinition(nameof(TestingWebAppCallsApiCallsGraphLocally), DisableParallelization = true)] // since this changes environment variables we'd prefer it not run at the same time as other tests
    public class TestingWebAppCallsApiCallsGraphLocally : IClassFixture<InstallPlaywrightBrowserFixture>
    {
        private const string LocalhostUrl = @"https://localhost:";
        private const uint GrpcPort = 5001;
        private const uint TodoListClientPort = 44321;
        private const uint TodoListServicePort = 44350;
        private const string SignOutPageUriPath = @"/MicrosoftIdentity/Account/SignedOut";
        private const string TodoTitle1 = "Testing create todo item";
        private const string TodoTitle2 = "Testing edit todo item";
        private const string TraceFileClassName = "TestingWebAppCallsApiCallsGraphLocally";
        private readonly string _devAppPath = "DevApps" + Path.DirectorySeparatorChar.ToString() + "WebAppCallsWebApiCallsGraph";
        private readonly string _grpcExecutable = Path.DirectorySeparatorChar.ToString() + "grpc.exe";
        private readonly string _grpcPath = Path.DirectorySeparatorChar.ToString() + "gRPC";
        private readonly string _todoListClientExecutable = Path.DirectorySeparatorChar.ToString() + "TodoListClient.exe";
        private readonly string _todoListClientPath = Path.DirectorySeparatorChar.ToString() + "Client";
        private readonly string _todoListServiceExecutable = Path.DirectorySeparatorChar.ToString() + "TodoListService.exe";
        private readonly string _todoListServicePath = Path.DirectorySeparatorChar.ToString() + "TodoListService";
        private readonly string _uiTestAssemblyLocation = typeof(TestingWebAppCallsApiCallsGraphLocally).Assembly.Location;
        private readonly ITestOutputHelper _output;

        public TestingWebAppCallsApiCallsGraphLocally(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public async Task ChallengeUser_MicrosoftIdFlow_LocalApp_ValidEmailPasswordCreds_TodoAppFunctionsCorrectly()
        {
            // Arrange web app setup
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Process? grpcProcess = UiTestHelpers.StartProcessLocally(_uiTestAssemblyLocation, _devAppPath + _grpcPath, _grpcExecutable, GrpcPort);
            Process? serviceProcess = UiTestHelpers.StartProcessLocally(_uiTestAssemblyLocation, _devAppPath + _todoListServicePath, _todoListServiceExecutable, TodoListServicePort, true);
            
            // Wait 5s for service to start. Prevents transient issue where client fails to load on devbox the first time the test is run in VS after rebuilding.
            Thread.Sleep(5000); 
            Process? clientProcess = UiTestHelpers.StartProcessLocally(_uiTestAssemblyLocation, _devAppPath + _todoListClientPath, _todoListClientExecutable, TodoListClientPort);

            // Arrange Playwright setup, to see the browser UI, set Headless = false
            const string TraceFileName = TraceFileClassName + "_TodoAppFunctionsCorrectly";
            using IPlaywright playwright = await Playwright.CreateAsync();
            IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
            await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });

            try
            {
                if ( !UiTestHelpers.ProcessesAreAlive(new List<Process>() { clientProcess!, serviceProcess!, grpcProcess! }))
                    {
                        Assert.Fail($"Could not run web app locally.");
                    }

                IPage page = await context.NewPageAsync();
                await page.GotoAsync(LocalhostUrl + TodoListClientPort);
                LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

                // Initial sign in
                _output.WriteLine("Starting web app sign-in flow.");
                string email = labResponse.User.Upn;
                await UiTestHelpers.FirstLogin_MicrosoftIdFlow_ValidEmailPassword(page, email, labResponse.User.GetOrFetchPassword(), _output);
                await Assertions.Expect(page.GetByText("TodoList")).ToBeVisibleAsync();
                await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync();
                _output.WriteLine("Web app sign-in flow successful.");

                // Sign out
                _output.WriteLine("Starting web app sign-out flow.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Sign out" }).ClickAsync();
                await UiTestHelpers.PerformSignOut_MicrosoftIdFlow(page, email, LocalhostUrl + TodoListClientPort + SignOutPageUriPath, _output);
                _output.WriteLine("Web app sign out successful.");

                // Sign in again using Todo List button
                _output.WriteLine("Starting web app sign-in flow using Todo List button after sign out.");
                await page.GetByRole(AriaRole.Link, new() { Name = "TodoList" }).ClickAsync();
                await UiTestHelpers.SuccessiveLogin_MicrosoftIdFlow_ValidEmailPassword(page, email, labResponse.User.GetOrFetchPassword(), _output);
                var TodoLink =  page.GetByRole(AriaRole.Link, new() { Name = "Create New" });
                await Assertions.Expect(TodoLink).ToBeVisibleAsync();
                _output.WriteLine("Web app sign-in flow successful using Todo List button after sign out.");

                // Create new todo item
                _output.WriteLine("Starting web app create new todo flow.");
                await TodoLink.ClickAsync();
                var TitleEntryBox = page.GetByLabel("Title");
                await UiTestHelpers.FillEntryBox(TitleEntryBox, TodoTitle1);
                await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TodoTitle1 })).ToBeVisibleAsync();
                _output.WriteLine("Web app create new todo flow successful.");

                // Edit todo item
                _output.WriteLine("Starting web app edit todo flow.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync();
                await UiTestHelpers.FillEntryBox(TitleEntryBox, TodoTitle2);
                await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TodoTitle2 })).ToBeVisibleAsync();
                _output.WriteLine("Web app edit todo flow successful.");

                // Delete todo item
                _output.WriteLine("Starting web app delete todo flow.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
                await page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
                await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TodoTitle2 })).Not.ToBeVisibleAsync();
                _output.WriteLine("Web app delete todo flow successful.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"the UI automation failed: {ex} output: {ex.Message}.");
            }
            finally
            {
                // Add the following to make sure all processes and their children are stopped 
                Queue<Process> processes = new Queue<Process>();
                processes.Enqueue(serviceProcess!);
                processes.Enqueue(clientProcess!);
                processes.Enqueue(grpcProcess!);
                UiTestHelpers.KillProcessTrees(processes);

                // Stop tracing and export it into a zip archive.
                string path = UiTestHelpers.GetTracePath(_uiTestAssemblyLocation, TraceFileName);
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
