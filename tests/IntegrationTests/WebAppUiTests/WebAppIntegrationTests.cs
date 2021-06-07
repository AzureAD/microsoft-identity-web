// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.LabInfrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
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

            ChromeOptions options = new ChromeOptions();
            // ~2x faster, no visual rendering
            // comment-out below when debugging to see the UI automation
            options.AddArguments(TestConstants.Headless);
            using IWebDriver driver = new ChromeDriver(options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);

            // Act
            Trace.WriteLine("Starting Selenium automation: web app sign-in & call Graph");
            driver.Navigate()
               .GoToUrl("https://webapptestmsidweb.azurewebsites.net/MicrosoftIdentity/Account/signin");
            PerformLogin(driver, labResponse.User);

            // Assert
            Assert.Contains(labResponse.User.Upn, driver.PageSource, System.StringComparison.OrdinalIgnoreCase);
            Assert.Contains(TestConstants.PhotoLabel, driver.PageSource, System.StringComparison.OrdinalIgnoreCase);
            driver.Quit();
            driver.Dispose();
        }

        private static void PerformLogin(
            IWebDriver driver,
            LabUser user)
        {
            UserInformationFieldIds fields = new UserInformationFieldIds();

            EnterUsername(driver, user, fields);
            EnterPassword(driver, user, fields);
            HandleStaySignedInPrompt(driver);
        }

        private static void EnterUsername(
            IWebDriver driver,
            LabUser user,
            UserInformationFieldIds fields)
        {
            // Lab user needs to be a guest in the msidentity-samples-testing tenant
            Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Logging in ... Entering user name: {0}", user.Upn));

            driver.FindElement(By.Id(fields.AADUsernameInputId)).SendKeys(user.Upn.Contains("EXT", System.StringComparison.OrdinalIgnoreCase) ? user.HomeUPN : user.Upn);

            Trace.WriteLine("Logging in ... Clicking <Next> after user name");

            driver.FindElement(By.Id(fields.AADSignInButtonId)).Click();
        }

        private static void EnterPassword(
            IWebDriver driver,
            LabUser user,
            UserInformationFieldIds fields)
        {
            Trace.WriteLine("Logging in ... Entering password");
            string password = user.GetOrFetchPassword();
            string passwordField = fields.GetPasswordInputId();
            driver.FindElement(By.Id(passwordField)).SendKeys(password);

            Trace.WriteLine("Logging in ... Clicking next after password");
            driver.FindElement(By.Id(fields.GetPasswordSignInButtonId())).Click();
        }

        private static void HandleStaySignedInPrompt(IWebDriver driver)
        {
            Trace.WriteLine("Logging in ... Clicking 'No' for staying signed in");
            var acceptBtn = driver.FindElement(By.Id(TestConstants.StaySignedInNoId));
            acceptBtn?.Click();
        }
    }
#endif //FROM_GITHUB_ACTION
}
