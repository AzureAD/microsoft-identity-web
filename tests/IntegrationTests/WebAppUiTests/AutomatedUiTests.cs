// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace WebAppUiTests
{
    public class AutomatedUiTests
    {
        public AutomatedUiTests()
        {
            ChromeOptions options = new ChromeOptions();
            _driver = new ChromeDriver(options);
        }

        private readonly IWebDriver _driver;

        [Fact]
        public void Test1()
        {
            _driver.Navigate()
                .GoToUrl("https://webapptestmsidweb.azurewebsites.net/MicrosoftIdentity/Account/signin");
        }
    }
}
