// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Lab.Api;
using Microsoft.Playwright;
using Xunit;

namespace WebAppUiTests;

#if !FROM_GITHUB_ACTION && !AZURE_DEVOPS_BUILD
public class TestingWebAppLocally
{
    const string UrlString = "https://localhost:5001/MicrosoftIdentity/Account/signin";

    [Theory]
    [InlineData(TestConstants.BrowserTypeEnum.CHROMIUM)]
    [InlineData(TestConstants.BrowserTypeEnum.FIREFOX)]
    public async Task ChallengeUser_MicrosoftIdentityFlow_LocalApp_ValidEmailPasswordCreds_SignInSucceedsTestAsync(TestConstants.BrowserTypeEnum browserType)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        { return; }

        // Arrange
        Process? p = StartWebAppLocally();
        if (p != null)
        {
            if (p.HasExited)
            {
                Assert.Fail($"Could not run web app locally.");
            }

            using var playwright = await Playwright.CreateAsync();
            IBrowser browser;
            switch (browserType)
            {
                case TestConstants.BrowserTypeEnum.CHROMIUM:
                    browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
                    break;

                case TestConstants.BrowserTypeEnum.FIREFOX:
                    browser = await playwright.Firefox.LaunchAsync(new() { Headless = true });
                    break;

                default:
                    Trace.WriteLine($"Testing for the {browserType} has not been implemented, defaulting to Chromium");
                    browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
                    break;
            }
            IPage page = await browser.NewPageAsync();
            await page.GotoAsync(UrlString);
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

            try
            {
                // Act
                Trace.WriteLine("Starting Playwright automation: web app sign-in & call Graph");
                string email = labResponse.User.Upn;
                string password = labResponse.User.GetOrFetchPassword();
                await UiTestHelpers.PerformLogin_MicrosoftIdentityFlow_ValidEmailPasswordCreds(page, email, password);

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

    private Process? StartWebAppLocally()
    {
        string uiTestAssemblyLocation = typeof(TestingWebAppLocally).Assembly.Location;
        // e.g. microsoft-identity-web\tests\IntegrationTests\WebAppUiTests\bin\Debug\net6.0\WebAppUiTests.dll
        string testedAppLocation = Path.Combine(Path.GetDirectoryName(uiTestAssemblyLocation)!);
        // e.g. microsoft-identity-web\tests\IntegrationTests\WebAppUiTests\bin\Debug\net6.0
        string[] segments = testedAppLocation.Split(Path.DirectorySeparatorChar);
        int numberSegments = segments.Length;
        int startLastSegments = numberSegments - 3;
        int endFirstSegments = startLastSegments - 2;
        string testedApplicationPath = Path.Combine(
            Path.Combine(segments.Take(endFirstSegments).ToArray()),
            @"DevApps\WebAppCallsMicrosoftGraph",
            Path.Combine(segments.Skip(startLastSegments).ToArray()),
            "WebAppCallsMicrosoftGraph.exe");

        ProcessStartInfo processStartInfo = new ProcessStartInfo(testedApplicationPath);
        return Process.Start(processStartInfo);
    }
}
#endif //FROM_GITHUB_ACTION
