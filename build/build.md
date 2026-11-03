# Structure of the release build

[pipeline-releasebuild.yaml](pipeline-releasebuild.yaml):
- [template-prebuild-code-analysis.yaml](template-prebuild-code-analysis.yaml)
  - 'Run PoliCheck'
  - 'Run CredScan'
  - 'Post Analysis'
- [template-bootstrap-build.yaml](template-bootstrap-build.yaml)
  - [template-install-dotnet-core.yaml](template-install-dotnet-core.yaml)
    - 'Use .Net Core SDK 6'
    - 'Use .Net Core SDK 7'
  - [template-install-nuget.yaml](template-install-nuget.yaml)
- [template-install-keyvault-secrets.yaml](template-install-keyvault-secrets.yaml)
  - 'Azure Key Vault: buildautomation'
  - 'Install Keyvault Secrets'

- [template-restore-build-MSIdentityWeb.yaml](template-restore-build-MSIdentityWeb.yaml) `(BuildPlatform:'$(BuildPlatform)', BuildConfiguration: '$(BuildConfiguration)', MicrosoftIdentityWebVersion: $(MicrosoftIdentityWebVersion))`
  - Build solution Microsoft.Identity.Web.sln and run tests' (.NET Core)
  - [Build](template-restore-build-MSIdentityWeb.yaml) solution Microsoft.Identity.Web.sln netcoreapp3.1 for Roslyn analyzers' (VSBuild@1)
  - 'Component Detection'
- [template-postbuild-code-analysis.yaml](template-postbuild-code-analysis.yaml)
  - 'Run Roslyn Analyzers'
  - 'Check Roslyn Results '
- [template-sign-binary.yaml](template-sign-binary.yaml) - Sign the binaries, requires dotnet core 2.x.
- [template-pack-and-sign-all-nugets.yaml](template-pack-and-sign-all-nugets.yaml)
  - [template-pack-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\src\Microsoft.Identity.Web')`
  - [template-pack-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\src\Microsoft.Identity.Web.UI')`
  - [template-pack-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\src\Microsoft.Identity.Web.MicrosoftGraph')`
  - [template-pack-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\src\Microsoft.Identity.Web.MicrosoftGraphBeta')`
  - [template-pack-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\ProjectTemplates')`
  - [template-pack-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\src\Microsoft.Identity.Web.TokenCache')`
  - [template-pack-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\src\Microsoft.Identity.Web.TokenAcquisition')`
  - [template-pack-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\src\Microsoft.Identity.Web.Certificate')`
  - [template-pack-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\src\Microsoft.Identity.Web.Certificateless')`
  - [template-pack-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\src\Microsoft.Identity.Web.OWIN')`
  - [template-pack-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\src\Microsoft.Identity.Web.DownstreamApi')`
  - [template-pack-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\src\Microsoft.Identity.Web.GraphServiceClient')`
  - [template-pack-nuget.yaml](template-pack-and-sign-nuget.yaml) `('$(Build.SourcesDirectory)\src\Microsoft.Identity.Web.GraphServiceClientBeta')`
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

## Certificate-backed performance benchmarks

The `Performance` stage in `azure-pipelines.yml` runs the two token acquisition
benchmarks on .NET 8 for `master` builds and manually queued builds. It does not
run for pull requests.

The stage reuses `template-install-dependencies.yaml` to install the `LabAuth`
certificate from Key Vault into `LocalMachine/My`. It selects the certificate
using `BenchmarkCertificateThumbprint` and fails before running benchmarks if
the certificate is missing, lacks a private key, or is outside its validity period.
When rotating the certificate, update both the app registration and this thumbprint.
No Entra client secret is needed.

Reports appear in the build summary and the `token-acquisition-benchmarks`
pipeline artifact, including BenchmarkDotNet logs when execution fails. This
replaces the GitHub benchmark workflow; the existing public chart is no longer
updated. Automatic regression comparison is deferred until an ADO baseline is
established, so these runs do not enforce the previous 130% threshold.
