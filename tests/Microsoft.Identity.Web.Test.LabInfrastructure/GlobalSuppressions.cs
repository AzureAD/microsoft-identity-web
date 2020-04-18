// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "not applicable", Scope = "type", Target = "~T:Microsoft.Identity.Web.Test.LabInfrastructure.LabAccessAuthenticationType")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this", Justification = "not applicable", Scope = "member", Target = "~M:Microsoft.Identity.Web.Test.LabInfrastructure.LabUserNotFoundException.#ctor(Microsoft.Identity.Web.Test.LabInfrastructure.UserQuery,System.String)")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "not applicable", Scope = "member", Target = "~P:Microsoft.Identity.Web.Test.LabInfrastructure.LabUserNotFoundException.Parameters")]
[assembly: SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "not applicable", Scope = "type", Target = "~T:Microsoft.Identity.Web.Test.LabInfrastructure.LabUserNotFoundException")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "not applicable", Scope = "member", Target = "~M:Microsoft.Identity.Web.Test.LabInfrastructure.LabUserNotFoundException.#ctor(Microsoft.Identity.Web.Test.LabInfrastructure.UserQuery,System.String)")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "not applicable", Scope = "member", Target = "~M:Microsoft.Identity.Web.Test.LabInfrastructure.LabServiceApi.#ctor")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "not applicable", Scope = "member", Target = "~M:Microsoft.Identity.Web.Test.LabInfrastructure.LabServiceApi.#ctor")]
[assembly: SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "not applicable", Scope = "member", Target = "~M:Microsoft.Identity.Web.Test.LabInfrastructure.LabAuthenticationHelper.GetLabAccessTokenAsync(System.String,System.String[],Microsoft.Identity.Web.Test.LabInfrastructure.LabAccessAuthenticationType,System.String,System.String,System.String)~System.Threading.Tasks.Task{System.String}")]
[assembly: SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "not applicable", Scope = "member", Target = "~M:Microsoft.Identity.Web.Test.LabInfrastructure.KeyVaultSecretsProvider.GetSecret(System.String)~Microsoft.Azure.KeyVault.Models.SecretBundle")]
[assembly: SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "not applicable", Scope = "member", Target = "~P:Microsoft.Identity.Web.Test.LabInfrastructure.KeyVaultConfiguration.Url")]
[assembly: SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "not applicable", Scope = "member", Target = "~P:Microsoft.Identity.Web.Test.LabInfrastructure.LabApp.RedirectUri")]
