// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Identity.Lab.Api;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using TC = Microsoft.Identity.Web.Test.Common.TestConstants;

namespace WebAppUiTests
#if !FROM_GITHUB_ACTION
{
    // since these tests change environment variables we'd prefer it not run at the same time as other tests
    [CollectionDefinition(nameof(UiTestNoParallelization), DisableParallelization = true)]
    public class B2CWebAppCallsWebApiLocally : IClassFixture<InstallPlaywrightBrowserFixture>
    {
        private const string KeyvaultEmailName = "IdWeb-B2C-user";
        private const string KeyvaultPasswordName = "IdWeb-B2C-password";
        private const string KeyvaultClientSecretName = "IdWeb-B2C-Client-ClientSecret";
        private const string NameOfUser = "unknown";
        private const uint TodoListClientPort = 5000;
        private const uint TodoListServicePort = 44332;
        private const string TraceClassName = "B2CWebAppCallsWebApiLocally";
        private readonly LocatorAssertionsToBeVisibleOptions _assertVisibleOptions = new() { Timeout = 25000 };
        private readonly string _devAppPath = Path.Join("DevApps", "B2CWebAppCallsWebApi");
        private readonly Uri _keyvaultUri = new("https://webappsapistests.vault.azure.net");
        private readonly ITestOutputHelper _output;
        private readonly string _testAssemblyPath = typeof(B2CWebAppCallsWebApiLocally).Assembly.Location;

        public B2CWebAppCallsWebApiLocally(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public async Task Susi_B2C_LocalAccount_TodoAppFucntionsCorrectlyAsync()
        {
            // Web app and api environmental variable setup.
            DefaultAzureCredential azureCred = new();
            string clientSecret = await UiTestHelpers.GetValueFromKeyvaultWitDefaultCredsAsync(_keyvaultUri, KeyvaultClientSecretName, azureCred);
            var serviceEnvVars = new Dictionary<string, string>
            {
                {"ASPNETCORE_ENVIRONMENT", "Development" },
                {TC.KestrelEndpointEnvVar, TC.HttpStarColon + TodoListServicePort}
            };
            var clientEnvVars = new Dictionary<string, string>
            {
                {"ASPNETCORE_ENVIRONMENT", "Development"},
                {"AzureAdB2C__ClientSecret", clientSecret},
                {TC.KestrelEndpointEnvVar, TC.HttpsStarColon + TodoListClientPort}
            };

            // Get email and password from keyvault.
            string email = await UiTestHelpers.GetValueFromKeyvaultWitDefaultCredsAsync(_keyvaultUri, KeyvaultEmailName, azureCred);
            string password = await UiTestHelpers.GetValueFromKeyvaultWitDefaultCredsAsync(_keyvaultUri, KeyvaultPasswordName, azureCred);

            // Playwright setup. To see browser UI, set 'Headless = false'.
            const string TraceFileName = TraceClassName + "_TodoAppFunctionsCorrectly";
            using IPlaywright playwright = await Playwright.CreateAsync();
            IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
            await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });

            Process? serviceProcess= null;
            Process? clientProcess = null;

            try
            {
                // Start the web app and api processes.
                // The delay before starting client prevents transient devbox issue where the client fails to load the first time after rebuilding.
                serviceProcess = UiTestHelpers.StartProcessLocally(_testAssemblyPath, _devAppPath + TC.s_todoListServicePath, TC.s_todoListServiceExe, serviceEnvVars);
                await Task.Delay(3000);
                clientProcess = UiTestHelpers.StartProcessLocally(_testAssemblyPath, _devAppPath + TC.s_todoListClientPath, TC.s_todoListClientExe, clientEnvVars);

                if (!UiTestHelpers.ProcessesAreAlive(new List<Process>() { clientProcess, serviceProcess }))
                {
                    Assert.Fail(TC.WebAppCrashedString);
                }

                // Navigate to web app the retry logic ensures the web app has time to start up to establish a connection.
                IPage page = await context.NewPageAsync();
                uint InitialConnectionRetryCount = 5;
                while (InitialConnectionRetryCount > 0)
                {
                    try
                    {
                        await page.GotoAsync(TC.LocalhostUrl + TodoListClientPort);
                        break;
                    }
                    catch (PlaywrightException ex)
                    {
                        await Task.Delay(1000);
                        InitialConnectionRetryCount--;
                        if (InitialConnectionRetryCount == 0) { throw ex; }
                    }
                }
                LabResponse labResponse = await LabUserHelper.GetB2CLocalAccountAsync();

                // Initial sign in
                _output.WriteLine("Starting web app sign-in flow.");
                ILocator emailEntryBox = page.GetByPlaceholder("Email Address");
                await emailEntryBox.FillAsync(email);
                await emailEntryBox.PressAsync("Tab");
                await page.GetByPlaceholder("Password").FillAsync(password);
                await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
                await Assertions.Expect(page.GetByText("TodoList")).ToBeVisibleAsync(_assertVisibleOptions);
                await Assertions.Expect(page.GetByText(NameOfUser)).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app sign-in flow successful.");

                // Sign out
                _output.WriteLine("Starting web app sign-out flow.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Sign out" }).ClickAsync();
                _output.WriteLine("Signing out ...");
                await Assertions.Expect(page.GetByText("You have successfully signed out.")).ToBeVisibleAsync(_assertVisibleOptions);
                await Assertions.Expect(page.GetByText(NameOfUser)).ToBeHiddenAsync();
                _output.WriteLine("Web app sign out successful.");

                // Sign in again using Todo List button
                _output.WriteLine("Starting web app sign-in flow using Todo List button after sign out.");
                await page.GetByRole(AriaRole.Link, new() { Name = "TodoList" }).ClickAsync();
                await emailEntryBox.FillAsync(email);
                await emailEntryBox.PressAsync("Tab");
                await page.GetByPlaceholder("Password").FillAsync(password);
                await page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
                await Assertions.Expect(page.GetByText(NameOfUser).Nth(0)).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app sign-in flow successful using Todo List button after sign out.");

                // Create new todo item
                _output.WriteLine("Starting web app create new todo flow.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Create New" }).ClickAsync();
                ILocator titleEntryBox = page.GetByLabel("Title");
                await UiTestHelpers.FillEntryBoxAsync(titleEntryBox, TC.TodoTitle1);
                await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TC.TodoTitle1 })).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app create new todo flow successful.");

                // Edit todo item
                _output.WriteLine("Starting web app edit todo flow.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).Nth(3).ClickAsync();
                await titleEntryBox.ClickAsync();
                await titleEntryBox.FillAsync(TC.TodoTitle2);
                await titleEntryBox.PressAsync("Enter");
                await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TC.TodoTitle2 })).ToBeVisibleAsync(_assertVisibleOptions);
                _output.WriteLine("Web app edit todo flow successful.");

                // Delete todo item
                _output.WriteLine("Starting web app delete todo flow.");
                await page.GetByRole(AriaRole.Link, new() { Name = "Delete" }).Last.ClickAsync();
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
                Queue<Process> processes = new Queue<Process>();
                if (serviceProcess != null) { processes.Enqueue(serviceProcess); }
                if (clientProcess != null) { processes.Enqueue(clientProcess); }
                UiTestHelpers.KillProcessTrees(processes);

                // Stop tracing and export it into a zip archive.
                string path = UiTestHelpers.GetTracePath(_testAssemblyPath, TraceFileName);
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
