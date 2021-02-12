using DotnetTool.CodeReaderWriter;
using DotnetTool.DeveloperCredentials;
using DotnetTool.MicrosoftIdentityPlatformApplication;
using DotnetTool.Project;
using System.IO;
using Xunit;

namespace Tests
{
    public class UnitTests
    {
        readonly ProjectDescriptionReader projectDescriptionReader = new ProjectDescriptionReader();

        readonly CodeReader codeReader = new CodeReader();
        readonly DeveloperCredentialsReader developerCredentialsReader = new DeveloperCredentialsReader();

        readonly MicrosoftIdentityPlatformApplicationManager MicrosoftIdentityPlatformApplicationManager = new MicrosoftIdentityPlatformApplicationManager();

        [InlineData(@"blazorserver2\blazorserver2-noauth", false, "dotnet-webapp")]
        [InlineData(@"blazorserver2\blazorserver2-b2c", true, "dotnet-webapp")]
        [InlineData(@"blazorserver2\blazorserver2-b2c-callswebapi", true, "dotnet-webapp")]
        [InlineData(@"blazorserver2\blazorserver2-singleorg", false, "dotnet-webapp")]
        [InlineData(@"blazorserver2\blazorserver2-singleorg-callsgraph", false, "dotnet-webapp")]
        [InlineData(@"blazorserver2\blazorserver2-singleorg-callswebapi", false, "dotnet-webapp")]
        [InlineData(@"blazorwasm2\blazorwasm2-b2c", true, "dotnet-blazorwasm")]
        [InlineData(@"blazorwasm2\blazorwasm2-b2c-hosted", true, "dotnet-blazorwasm-hosted")]
        [InlineData(@"blazorwasm2\blazorwasm2-noauth", false, "dotnet-blazorwasm")]
        [InlineData(@"blazorwasm2\blazorwasm2-singleorg", false, "dotnet-blazorwasm")]
        //[InlineData(@"blazorwasm2\blazorwasm2-singleorg-callsgraph", false, "dotnet-blazorwasm")]
        [InlineData(@"blazorwasm2\blazorwasm2-singleorg-callsgraph-hosted", false, "dotnet-blazorwasm-hosted")]
        //[InlineData(@"blazorwasm2\blazorwasm2-singleorg-callswebapi", false, "dotnet-blazorwasm")]
        [InlineData(@"blazorwasm2\blazorwasm2-singleorg-callswebapi-hosted", false, "dotnet-blazorwasm-hosted")]
        [InlineData(@"blazorwasm2\blazorwasm2-singleorg-hosted", false, "dotnet-blazorwasm-hosted")]
        [InlineData(@"mvc2\mvc2-b2c", true, "dotnet-webapp")]
        [InlineData(@"mvc2\mvc2-b2c-callswebapi", true, "dotnet-webapp")]
        [InlineData(@"mvc2\mvc2-noauth", false, "dotnet-webapp")]
        [InlineData(@"mvc2\mvc2-singleorg", false, "dotnet-webapp")]
        [InlineData(@"mvc2\mvc2-singleorg-callsgraph", false, "dotnet-webapp")]
        [InlineData(@"mvc2\mvc2-singleorg-callswebapi", false, "dotnet-webapp")]
        [InlineData(@"webapi2\webapi2-b2c", true, "dotnet-webapi")]
        [InlineData(@"webapi2\webapi2-noauth", false, "dotnet-webapi")]
        [InlineData(@"webapi2\webapi2-singleorg", false, "dotnet-webapi")]
        [InlineData(@"webapi2\webapi2-singleorg-callsgraph", false, "dotnet-webapi")]
        [InlineData(@"webapi2\webapi2-singleorg-callswebapi", false, "dotnet-webapi")]
        [InlineData(@"webapp2\webapp2-b2c", true, "dotnet-webapp")]
        [InlineData(@"webapp2\webapp2-b2c-callswebapi", true, "dotnet-webapp")]
        [InlineData(@"webapp2\webapp2-noauth", false, "dotnet-webapp")]
        [InlineData(@"webapp2\webapp2-singleorg", false, "dotnet-webapp")]
        [InlineData(@"webapp2\webapp2-singleorg-callsgraph", false, "dotnet-webapp")]
        [InlineData(@"webapp2\webapp2-singleorg-callswebapi", false, "dotnet-webapp")]
        [Theory]
        public void TestProjectDescriptionReader(string folderPath, bool isB2C, string expectedProjectType)
        {
            string parentFolder = @"C:\gh\microsoft-identity-web\ProjectTemplates\bin\Debug\tests";


            string folder = Path.Combine(parentFolder, folderPath);
            var projectDescription = projectDescriptionReader.GetProjectDescription(string.Empty, folder);
            Assert.NotNull(projectDescription);
            Assert.Equal(expectedProjectType, projectDescription.Identifier);

            var authenticationSettings = codeReader.ReadFromFiles(
                folder, 
                projectDescription, 
                projectDescriptionReader.projectDescriptions);
            var applicationParameters = authenticationSettings.ApplicationParameters;

            bool callsGraph = folderPath.Contains("callsgraph");
            bool callsWebApi = folderPath.Contains("callswebapi") || callsGraph;
            Assert.Equal(applicationParameters.IsB2C, isB2C);

            Assert.Equal(callsGraph, applicationParameters.CallsMicrosoftGraph);
            Assert.Equal(callsWebApi, applicationParameters.CallsDownstreamApi);

            var developerCredentials = developerCredentialsReader.GetDeveloperCredentials(
                null,
                applicationParameters.TenantId?? applicationParameters.Domain);

            var readApplicationParameters = MicrosoftIdentityPlatformApplicationManager.ReadApplication(developerCredentials,
                applicationParameters);
            Assert.NotNull(readApplicationParameters);
        }
    }
}
