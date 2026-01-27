// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Playwright;
using Xunit;

namespace WebAppUiTests;
#if !FROM_GITHUB_ACTION && !NET10_0 // NET 10 is temporary and will be removed before using official release of .NET 10

[Collection(nameof(UiTestNoParallelization))]
public class WebAppIntegrationTests
{
    const string UrlString = "https://webapptestmsidweb.azurewebsites.net/MicrosoftIdentity/Account/signin";

    [Fact(Skip = "We cannot republish the web app atm. https://github.com/AzureAD/microsoft-identity-web/issues/984")]
    public async Task ChallengeUser_MicrosoftIdentityFlow_RemoteApp_ValidEmailPasswordCreds_SignInSucceedsTestAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }

        // Arrange
        using var playwright = await Playwright.CreateAsync();

        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync(UrlString);
        var userConfig = await LabResponseHelper.GetUserConfigAsync("MSAL-User-Default-JSON");

        try
        {
            // Act
            Trace.WriteLine("Starting Playwright automation: web app sign-in & call Graph");
            string email = userConfig.UPN;
            await UiTestHelpers.FirstLogin_MicrosoftIdFlow_ValidEmailPasswordAsync(page, email, LabResponseHelper.FetchUserPassword(userConfig.LabName));

            // Assert
            await Assertions.Expect(page.GetByText("Welcome")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync();
        }
        catch (Exception ex)
        {
            Assert.Fail($"The UI automation failed: {ex}");
        }

    }
}
#endif //FROM_GITHUB_ACTION
