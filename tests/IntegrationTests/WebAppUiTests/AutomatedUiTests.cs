// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Identity.Web.Test.LabInfrastructure;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace WebAppUiTests
{
    public class AutomatedUiTests
    {
        [Fact]
        public async Task ChallengeUser_SignInSucceedsTestAsync()
        {
            // Arrange
            ChromeOptions options = new ChromeOptions();
            IWebDriver driver = new ChromeDriver(options);
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

            Trace.WriteLine("Starting Selenium automation: web app sign-in & call Graph");
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

            // Act
            driver.Navigate()
               .GoToUrl("https://webapptestmsidweb.azurewebsites.net/MicrosoftIdentity/Account/signin");
            PerformLogin(driver, labResponse);

            // Assert
            Assert.Contains("Hello idlab1@msidlab4.onmicrosoft.com!", driver.PageSource);
            Assert.Contains("photo", driver.PageSource);
            driver.Dispose();
        }

        private static void PerformLogin(
            IWebDriver driver,
            LabResponse response)
        {
            UserInformationFieldIds fields = new UserInformationFieldIds();

            EnterUsername(driver, response.User, fields);
            EnterPassword(driver, response.User, fields);
            HandleStaySignedInPrompt(driver);
        }

        private static void EnterUsername(
            IWebDriver driver,
            LabUser user,
            UserInformationFieldIds fields)
        {
            Trace.WriteLine(string.Format("Logging in ... Entering user name: {0}", user.Upn));

            driver.FindElement(By.Id(fields.AADUsernameInputId)).SendKeys(user.Upn.Contains("EXT") ? user.HomeUPN : user.Upn);

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
}
