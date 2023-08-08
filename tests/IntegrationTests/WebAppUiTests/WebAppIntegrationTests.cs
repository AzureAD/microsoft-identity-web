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

            var options = new BrowserTypeLaunchOptions { Headless = true };
            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(options);
            var page = await browser.NewPageAsync();
            ILocator locator = page.Locator("//body");

            // Act
            Trace.WriteLine("Starting Playwright automation: web app sign-in & call Graph");
            await page.GotoAsync("https://webapptestmsidweb.azurewebsites.net/MicrosoftIdentity/Account/signin");
            await PerformLogin(page, labResponse.User);

            // Assert
            var pageContent = await locator.InnerHTMLAsync();
            Assert.Contains(labResponse.User.Upn, pageContent, System.StringComparison.OrdinalIgnoreCase);
            Assert.Contains(TestConstants.PhotoLabel, pageContent, StringComparison.OrdinalIgnoreCase);
        }
        protected static async Task PerformLogin(
        IPage page, LabUser user)
        {
            var fields = new UserInformationFieldIds();

            Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Logging in ... Entering user name: {0}", user.Upn));
            await page.TypeAsync($"#{fields.AADUsernameInputId}", user.Upn);

            Trace.WriteLine("Logging in ... Clicking <Next> after user name");
            await page.ClickAsync("button[type='submit'][name='Next']");

            Trace.WriteLine("Logging in ... Entering password");
            var password = user.GetOrFetchPassword();
            await page.TypeAsync(fields.GetPasswordSignInButtonId(), password);
/*            await WaitUntilWithRetryAsync(page, fields.GetPasswordInputId()).TypeAsync(password);*/

            // Wait a bit for the dialog to be refreshed before attempting
            // to get the button (who is already there, inactive, but will be stale
            // when the dialog refreshes.
/*            await Task.Delay(1000);

            Trace.WriteLine("Logging in ... Clicking next after password");
            await WaitUntilWithRetryAsync(page, fields.GetPasswordSignInButtonId())
               .ClickAsync(locator);

            Trace.WriteLine("Logging in ... Clicking 'No' for staying signed in");
            await WaitUntilWithRetryAsync(page, TestConstants.StaySignedInNoId)
                .ClickAsync();
        }*/

/*        private static async Task<IElementHandle> WaitUntilWithRetryAsync(IPage page, string elementName)
        {
            var wait = new WaitForSelectorOptions() { State = WaitForSelectorState.Attached, Timeout = 10000 };
            try
            {
                return await page.WaitForSelectorAsync($"#{elementName}", wait);
            }
            catch (Exception)
            {
                return await page.WaitForSelectorAsync($"#{elementName}", wait);
            }
        }*/
    }
#endif //FROM_GITHUB_ACTION
}
