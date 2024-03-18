// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.Identity.Lab.Api;
using TC = Microsoft.Identity.Web.Test.Common.TestConstants;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using System.Threading;

namespace WebAppUiTests;

#if !FROM_GITHUB_ACTION && !AZURE_DEVOPS_BUILD

// Since this test affects Kestrel environment variables it can cause a race condition when run in parallel with other UI tests.
[CollectionDefinition(nameof(UiTestNoParallelization), DisableParallelization = true)]
public class TestingWebAppLocally : IClassFixture<InstallPlaywrightBrowserFixture>
{
    private const string UrlString = "https://localhost:5001/MicrosoftIdentity/Account/signin";
    private const string TraceFileClassName = "TestingWebAppLocally";
    private readonly ITestOutputHelper _output;
    private readonly string _devAppExecutable = Path.DirectorySeparatorChar.ToString() + "WebAppCallsMicrosoftGraph.exe";
    private readonly string _devAppPath = "DevApps" + Path.DirectorySeparatorChar.ToString() + "WebAppCallsMicrosoftGraph";
    private readonly string _uiTestAssemblyLocation = typeof(TestingWebAppLocally).Assembly.Location;
    private readonly LocatorAssertionsToBeVisibleOptions _assertVisibleOptions = new() { Timeout = 15000 };

    public TestingWebAppLocally(ITestOutputHelper output) 
    {
        _output = output;
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task ChallengeUser_MicrosoftIdFlow_LocalApp_ValidEmailPassword()
    {
        // Arrange
        Process? process = null;
        const string TraceFileName = TraceFileClassName + "_ValidEmailPassword";
        using IPlaywright playwright = await Playwright.CreateAsync();
        IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });

        try
        {
            process = UiTestHelpers.StartProcessLocally(_uiTestAssemblyLocation, _devAppPath, _devAppExecutable);

            if (!UiTestHelpers.ProcessIsAlive(process)) { Assert.Fail(TC.WebAppCrashedString); }

            IPage page = await browser.NewPageAsync();

            // The retry logic ensures the web app has time to start up to establish a connection.
            uint InitialConnectionRetryCount = 5;
            while (InitialConnectionRetryCount > 0)
            {
                try
                {
                    await page.GotoAsync(UrlString);
                    break;
                }
                catch (PlaywrightException ex)
                {
                    await Task.Delay(1000);
                    InitialConnectionRetryCount--;
                    if (InitialConnectionRetryCount == 0) { throw ex; }
                }
            }

            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

            // Act
            Trace.WriteLine("Starting Playwright automation: web app sign-in & call Graph.");
            string email = labResponse.User.Upn;
            await UiTestHelpers.FirstLogin_MicrosoftIdFlow_ValidEmailPassword(page, email, labResponse.User.GetOrFetchPassword(), _output);

            // Assert
            await Assertions.Expect(page.GetByText("Welcome")).ToBeVisibleAsync(_assertVisibleOptions);
            await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync(_assertVisibleOptions);
        }
        catch (Exception ex)
        {
            Assert.Fail($"the UI automation failed: {ex} output: {ex.Message}");
        }
        finally
        {
            // Cleanup the web app process and any child processes
            Queue<Process> processes = new();
            if (process != null) { processes.Enqueue(process); }
            UiTestHelpers.KillProcessTrees(processes);

            // Cleanup Playwright
            // Stop tracing and export it into a zip archive.
            string path = UiTestHelpers.GetTracePath(_uiTestAssemblyLocation, TraceFileName);
            await context.Tracing.StopAsync(new() { Path = path });
            _output.WriteLine($"Trace data for {TraceFileName} recorded to {path}.");
            await browser.DisposeAsync();
            playwright.Dispose();
        }
    }
}
#endif //FROM_GITHUB_ACTION
