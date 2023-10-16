// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestPlatform.Utilities;
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

        /// <summary>
        /// Starts a process from an executable, sets its working directory and redirects its output to the test's output
        /// </summary>
        /// <param name="testAssemblyLocation">The path to the test's directory</param>
        /// <param name="appLocation">The path to the processes directory</param>
        /// <param name="executableName">The name of the executable that launches the process</param>
        /// <param name="portNumber">The port for the process to listen on</param>
        /// <returns></returns>
        public static Process? StartProcessLocally(string testAssemblyLocation, string appLocation, string executableName, string? portNumber = null)
        {
            string applicationWorkingDirectory = GetApplicationWorkingDirectory(testAssemblyLocation, appLocation);
            ProcessStartInfo processStartInfo = new ProcessStartInfo(applicationWorkingDirectory + executableName);
            processStartInfo.WorkingDirectory = applicationWorkingDirectory;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;

            if (!portNumber.IsNullOrEmpty())
            {
                processStartInfo.EnvironmentVariables["Kestrel:Endpoints:Https:Url"] = "https://*:" + portNumber;
            }
            return Process.Start(processStartInfo);
        }

        /// <summary>
        /// Builds the path to the process's directory
        /// </summary>
        /// <param name="testAssemblyLocation">The path to the test's directory</param>
        /// <param name="appLocation">The path to the processes directory</param>
        /// <returns></returns>
        private static string GetApplicationWorkingDirectory(string testAssemblyLocation, string appLocation)
        {
            string testedAppLocation = Path.Combine(Path.GetDirectoryName(testAssemblyLocation)!);
            // e.g. microsoft-identity-web\tests\IntegrationTests\WebAppUiTests\bin\Debug\net6.0
            string[] segments = testedAppLocation.Split(Path.DirectorySeparatorChar);
            int numberSegments = segments.Length;
            int startLastSegments = numberSegments - 3;
            int endFirstSegments = startLastSegments - 2;
            return Path.Combine(
                Path.Combine(segments.Take(endFirstSegments).ToArray()),
                appLocation,
                Path.Combine(segments.Skip(startLastSegments).ToArray())
            );
        }

        /// <summary>
        /// Kills the processes in the queue and all of their children
        /// </summary>
        /// <param name="processQueue">queue of parent processes</param>
        public static void killProcessTrees(Queue<Process> processQueue)
        {
            Process currentProcess;
            while (processQueue.Count > 0)
            {
                currentProcess = processQueue.Dequeue();
                if (currentProcess == null) { continue;}

                foreach (Process child in GetChildProcesses(currentProcess))
                {
                    processQueue.Enqueue(child);
                };
                currentProcess.Kill();
                currentProcess.Close();
            }
        }

        /// <summary>
        /// Gets the child processes of a process on Windows
        /// </summary>
        /// <param name="process">The parent process</param>
        /// <returns>A list of child processes</returns>
        /// <exception cref="NotImplementedException">Thrown if running on an OS other than Windows</exception>
        public static IList<Process> GetChildProcesses(this Process process)
        {
            // Validate platform compatibility
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new ManagementObjectSearcher(
                    $"Select * From Win32_Process Where ParentProcessID={process.Id}")
                    .Get()
                    .Cast<ManagementObject>()
#pragma warning disable CA1416 // This call can not be reached except on Windows due to the enclosing if statement
                    .Select(mo =>
                        Process.GetProcessById(Convert.ToInt32(mo["ProcessID"], System.Globalization.CultureInfo.InvariantCulture)))
#pragma warning restore CA1416
                    .ToList();
            }
            else
            {
                throw new NotImplementedException("Not implemented for this OS");
            }
        }

        /// <summary>
        /// Checks if all processes in a list are alive
        /// </summary>
        /// <param name="processes">List of processes to check</param>
        /// <returns>True if all are alive else false</returns>
        public static bool ProcessesAreAlive(List<Process> processes)
        {
            foreach (Process process in processes)
            {
                if (!ProcessIsAlive(process))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if a process is alive
        /// </summary>
        /// <param name="process">Process to check</param>
        /// <returns>True if alive false if not</returns>
        public static bool ProcessIsAlive(Process process)
        {
            if (process == null || process.HasExited)
            {
                return false;
            }
            return true;
        }

        public static void InstallPlaywrightBrowser()
        {
            var exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });
            if (exitCode != 0)
            {
                throw new Exception($"Playwright exited with code {exitCode}");
            }
        }
    }
}

