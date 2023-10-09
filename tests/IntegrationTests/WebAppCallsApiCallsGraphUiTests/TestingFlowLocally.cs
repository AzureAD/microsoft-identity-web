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
using Castle.Core.Internal;
using Microsoft.Identity.Lab.Api;
using Microsoft.Playwright;
using WebAppUiTests;
using Xunit;
using Xunit.Abstractions;
using Process = System.Diagnostics.Process;

namespace WebAppCallsApiCallsGraphUiTests
{
    public class TestingFlowLocally
    {
#if !FROM_GITHUB_ACTION
        private const string UrlString = @"https://localhost:44321";
        private const string DevAppPath = @"DevApps\WebAppCallsWebApiCallsGraph";
        private const string TodoListServicePath = @"\TodoListService";
        private const string TodoListClientPath = @"\Client";
        private const string GrpcPath = @"\gRPC";
        private const string TodoListServiceExecutable = @"\TodoListService.exe";
        private const string TodoListClientExecutable = @"\TodoListClient.exe";
        private const string GrpcExecutable = @"\grpc.exe";
        private const string SignOutPagePath = @"/MicrosoftIdentity/Account/SignedOut";
        private const string TodoTitle1 = "Testing create todo item";
        private const string TodoTitle2 = "Testing edit todo item";
        private string UiTestAssemblyLocation = typeof(TestingFlowLocally).Assembly.Location;
        private readonly ITestOutputHelper _output;


        public TestingFlowLocally(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task ChallengeUser_MicrosoftIdentityFlow_LocalApp_ValidEmailPasswordCreds_TodoAppFunctionsCorrectly()
        {
            // Arrange web app setup
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Process? grpcProcess = TestingFlowLocally.StartWebAppLocally(UiTestAssemblyLocation, DevAppPath + GrpcPath, GrpcExecutable, "5001");
            Process? clientProcess = TestingFlowLocally.StartWebAppLocally(UiTestAssemblyLocation, DevAppPath + TodoListClientPath, TodoListClientExecutable, "44321");
            Process? serviceProcess = TestingFlowLocally.StartWebAppLocally(UiTestAssemblyLocation, DevAppPath + TodoListServicePath, TodoListServiceExecutable, "44351");
            try
            {
                if (clientProcess != null && serviceProcess != null && grpcProcess != null)
                {
                    if (clientProcess.HasExited || serviceProcess.HasExited || grpcProcess.HasExited)
                    {
                        Assert.Fail($"Could not run web app locally.");
                    }

                    // Arrange Playwright setup
                    using var playwright = await Playwright.CreateAsync();
                    IBrowser browser;
                    browser = await playwright.Chromium.LaunchAsync(new() { Headless = true }); // To see the browser UI, set Headless = false
                    IPage page = await browser.NewPageAsync();
                    await page.GotoAsync(UrlString);
                    LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

                    // Initial sign in
                    _output.WriteLine("Starting web app sign-in flow");
                    string email = labResponse.User.Upn;
                    await UiTestHelpers.FirstLogin_MicrosoftIdentityFlow_ValidEmailPassword(page, email, labResponse.User.GetOrFetchPassword(), _output);
                    await Assertions.Expect(page.GetByText("TodoList")).ToBeVisibleAsync();
                    await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync();
                    _output.WriteLine("Web app sign-in flow successful");

                    // Sign out
                    _output.WriteLine("Starting web app sign-out flow");
                    await page.GetByRole(AriaRole.Link, new() { Name = "Sign out" }).ClickAsync();
                    await UiTestHelpers.PerformSignOut_MicrosoftIdentityFlow(page, email, UrlString + SignOutPagePath, _output);
                    _output.WriteLine("Web app sign out successful");

                    // Sign in again using Todo List button
                    _output.WriteLine("Starting web app sign-in flow using Todo List button after sign out");
                    await page.GetByRole(AriaRole.Link, new() { Name = "TodoList" }).ClickAsync();
                    await UiTestHelpers.SuccessiveLogin_MicrosoftIdentityFlow_ValidEmailPassword(page, email, labResponse.User.GetOrFetchPassword(), _output);
                    var TodoLink =  page.GetByRole(AriaRole.Link, new() { Name = "Create New" });
                    await Assertions.Expect(TodoLink).ToBeVisibleAsync();
                    _output.WriteLine("Web app sign-in flow successful using Todo List button after sign out");

                    // Create new todo item
                    _output.WriteLine("Starting web app create new todo flow");
                    await TodoLink.ClickAsync();
                    var TitleEntryBox = page.GetByLabel("Title");
                    await UiTestHelpers.FillEntryBox(TitleEntryBox, TodoTitle1);
                    await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TodoTitle1 })).ToBeVisibleAsync();
                    _output.WriteLine("Web app create new todo flow successful");

                    // Edit todo item
                    _output.WriteLine("Starting web app edit todo flow");
                    await page.GetByRole(AriaRole.Link, new() { Name = "Edit" }).ClickAsync();
                    await UiTestHelpers.FillEntryBox(TitleEntryBox, TodoTitle2);
                    await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TodoTitle2 })).ToBeVisibleAsync();
                    _output.WriteLine("Web app edit todo flow successful");

                    // Delete todo item
                    _output.WriteLine("Starting web app delete todo flow");
                    await page.GetByRole(AriaRole.Link, new() { Name = "Delete" }).ClickAsync();
                    await page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
                    await Assertions.Expect(page.GetByRole(AriaRole.Cell, new() { Name = TodoTitle2 })).Not.ToBeVisibleAsync();
                    _output.WriteLine("Web app delete todo flow successful");

                }
            }
            catch (System.Exception ex)
            {
                //String message = clientProcess.StandardError.ReadToEnd();
                Assert.Fail($"the UI automation failed: {ex} output: {ex.Message}");

            }
            finally
            {
                //add the following to make sure sockets are unbound 
                Queue<Process> processes = new Queue<Process>();
                processes.Enqueue(serviceProcess);
                processes.Enqueue(clientProcess);
                processes.Enqueue(grpcProcess);
                killProcessTrees(processes);
            }
        }
        
        public static Process? StartWebAppLocally(string testAssemblyLocation, string appLocation, string executableName, string pathNumber=null)
        {
            string applicationWorkingDirectory = GetApplicationWorkingDirectory(testAssemblyLocation, appLocation);
            ProcessStartInfo processStartInfo = new ProcessStartInfo(applicationWorkingDirectory + executableName);
            processStartInfo.WorkingDirectory = applicationWorkingDirectory;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;

            if (!pathNumber.IsNullOrEmpty())
            {
                processStartInfo.EnvironmentVariables["Kestrel:Endpoints:Https:Url"] = "https://*:" + pathNumber;
            }
            return Process.Start(processStartInfo);
        }
     
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
        private void killProcessTrees(Queue<Process> processQueue)
        {
            Process currentProcess;
            while (processQueue.Count > 0)
            {
                currentProcess = processQueue.Dequeue();
                foreach (Process child in ProcessExtensions.GetChildProcesses(currentProcess))
                {
                    processQueue.Enqueue(child);
                };
                _output.WriteLine($"Killing process {currentProcess.Id}");
                currentProcess.Kill();
                currentProcess.Close();
            }
        }
    }

    public static class ProcessExtensions
    {
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
    }
#endif // !FROM_GITHUB_ACTION
}
