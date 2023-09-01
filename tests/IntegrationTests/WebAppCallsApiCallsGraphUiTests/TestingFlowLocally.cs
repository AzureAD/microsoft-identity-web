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
        private const string UrlString = @"https://localhost:44321";
        private const string DevAppPath = @"DevApps\WebAppCallsWebApiCallsGraph";
        private const string TodoListServicePath = @"\TodoListService";
        private const string TodoListClientPath = @"\Client";
        private const string GrpcPath = @"\gRPC";
        private const string TodoListServiceExecutable = @"\TodoListService.exe";
        private const string TodoListClientExecutable = @"\TodoListClient.exe";
        private const string GrpcExecutable = "grpc.exe";
        private string UiTestAssemblyLocation = typeof(TestingFlowLocally).Assembly.Location;
        private readonly ITestOutputHelper _output;


        public TestingFlowLocally(ITestOutputHelper output)
        {
            //UiTestAssemblyLocation = typeof(TestingFlowLocally).Assembly.Location;
            _output = output;
        }

        [Fact]
        public async Task ChallengeUser_MicrosoftIdentityFlow_LocalApp_ValidEmailPasswordCreds_TodoAppFunctionsCorrectly()
        {
            // Arrange process setup, grpc left commented out for now
            //Process? grpcProcess = TestingFlowLocally.StartWebAppLocally(UiTestAssemblyLocation, DevAppPath + GrpcPath, GrpcExecutable);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("Kestrel:Endpoints:Https:Url", "https://*:44351");
            Process? serviceProcess = TestingFlowLocally.StartWebAppLocally(UiTestAssemblyLocation, DevAppPath + TodoListServicePath, TodoListServiceExecutable, true, "44351");
            Environment.SetEnvironmentVariable("Kestrel:Endpoints:Https:Url", "https://*:44321");
            Process? clientProcess = TestingFlowLocally.StartWebAppLocally(UiTestAssemblyLocation, DevAppPath + TodoListClientPath, TodoListClientExecutable, true, "44321");

            try
            {
                if (clientProcess != null && serviceProcess != null /*&& grpcProcess != null*/)
                {
                    if (clientProcess.HasExited || serviceProcess.HasExited/* || grpcProcess.HasExited*/)
                    {
                        Assert.Fail($"Could not run web app locally.");
                    }

                    // Arrange Playwright setup
                    using var playwright = await Playwright.CreateAsync();
                    IBrowser browser;
                    browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
                    IPage page = await browser.NewPageAsync();
                    await page.GotoAsync(UrlString);
                    LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);

                    // Act Login
                    Trace.WriteLine("Starting web app sign-in flow");
                    string email = labResponse.User.Upn;
                    await UiTestHelpers.PerformLogin_MicrosoftIdentityFlow_ValidEmailPasswordCreds(page, email, labResponse.User.GetOrFetchPassword());

                    // Assert Login Successful
                    await Assertions.Expect(page.GetByText("Welcome")).ToBeVisibleAsync();
                    await Assertions.Expect(page.GetByText(email)).ToBeVisibleAsync();
                    Trace.WriteLine("Web app sign-in flow successful");

                    // Act Sign Out
                    Trace.WriteLine("Starting web app sign-out flow");
                    var accountSelectMenuLocator = page.Locator($"[data-test-id=\"{email}\"]");
                    await page.GetByRole(AriaRole.Link, new() { Name = "Sign out" }).ClickAsync();
                    await accountSelectMenuLocator.ClickAsync();

                    // Assert Sign Out Successful
                    await page.WaitForURLAsync(UrlString + "/MicrosoftIdentity/Account/SignedOut");
                    Trace.WriteLine("Web app sign out successful");
                    
                    // Act Sign In Again
                    Trace.WriteLine("Starting web app sign-in flow after sign out");
                    

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
                //processes.Enqueue(grpcProcess);
                killProcessTrees(processes);
            }
            }
        
        public static Process? StartWebAppLocally(string testAssemblyLocation, string appLocation, string executableName, bool elevated = false, string pathNumber=null)
        {
            string testedAppLocation = Path.Combine(Path.GetDirectoryName(testAssemblyLocation)!);
            // e.g. microsoft-identity-web\tests\IntegrationTests\WebAppUiTests\bin\Debug\net6.0
            string[] segments = testedAppLocation.Split(Path.DirectorySeparatorChar);
            int numberSegments = segments.Length;
            int startLastSegments = numberSegments - 3;
            int endFirstSegments = startLastSegments - 2;
            string applicationWorkingDirectory = Path.Combine(
                Path.Combine(segments.Take(endFirstSegments).ToArray()),
                appLocation,
                Path.Combine(segments.Skip(startLastSegments).ToArray())
                
            );
            ProcessStartInfo processStartInfo = new ProcessStartInfo(applicationWorkingDirectory + executableName);
            if (elevated)
            {
                processStartInfo.UseShellExecute = true;
                processStartInfo.Verb = "runas";
                
            } else
            {
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.CreateNoWindow = false;
                // processStartInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";
                // processStartInfo.AddEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
                

                if (!pathNumber.IsNullOrEmpty())
                {
                    processStartInfo.EnvironmentVariables["Kestrel:Endpoints:Https:Url"] = "https://*:" + pathNumber;
                }
            }
            return Process.Start(processStartInfo);
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
 // Validate platform compatibility
                return new ManagementObjectSearcher(
                    $"Select * From Win32_Process Where ParentProcessID={process.Id}")
                    .Get()
                    .Cast<ManagementObject>()
#pragma warning disable CA1416 // This call can not be reached except on Windows
                    .Select(mo =>
                        Process.GetProcessById(Convert.ToInt32(mo["ProcessID"], System.Globalization.CultureInfo.InvariantCulture)))
#pragma warning restore CA1416
                    .ToList();
 // Validate platform compatibility
            }
            else
            {
                throw new NotImplementedException("Not implemented for this OS");
            }
        }
    }
}
