// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Lab.Api;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Playwright;
using Xunit;

namespace WebAppUiTests
{
#if !FROM_GITHUB_ACTION

    public class WebAppIntegrationTests
    {
        const string UrlString = "https://webapptestmsidweb.azurewebsites.net/MicrosoftIdentity/Account/signin";

        [Theory(Skip = "We cannot republish the web app atm. https://github.com/AzureAD/microsoft-identity-web/issues/984")]
        [InlineData(TestConstants.BrowserTypeEnum.CHROMIUM)]
        [InlineData(TestConstants.BrowserTypeEnum.FIREFOX)]
        public async Task ChallengeUser_MicrosoftIdentityFlow_RemoteApp_ValidEmailPasswordCreds_SignInSucceedsTestAsync(TestConstants.BrowserTypeEnum browserType)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }

            // Arrange
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

        }
#endif //FROM_GITHUB_ACTION
    }
}
