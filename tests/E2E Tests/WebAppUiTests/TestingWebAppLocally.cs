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

namespace WebAppUiTests;

#if !FROM_GITHUB_ACTION && !AZURE_DEVOPS_BUILD

// since this test changes environment variables we'd prefer it not run at the same time as other tests
[CollectionDefinition(nameof(UiTestNoParallelization), DisableParallelization = true)]
[Collection("WebAppUiTests")]
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
        Process? p = null;
        const string TraceFileName = TraceFileClassName + "_ValidEmailPassword";
        using IPlaywright playwright = await Playwright.CreateAsync();
        IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        await context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });

        try
        {
            p = UiTestHelpers.StartProcessLocally(_uiTestAssemblyLocation, _devAppPath, _devAppExecutable);
            await Task.Delay(5000); // Allow the web app time to start up.

            if (!UiTestHelpers.ProcessIsAlive(p)) { Assert.Fail(TC.WebAppCrashedString); }

            IPage page = await browser.NewPageAsync();
            await page.GotoAsync(UrlString);
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
            if (p != null) { processes.Enqueue(p); }
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
