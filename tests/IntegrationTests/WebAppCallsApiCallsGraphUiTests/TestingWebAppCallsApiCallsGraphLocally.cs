// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Lab.Api;
using Microsoft.Playwright;
using WebAppUiTests;
using Xunit;
using Xunit.Abstractions;
using Process = System.Diagnostics.Process;

namespace WebAppCallsApiCallsGraphUiTests
{
#if !FROM_GITHUB_ACTION
    public class TestingWebAppCallsApiCallsGraphLocally
    {

        private const string UrlString = @"https://localhost:44321";
        private const string DevAppPath = @"DevApps\WebAppCallsWebApiCallsGraph";
        private const string TodoListServicePath = @"\TodoListService";
        private const string TodoListClientPath = @"\Client";
        private const string GrpcPath = @"\gRPC";
        private const string TodoListServiceExecutable = @"\TodoListService.exe";
        private const string TodoListClientExecutable = @"\TodoListClient.exe";
        private const string GrpcExecutable = @"\grpc.exe";
        private const string SignOutPagePath = @"/MicrosoftIdentity/Account/SignedOut";
        private const string TodoTitle1 = "Testing create todo item";
        private const string TodoTitle2 = "Testing edit todo item";
        private string UiTestAssemblyLocation = typeof(TestingWebAppCallsApiCallsGraphLocally).Assembly.Location;
        private readonly ITestOutputHelper _output;

        public TestingWebAppCallsApiCallsGraphLocally(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task ChallengeUser_MicrosoftIdentityFlow_LocalApp_ValidEmailPasswordCreds_TodoAppFunctionsCorrectly()
        {
            // Arrange web app setup
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Process? grpcProcess = UiTestHelpers.StartWebAppLocally(UiTestAssemblyLocation, DevAppPath + GrpcPath, GrpcExecutable, "5001");
            Process? clientProcess = UiTestHelpers.StartWebAppLocally(UiTestAssemblyLocation, DevAppPath + TodoListClientPath, TodoListClientExecutable, "44321");
            Process? serviceProcess = UiTestHelpers.StartWebAppLocally(UiTestAssemblyLocation, DevAppPath + TodoListServicePath, TodoListServiceExecutable, "44351");
            try
            {
                if (clientProcess != null && serviceProcess != null && grpcProcess != null)
                {
                    if (clientProcess.HasExited || serviceProcess.HasExited || grpcProcess.HasExited)
                    {
                        Assert.Fail($"Could not run web app locally.");
                    }

                    // Arrange Playwright setup
                    using var playwright = await Playwright.CreateAsync();
                    IBrowser browser;
                    browser = await playwright.Chromium.LaunchAsync(new() { Headless = true }); // To see the browser UI, set Headless = false
                    IPage page = await browser.NewPageAsync();
                    await page.GotoAsync(UrlString);
                    LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

                    // Initial sign in
                    _output.WriteLine("Starting web app sign-in flow");
                    string email = labResponse.User.Upn;
                    await UiTestHelpers.FirstLogin_MicrosoftIdentityFlow_ValidEmailPassword(page, email, labResponse.User.GetOrFetchPassword(), _output);
                    await Assertions.Expect(page.GetByText("TodoList")).ToBeVisibleAsync();
                    await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync();
                    _output.WriteLine("Web app sign-in flow successful");

                    // Sign out
                    _output.WriteLine("Starting web app sign-out flow");
                    await page.GetByRole(AriaRole.Link, new() { Name = "Sign out" }).ClickAsync();
                    await UiTestHelpers.PerformSignOut_MicrosoftIdentityFlow(page, email, UrlString + SignOutPagePath, _output);
                    _output.WriteLine("Web app sign out successful");

                    // Sign in again using Todo List button
                    _output.WriteLine("Starting web app sign-in flow using Todo List button after sign out");
                    await page.GetByRole(AriaRole.Link, new() { Name = "TodoList" }).ClickAsync();
                    await UiTestHelpers.SuccessiveLogin_MicrosoftIdentityFlow_ValidEmailPassword(page, email, labResponse.User.GetOrFetchPassword(), _output);
                    var TodoLink =  page.GetByRole(AriaRole.Link, new() { Name = "Create New" });
                    await Assertions.Expect(TodoLink).ToBeVisibleAsync();
                    _output.WriteLine("Web app sign-in flow successful using Todo List button after sign out");

                    // Create new todo item
                    _output.WriteLine("Starting web app create new todo flow");
                    await TodoLink.ClickAsync();
                    var TitleEntryBox = page.GetByLabel("Title");
                    await UiTestHelpers.FillEntryBox(TitleEntryBox, TodoTitle1);
                    await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TodoTitle1 })).ToBeVisibleAsync();
                    _output.WriteLine("Web app create new todo flow successful");

                    // Edit todo item
                    _output.WriteLine("Starting web app edit todo flow");
                    await page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync();
                    await UiTestHelpers.FillEntryBox(TitleEntryBox, TodoTitle2);
                    await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TodoTitle2 })).ToBeVisibleAsync();
                    _output.WriteLine("Web app edit todo flow successful");

                    // Delete todo item
                    _output.WriteLine("Starting web app delete todo flow");
                    await page.GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
                    await page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
                    await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TodoTitle2 })).Not.ToBeVisibleAsync();
                    _output.WriteLine("Web app delete todo flow successful");

                }
            }
            catch (System.Exception ex)
            {
                Assert.Fail($"the UI automation failed: {ex} output: {ex.Message}");
            }
            finally
            {
                //add the following to make sure sockets are unbound 
                Queue<Process> processes = new Queue<Process>();
                processes.Enqueue(serviceProcess);
                processes.Enqueue(clientProcess);
                processes.Enqueue(grpcProcess);
                UiTestHelpers.killProcessTrees(processes);
            }
        }
    }
#endif // !FROM_GITHUB_ACTION
}
