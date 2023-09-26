// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Lab.Api;
using Microsoft.Playwright;
using Xunit;

namespace WebAppUiTests;

#if !FROM_GITHUB_ACTION && !AZURE_DEVOPS_BUILD
public class TestingWebAppLocally
{
    private const string UrlString = "https://localhost:5001/MicrosoftIdentity/Account/signin";
    private const string DevAppPath = @"DevApps\WebAppCallsMicrosoftGraph";
    private const string DevAppExecutable = "WebAppCallsMicrosoftGraph.exe";
    private string UiTestAssemblyLocation = typeof(TestingWebAppLocally).Assembly.Location;

    [Fact]
    public async Task ChallengeUser_MicrosoftIdentityFlow_LocalApp_ValidEmailPasswordCreds_SignInSucceedsTestAsync()
    {
/*
        // Uncomment to initialise Playwright, which will download the browsers
        var exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });
        if (exitCode != 0)
        {
            throw new Exception($"Playwright exited with code {exitCode}");
        }
*/
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        { return; }

        // Arrange
        Process? p = UiTestHelpers.StartWebAppLocally(UiTestAssemblyLocation, DevAppPath, DevAppExecutable);

        if (p != null)
        {
            if (p.HasExited)
            {
                Assert.Fail($"Could not run web app locally.");
            }

            using var playwright = await Playwright.CreateAsync();
            IBrowser browser;
            browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            IPage page = await browser.NewPageAsync();
            await page.GotoAsync(UrlString);
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

            try
            {
                // Act
                Trace.WriteLine("Starting Playwright automation: web app sign-in & call Graph");
                string email = labResponse.User.Upn;
                await UiTestHelpers.FirstLogin_MicrosoftIdentityFlow_ValidEmailPassword(page, email, labResponse.User.GetOrFetchPassword());

                // Assert
                await Assertions.Expect(page.GetByText("Welcome")).ToBeVisibleAsync();
                await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync();
            }
            catch (Exception ex)
            {
                Assert.Fail($"the UI automation failed: {ex}");
            }
            finally
            {
                p.Kill(true);
            }
        }
    }
}
#endif //FROM_GITHUB_ACTION
