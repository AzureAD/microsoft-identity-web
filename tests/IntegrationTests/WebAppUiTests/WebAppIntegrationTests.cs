// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.LabInfrastructure;
using Microsoft.Playwright;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace WebAppUiTests
{
#if !FROM_GITHUB_ACTION
    public class WebAppIntegrationTests
    {
        [Fact(Skip = "We cannot republish the web app atm. https://github.com/AzureAD/microsoft-identity-web/issues/984")]
        public async Task ChallengeUser_SignInSucceedsTestAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }

            // Arrange
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

            var options = new BrowserTypeLaunchOptions { Headless = false };
            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(options);
            var page = await browser.NewPageAsync();
            ILocator locator = page.Locator("//body");

            // Act
            Trace.WriteLine("Starting Playwright automation: web app sign-in & call Graph");
            await page.GotoAsync("https://webapptestmsidweb.azurewebsites.net/MicrosoftIdentity/Account/signin");
            await LoginMethods.PerformLogin(page, labResponse.User);

            // Assert
            var pageContent = await locator.InnerHTMLAsync();
            Assert.Contains(labResponse.User.Upn, pageContent, System.StringComparison.OrdinalIgnoreCase);
            Assert.Contains(TestConstants.PhotoLabel, pageContent, StringComparison.OrdinalIgnoreCase);
        }
#endif //FROM_GITHUB_ACTION
    }
    public class LoginMethods
    { 
        protected static async Task PerformLogin(IPage page, LabUser user)
        {
            const string EmailEntryPlaceholderText = "email";
            const string PasswordEntryPlaceholderText = "password";

            ILocator emailInputLocator = page.GetByPlaceholder(EmailEntryPlaceholderText);
            Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Logging in ... Entering user name: {0}", user.Upn));
            await emailInputLocator.ClickAsync();
            await emailInputLocator.FillAsync(user.Upn);
            

            Trace.WriteLine("Logging in ... submitting email input");
            await emailInputLocator.PressAsync("Enter");

            Trace.WriteLine("Selecting \"Password\" as authentication method");
            await page.GetByRole(AriaRole.Button, new() { Name = "Password" }).ClickAsync();

            Trace.WriteLine("Logging in ... entering password");
            var password = user.GetOrFetchPassword();
            ILocator passwordInputLocator = page.GetByPlaceholder(PasswordEntryPlaceholderText);
            await passwordInputLocator.ClickAsync();
            await passwordInputLocator.FillAsync(password);

            Trace.WriteLine("Logging in ... submitting password input");
            await passwordInputLocator.PressAsync("Enter");
            
            Trace.WriteLine("Logging in ... Clicking 'No' for staying signed in");
            await page.GetByRole(AriaRole.Button, new() { Name = "No" }).ClickAsync();
        }

    }
}
