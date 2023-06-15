// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;
using System.Threading.Tasks;
using OpenQA.Selenium;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using Microsoft.Identity.Web.Test.LabInfrastructure;
using Microsoft.Identity.Web.Test.Common;
using OpenQA.Selenium.Edge;
using System.IO;
using System.Linq;

namespace WebAppUiTests;

#if !FROM_GITHUB_ACTION && !AZURE_DEVOPS_BUILD
public class TestingWebAppLocally 
{
    [Fact]
    public async Task ChallengeUser_SignInSucceedsTestAsync_LocalHttp()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        { return; }

        string uiTestAssemblyLocation = typeof(TestingWebAppLocally).Assembly.Location;
        // e.g. C:\gh\microsoft-identity-web\tests\IntegrationTests\WebAppUiTests\bin\Debug\net6.0\WebAppUiTests.dll
        string testedAppLocation = Path.Combine(Path.GetDirectoryName(uiTestAssemblyLocation)!);
        // e.g. C:\gh\microsoft-identity-web\tests\IntegrationTests\WebAppUiTests\bin\Debug\net6.0
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
        Process? p = Process.Start(processStartInfo);
        if (p != null)
        {
            if (p.HasExited)
            {
                Assert.Fail($"Could not run {testedApplicationPath}.");
            }

            EdgeOptions edgeOptions = new EdgeOptions();
            edgeOptions.AddArgument("-inprivate");
            // ~2x faster, no visual rendering
            // comment-out below when debugging to see the UI automation
            edgeOptions.AddArgument(TestConstants.Headless);
            using IWebDriver driver = new EdgeDriver(edgeOptions);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);

            try
            {
                // Act
                Trace.WriteLine("Starting Selenium automation: web app sign-in & call Graph");
                driver.Navigate()
                   .GoToUrl($"https://localhost:5001/MicrosoftIdentity/Account/signin");
                LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
                WebAppIntegrationTests.PerformLogin(driver, labResponse.User);

                // Assert
                Assert.Contains(labResponse.User.Upn, driver.PageSource, System.StringComparison.OrdinalIgnoreCase);
                Assert.Contains(TestConstants.PhotoLabel, driver.PageSource, System.StringComparison.OrdinalIgnoreCase);
                driver.Quit();
                driver.Dispose();
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
