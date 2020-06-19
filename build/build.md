# Stucture of the build

[pipeline-releasebuild.yaml](pipeline-releasebuild.yaml):
- [template-prebuild-code-analysis.yaml](template-prebuild-code-analysis.yaml)
  - 'Run PoliCheck'
  - 'Run CredScan'
  - 'Post Analysis'
- [template-bootstrap-build.yaml](template-bootstrap-build.yaml)
  - [template-install-dotnet-core.yaml](template-install-dotnet-core.yaml)
    - 'Use .Net Core SDK 5'
    - 'Use .Net Core SDK 3.1.101'
  - [template-install-nuget.yaml](template-install-nuget.yaml)
    - 'Use NuGet 4.6.2'
- [template-install-keyvault-secrets.yaml](template-install-keyvault-secrets.yaml)
  - 'Azure Key Vault: buildautomation'
  - 'Install Keyvault Secrets'

- [template-restore-build-MSIdentityWeb.yaml](template-restore-build-MSIdentityWeb.yaml) `(BuildPlatform:'$(BuildPlatform)', BuildConfiguration: '$(BuildConfiguration)', MsIdentityWebSemVer: $(MsIdentityWebSemVer))`
  - Build solution Microsoft.Identity.Web.sln and run tests' (.NET Core)
  - Buil(template-restore-build-MSIdentityWeb.yaml)d solution Microsoft.Identity.Web.sln netcoreapp3.1 for Roslyn analyzers' (VSBuild@1)
  - 'Component Detection'
- [template-postbuild-code-analysis.yaml](template-postbuild-code-analysis.yaml)
  - 'Run Roslyn Analyzers'
  - 'Check Roslyn Results '
- [template-pack-and-sign-all-nugets.yaml](template-pack-and-sign-all-nugets.yaml)
  - [template-pack-and-sign-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\src\Microsoft.Identity.Web')`
  - [template-pack-and-sign-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\src\Microsoft.Identity.Web.UI')`
  - [template-pack-and-sign-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\ProjectTemplates')`
  - 'Copy Files from `$(Build.SourcesDirectory)` to: `$(Build.ArtifactStagingDirectory)\packages'`
  - Sign Packages `'('$(Build.ArtifactStagingDirectory)\packages')`
- [template-publish-packages-and-symbols.yaml](template-publish-packages-and-symbols.yaml)
  - 'Verify packages are signed'
  - 'Publish Artifact: packages'
  - 'Publish packages to MyGet'
  - 'Publish packages to VSTS feed'
  - 'Publish symbols'
- [template-publish-analysis-and-cleanup.yaml](template-publish-analysis-and-cleanup.yaml)
  - Publish Security Analysis Logs'
  - 'TSA upload to Codebase: Microsoft Identity Web .NET Stamp: Azure'
  - 'Clean Agent Directories'
