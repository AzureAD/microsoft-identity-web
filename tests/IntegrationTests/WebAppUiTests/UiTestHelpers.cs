// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace WebAppUiTests
{
    public static class UiTestHelpers
    {
        public static async Task FirstLogin_MicrosoftIdentityFlow_ValidEmailPassword(IPage page, string email, string password, ITestOutputHelper? output=null ,bool staySignedIn=false)
        {
            string staySignedInText = staySignedIn ? "Yes" : "No";

            WriteLine(output, $"Logging in ... Entering and submitting user name: {email}");
            ILocator emailInputLocator = page.GetByPlaceholder(TestConstants.EmailText);
            await FillEntryBox(emailInputLocator, email);
            await EnterPassword_MicrosoftIdentityFlow_ValidPassword(page, password, staySignedInText);
        }

        public static async Task SuccessiveLogin_MicrosoftIdentityFlow_ValidEmailPassword(IPage page, string email, string password, ITestOutputHelper? output = null, bool staySignedIn = false)
        {
            string staySignedInText = staySignedIn ? "Yes" : "No";

            WriteLine(output, $"Logging in again in this browsing session... selecting user via email: {email}");
            await SelectKnownAccountByEmail_MicrosoftIdentityFlow(page, email);
            await EnterPassword_MicrosoftIdentityFlow_ValidPassword(page, password, staySignedInText);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="page"></param>
        /// <param name="email"></param>
        /// <param name="signOutPageUrl"></param>
        /// <returns></returns>
        public static async Task PerformSignOut_MicrosoftIdentityFlow(IPage page, string email, string signOutPageUrl, ITestOutputHelper? output = null)
        {
            WriteLine(output, "Signing out ...");
            await SelectKnownAccountByEmail_MicrosoftIdentityFlow(page, email);
            await page.WaitForURLAsync(signOutPageUrl);
            WriteLine(output, "Sign out page successfully reached");
        }

        /// <summary>
        /// In the Microsoft Identity flow, the user is at certain stages presented with a list of accounts known in 
        /// the current browsing session to choose from. This method selects the account using the user's email.
        /// </summary>
        /// <param name="page">page for the playwright browser</param>
        /// <param name="email">user email address to select</param>
        private static async Task SelectKnownAccountByEmail_MicrosoftIdentityFlow(IPage page, string email)
        {
            await page.Locator($"[data-test-id=\"{email}\"]").ClickAsync();
        }

        public static async Task EnterPassword_MicrosoftIdentityFlow_ValidPassword(IPage page, string password, string staySignedInText, ITestOutputHelper? output = null)
        {
            // If using an account that has other non-password validation options, the below code should be uncommented
            /* WriteLine(output, "Selecting \"Password\" as authentication method"); 
            await page.GetByRole(AriaRole.Button, new() { Name = TestConstants.PasswordText }).ClickAsync();*/

            WriteLine(output, "Logging in ... entering and submitting password");
            ILocator passwordInputLocator = page.GetByPlaceholder(TestConstants.PasswordText);
            await FillEntryBox(passwordInputLocator, password);

            WriteLine(output, $"Logging in ... Clicking {staySignedInText} on whether the browser should stay signed in");
            await page.GetByRole(AriaRole.Button, new() { Name = staySignedInText }).ClickAsync();
        }

        public static async Task FillEntryBox(ILocator entryBox, string entryText)
        {
            await entryBox.ClickAsync();
            await entryBox.FillAsync(entryText);
            await entryBox.PressAsync("Enter");
        }
        private static void WriteLine(ITestOutputHelper? output, string message)
        {
            if (output != null)
            {
                output.WriteLine(message);
            }
            else
            {
                Trace.WriteLine(message);
            }
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
            processStartInfo.UseShellExecute = true;
            processStartInfo.Verb = "runas";
            return Process.Start(processStartInfo);
        }
    }
}

