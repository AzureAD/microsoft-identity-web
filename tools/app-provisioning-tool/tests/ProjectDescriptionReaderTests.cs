// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using DotnetTool.CodeReaderWriter;
using DotnetTool.DeveloperCredentials;
using DotnetTool.MicrosoftIdentityPlatformApplication;
using DotnetTool.Project;
using System.IO;
using Xunit;

namespace Tests
{
    public class ProjectDescriptionReaderTests
    {
        readonly ProjectDescriptionReader _projectDescriptionReader = new ProjectDescriptionReader();
        readonly CodeReader _codeReader = new CodeReader();
        readonly DeveloperCredentialsReader _developerCredentialsReader = new DeveloperCredentialsReader();
        readonly MicrosoftIdentityPlatformApplicationManager _microsoftIdentityPlatformApplicationManager = new MicrosoftIdentityPlatformApplicationManager();

        [InlineData(@"blazorserver2\blazorserver2-b2c", "dotnet-webapp", true)]
        [InlineData(@"blazorserver2\blazorserver2-b2c-callswebapi", "dotnet-webapp", true)]
        [InlineData(@"blazorserver2\blazorserver2-singleorg", "dotnet-webapp")]
        [InlineData(@"blazorserver2\blazorserver2-singleorg-callsgraph", "dotnet-webapp")]
        [InlineData(@"blazorserver2\blazorserver2-singleorg-callswebapi", "dotnet-webapp")]
        [InlineData(@"blazorwasm2\blazorwasm2-b2c", "dotnet-blazorwasm", true)]
        [InlineData(@"blazorwasm2\blazorwasm2-b2c-hosted", "dotnet-blazorwasm-hosted", true)]
        [InlineData(@"blazorwasm2\blazorwasm2-singleorg", "dotnet-blazorwasm")]
        //[InlineData(@"blazorwasm2\blazorwasm2-singleorg-callsgraph", "dotnet-blazorwasm")]
        [InlineData(@"blazorwasm2\blazorwasm2-singleorg-callsgraph-hosted", "dotnet-blazorwasm-hosted")]
        //[InlineData(@"blazorwasm2\blazorwasm2-singleorg-callswebapi", "dotnet-blazorwasm")]
        [InlineData(@"blazorwasm2\blazorwasm2-singleorg-callswebapi-hosted", "dotnet-blazorwasm-hosted")]
        [InlineData(@"blazorwasm2\blazorwasm2-singleorg-hosted", "dotnet-blazorwasm-hosted")]
        [InlineData(@"mvc2\mvc2-b2c", "dotnet-webapp", true)]
        [InlineData(@"mvc2\mvc2-b2c-callswebapi", "dotnet-webapp", true)]
        [InlineData(@"mvc2\mvc2-noauth", "dotnet-webapp")]
        [InlineData(@"mvc2\mvc2-singleorg", "dotnet-webapp")]
        [InlineData(@"mvc2\mvc2-singleorg-callsgraph", "dotnet-webapp")]
        [InlineData(@"mvc2\mvc2-singleorg-callswebapi", "dotnet-webapp")]
        [InlineData(@"webapi2\webapi2-b2c", "dotnet-webapi", true)]
        [InlineData(@"webapi2\webapi2-singleorg", "dotnet-webapi")]
        [InlineData(@"webapi2\webapi2-singleorg-callsgraph", "dotnet-webapi")]
        [InlineData(@"webapi2\webapi2-singleorg-callswebapi", "dotnet-webapi")]
        [InlineData(@"webapp2\webapp2-b2c", "dotnet-webapp", true)]
        [InlineData(@"webapp2\webapp2-b2c-callswebapi", "dotnet-webapp", true)]
        [InlineData(@"webapp2\webapp2-singleorg", "dotnet-webapp")]
        [InlineData(@"webapp2\webapp2-singleorg-callsgraph", "dotnet-webapp")]
        [InlineData(@"webapp2\webapp2-singleorg-callswebapi", "dotnet-webapp")]
        [Theory]
        public void TestProjectDescriptionReader(string folderPath, string expectedProjectType, bool isB2C = false)
        {
            // string parentFolder = @"C:\gh\microsoft-identity-web\ProjectTemplates\bin\Debug\tests";
            string parentFolder = @"C:\git\idweb\ProjectTemplates\bin\Debug\tests";

            string folder = Path.Combine(parentFolder, folderPath);
            var projectDescription = _projectDescriptionReader.GetProjectDescription(string.Empty, folder);
            Assert.NotNull(projectDescription);
            Assert.Equal(expectedProjectType, projectDescription.Identifier);

            var authenticationSettings = _codeReader.ReadFromFiles(
                folder,
                projectDescription,
                _projectDescriptionReader.projectDescriptions);

            bool callsGraph = folderPath.Contains(TestConstants.CallsGraph);
            bool callsWebApi = folderPath.Contains(TestConstants.CallsWebApi) || callsGraph;

            if (isB2C)
            {
                Assert.True(authenticationSettings.ApplicationParameters.HasAuthentication);
                Assert.True(authenticationSettings.ApplicationParameters.IsB2C);
                Assert.Equal(TestConstants.B2CInstance, authenticationSettings.ApplicationParameters.Instance);
                Assert.Equal(TestConstants.B2CDomain, authenticationSettings.ApplicationParameters.Domain);
                Assert.Equal(TestConstants.B2CDomain1, authenticationSettings.ApplicationParameters.Domain1);
                Assert.Equal(TestConstants.B2CClientId, authenticationSettings.ApplicationParameters.ClientId);
                if (callsWebApi)
                {
                    Assert.Equal(TestConstants.B2CScopes, authenticationSettings.ApplicationParameters.CalledApiScopes);
                }
            }
            else
            {
                Assert.True(authenticationSettings.ApplicationParameters.HasAuthentication);
                Assert.True(authenticationSettings.ApplicationParameters.IsAAD);
                Assert.Null(authenticationSettings.ApplicationParameters.Instance);
                Assert.Equal(TestConstants.Domain, authenticationSettings.ApplicationParameters.Domain);
                Assert.Equal(TestConstants.Domain1, authenticationSettings.ApplicationParameters.Domain1);

                Assert.Equal(TestConstants.ClientId, authenticationSettings.ApplicationParameters.ClientId);

                if (callsWebApi)
                {
                    Assert.Equal(TestConstants.Scopes, authenticationSettings.ApplicationParameters.CalledApiScopes);
                }
            }

            if (authenticationSettings.ApplicationParameters.IsWebApi)
            {
                Assert.True(authenticationSettings.ApplicationParameters.IsWebApi);
            }
            else
            {
                Assert.True(authenticationSettings.ApplicationParameters.IsWebApp);
            }

            if (folderPath.Contains(TestConstants.Blazor))
            {
                //  Assert.True(authenticationSettings.ApplicationParameters.IsBlazor);
            }

            Assert.Equal(callsGraph, authenticationSettings.ApplicationParameters.CallsMicrosoftGraph);
            Assert.Equal(callsWebApi, authenticationSettings.ApplicationParameters.CallsDownstreamApi);

            var developerCredentials = _developerCredentialsReader.GetDeveloperCredentials(
                null,
                authenticationSettings.ApplicationParameters.TenantId ?? authenticationSettings.ApplicationParameters.Domain);

            Assert.NotNull(developerCredentials);

            var readApplicationParameters = _microsoftIdentityPlatformApplicationManager.ReadApplication(developerCredentials,
                authenticationSettings.ApplicationParameters);
            Assert.NotNull(readApplicationParameters);
        }

