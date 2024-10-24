// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Lab.Api;
using Microsoft.Playwright;
using Xunit;

namespace WebAppUiTests
{
#if !FROM_GITHUB_ACTION

    public class WebAppIntegrationTests
    {
        const string UrlString = "https://webapptestmsidweb.azurewebsites.net/MicrosoftIdentity/Account/signin";

        [Fact(Skip = "We cannot republish the web app atm. https://github.com/AzureAD/microsoft-identity-web/issues/984")]
        public async Task ChallengeUser_MicrosoftIdentityFlow_RemoteApp_ValidEmailPasswordCreds_SignInSucceedsTestAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }

            // Arrange
            using var playwright = await Playwright.CreateAsync();

            IBrowser browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            IPage page = await browser.NewPageAsync();
            await page.GotoAsync(UrlString);
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync();

            try
            {
                // Act
                Trace.WriteLine("Starting Playwright automation: web app sign-in & call Graph");
                string email = labResponse.User.Upn;
                await UiTestHelpers.FirstLogin_MicrosoftIdFlow_ValidEmailPasswordAsync(page, email, labResponse.User.GetOrFetchPassword());

                // Assert
                await Assertions.Expect(page.GetByText("Welcome")).ToBeVisibleAsync();
                await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync();
            }
            catch (Exception ex)
            {
                Assert.Fail($"The UI automation failed: {ex}");
            }

        }
#endif //FROM_GITHUB_ACTION
    }
}
