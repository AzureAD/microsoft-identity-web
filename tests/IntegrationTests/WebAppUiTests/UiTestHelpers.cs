// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Playwright;

namespace WebAppUiTests
{
    public static class UiTestHelpers
    {
        public static async Task PerformLogin_MicrosoftIdentityFlow_ValidEmailPasswordCreds(IPage page, string email, string password, bool staySignedIn=false)
        {
            string StaySignedInText = staySignedIn ? "Yes" : "No";

            Trace.WriteLine($"Logging in ... Entering and submitting user name: {email}");
            ILocator emailInputLocator = page.GetByPlaceholder(TestConstants.EmailText);
            await FillEntryBox(emailInputLocator, email);

            // If using an account that has other non-password validation options, the below code should be uncommented
            /* Trace.WriteLine("Selecting \"Password\" as authentication method"); 
            await page.GetByRole(AriaRole.Button, new() { Name = TestConstants.PasswordText }).ClickAsync();*/

            Trace.WriteLine("Logging in ... entering and submitting password");
            ILocator passwordInputLocator = page.GetByPlaceholder(TestConstants.PasswordText);
            await FillEntryBox(passwordInputLocator, password);
            
            Trace.WriteLine($"Logging in ... Clicking {StaySignedInText} to signed in");
            await page.GetByRole(AriaRole.Button, new() { Name = StaySignedInText }).ClickAsync();
        }

        private static async Task FillEntryBox(ILocator entryBox, string entryText)
        {
            await entryBox.ClickAsync();
            await entryBox.FillAsync(entryText);
            await entryBox.PressAsync("Enter");
        }

        /// <summary>
        /// This starts the recording of playwright trace files. The corresponsing EndAndWritePlaywrightTrace method will also need to be used.
        /// This is not used anywhere by default and will need to be added to the code if desired
        /// </summary>
        /// <param name="page">The page object whose context the trace will record</param>
        /// <returns></returns>
        public static async Task StartPlaywrightTrace(IPage page) 
        {
            await page.Context.Tracing.StartAsync(new()
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });
        }

        /// <summary>
        /// This file gets written to the test's bin/debug/[dotnet version] folder
        /// Use the app at https://trace.playwright.dev/ to easily view the trace, all trace data is opened locally by the web app.
        /// </summary>
        /// <param name="page">The page object whose context is recording a trace</param>
        /// <returns>Nothing just creates the file</returns>
        public static async Task EndAndWritePlaywrightTrace(IPage page) 
        {
            await page.Context.Tracing.StopAsync(new()
            {
                Path = "PlaywrightTrace.zip"
            });
        }

        public static Process? StartWebAppLocally(string testAssemblyLocation, string appLocation, string executableName)
        {
            string testedAppLocation = Path.Combine(Path.GetDirectoryName(testAssemblyLocation)!);
            // e.g. microsoft-identity-web\tests\IntegrationTests\WebAppUiTests\bin\Debug\net6.0
            string[] segments = testedAppLocation.Split(Path.DirectorySeparatorChar);
            int numberSegments = segments.Length;
            int startLastSegments = numberSegments - 3;
            int endFirstSegments = startLastSegments - 2;
            string testedApplicationPath = Path.Combine(
                Path.Combine(segments.Take(endFirstSegments).ToArray()),
                appLocation,
                Path.Combine(segments.Skip(startLastSegments).ToArray()),
                executableName
            );
            ProcessStartInfo processStartInfo = new ProcessStartInfo(testedApplicationPath);
            return Process.Start(processStartInfo);
        }
    }
}