        [InlineData(@"blazorserver2\blazorserver2-noauth", "dotnet-webapp")]
        //[InlineData(@"blazorwasm2\blazorwasm2-noauth", "dotnet-blazorwasm")] //...erPath: "blazorwasm2\\blazorwasm2-noauth", expectedProjectType: "dotnet-blazorwasm"
        [InlineData(@"mvc2\mvc2-noauth", "dotnet-webapp")]
        [InlineData(@"webapi2\webapi2-noauth", "dotnet-webapi")]
        [InlineData(@"webapp2\webapp2-noauth", "dotnet-webapp")]
        [Theory]
        public void TestProjectDescriptionReader_TemplatesWithNoAuth(string folderPath, string expectedProjectType)
        {
            // string parentFolder = @"C:\gh\microsoft-identity-web\ProjectTemplates\bin\Debug\tests";
            string parentFolder = @"C:\git\idweb\ProjectTemplates\bin\Debug\tests";

            string folder = Path.Combine(parentFolder, folderPath);
            var projectDescription = _projectDescriptionReader.GetProjectDescription(string.Empty, folder);
            Assert.NotNull(projectDescription);
            Assert.Equal(expectedProjectType, projectDescription.Identifier);

            var authenticationSettings = _codeReader.ReadFromFiles(
                folder,
                projectDescription,
                _projectDescriptionReader.projectDescriptions);

            Assert.False(authenticationSettings.ApplicationParameters.HasAuthentication);
            Assert.Empty(authenticationSettings.ApplicationParameters.ApiPermissions);
            Assert.Null(authenticationSettings.ApplicationParameters.Authority);
            Assert.Null(authenticationSettings.ApplicationParameters.CalledApiScopes);
            Assert.False(authenticationSettings.ApplicationParameters.CallsDownstreamApi);
            Assert.False(authenticationSettings.ApplicationParameters.CallsMicrosoftGraph);
            Assert.Null(authenticationSettings.ApplicationParameters.ClientId);
            Assert.Null(authenticationSettings.ApplicationParameters.Domain);
            Assert.Null(authenticationSettings.ApplicationParameters.Domain1);
            Assert.Null(authenticationSettings.ApplicationParameters.Instance);
            Assert.False(authenticationSettings.ApplicationParameters.IsAAD);
            Assert.False(authenticationSettings.ApplicationParameters.IsB2C);
            Assert.Null(authenticationSettings.ApplicationParameters.TenantId);

            if (authenticationSettings.ApplicationParameters.IsWebApi)
            {
                Assert.True(authenticationSettings.ApplicationParameters.IsWebApi);
            }
            else
            {
                Assert.True(authenticationSettings.ApplicationParameters.IsWebApp);
            }
        }
    }
}

