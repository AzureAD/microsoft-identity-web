// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using Microsoft.Identity.Web.Test.LabInfrastructure;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Playwright;
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

            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Edge.LaunchAsync();
            var page = await browser.NewPageAsync();
            await page.GotoAsync($"https://localhost:5001/MicrosoftIdentity/Account/signin");
            ILocator locator = page.Locater();

            try
            {
                // Act
                Trace.WriteLine("Starting Playwright automation: web app sign-in & call Graph");
                LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
                await WebAppIntegrationTests.PerformLogin(page, labResponse.User);

                // Assert
                Assert.Contains(labResponse.User.Upn, await page.InnerHTMLAsync(), System.StringComparison.OrdinalIgnoreCase);
                Assert.Contains(TestConstants.PhotoLabel, await page.InnerHTMLAsync(), System.StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Assert.Fail($"the UI automation failed: {ex}");
            }
            finally
            {
                await browser.CloseAsync();
                p.Kill(true);
            }
        }
    }
}
#endif //FROM_GITHUB_ACTION
