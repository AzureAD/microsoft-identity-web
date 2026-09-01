// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web.Test.Common;
using NSubstitute;
using NSubstitute.Extensions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    /// <summary>
    /// Pinning tests for the OIDC sign-in code path in
    /// <see cref="MicrosoftIdentityWebAppAuthenticationBuilderExtensions"/> when Authority
    /// is configured alongside Instance/TenantId/Domain/SignUpSignInPolicyId. OIDC counterpart
    /// of <see cref="MergedOptionsAuthorityConflictTests"/>; see each test for parity notes.
    /// </summary>
    public class WebAppExtensionsAuthorityConflictTests
    {
        private const string OidcScheme = "OpenIdConnect-Custom";
        private const string CookieScheme = "Cookies-Custom";
        private const string ConfigSectionName = "AzureAd-Custom";

        // All-zeros GUID lets failures fail loudly: if Authority ever stops winning,
        // this value would not appear in OpenIdConnectOptions.Authority.
        private const string BogusTenantGuid = "00000000-0000-0000-0000-000000000000";

        private const string BogusAadAuthority = "https://login.microsoftonline.com/" + BogusTenantGuid + "/v2.0";
        private const string BogusB2CAuthority = "https://fakeb2c.b2clogin.com/" + BogusTenantGuid + "/b2c_1_bogus";
        private const string BogusCiamAuthority = "https://fakeciam.ciamlogin.com/" + BogusTenantGuid;

        private readonly IHostEnvironment _env = new HostingEnvironment { EnvironmentName = Environments.Development };

        // ----- AAD: precedence (behavior pinning) -----

        [Fact]
        public void OidcSignIn_AuthorityAndInstance_ThrowsOnConflict()
        {
            // MSAL parity: ParseAuthorityIfNecessary_AuthorityAndInstance_LogsWarning. Now both paths throw.
            var ex = Assert.Throws<InvalidOperationException>(() => BuildAndGetOidcOptions(
                authority: BogusAadAuthority,
                instance: TestConstants.AadInstance,
                tenantId: null));

            Assert.Contains("Authority", ex.Message, StringComparison.Ordinal);
            Assert.Contains("conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void OidcSignIn_AuthorityAndTenantId_ThrowsOnConflict()
        {
            // MSAL parity: ParseAuthorityIfNecessary_AuthorityAndTenantId_LogsWarning. Now both paths throw.
            var ex = Assert.Throws<InvalidOperationException>(() => BuildAndGetOidcOptions(
                authority: BogusAadAuthority,
                instance: null,
                tenantId: TestConstants.TenantIdAsGuid));

            Assert.Contains("Authority", ex.Message, StringComparison.Ordinal);
            Assert.Contains("conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void OidcSignIn_AuthorityAndInstanceAndTenantId_ThrowsOnConflict()
        {
            // MSAL parity: ParseAuthorityIfNecessary_AuthorityAndInstanceAndTenantId_LogsWarning. Now both paths throw.
            var ex = Assert.Throws<InvalidOperationException>(() => BuildAndGetOidcOptions(
                authority: BogusAadAuthority,
                instance: TestConstants.AadInstance,
                tenantId: TestConstants.TenantIdAsGuid));

            Assert.Contains("Authority", ex.Message, StringComparison.Ordinal);
            Assert.Contains("conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void OidcSignIn_AuthorityOnly_PropagatesAuthorityAsIs()
        {
            // MSAL parity: ParseAuthorityIfNecessary_AuthorityOnly_LogsAuthorityUsedHint. Divergence: MSAL parses Authority + logs EventId 501; OIDC propagates Authority verbatim.
            var options = BuildAndGetOidcOptions(
                authority: TestConstants.AuthorityWithTenantSpecifiedWithV2,
                instance: null,
                tenantId: null);

            Assert.Equal(TestConstants.AuthorityWithTenantSpecifiedWithV2, options.Authority);
        }

        [Fact]
        public void OidcSignIn_InstanceAndTenantIdOnly_ComposesAuthorityFromInstanceTenantId()
        {
            // MSAL parity: ParseAuthorityIfNecessary_InstanceAndTenantIdOnly_NoWarning. Same outcome on both paths (no Authority -> Instance+TenantId compose).
            var options = BuildAndGetOidcOptions(
                authority: null,
                instance: TestConstants.AadInstance,
                tenantId: TestConstants.TenantIdAsGuid);

            Assert.Contains(TestConstants.TenantIdAsGuid, options.Authority, StringComparison.Ordinal);
            Assert.DoesNotContain(BogusTenantGuid, options.Authority, StringComparison.Ordinal);
        }

        // ----- B2C: precedence (behavior pinning) -----

        [Fact]
        public void OidcSignIn_B2CAuthorityAndInstance_ThrowsOnConflict()
        {
            // MSAL parity: ParseAuthorityIfNecessary_B2CAuthorityAndInstance_LogsWarning. Now both paths throw.
            var ex = Assert.Throws<InvalidOperationException>(() => BuildAndGetOidcOptions(
                authority: BogusB2CAuthority,
                instance: TestConstants.B2CInstance,
                tenantId: null,
                domain: TestConstants.B2CTenant,
                signUpSignInPolicyId: TestConstants.B2CSignUpSignInUserFlow));

            Assert.Contains("Authority", ex.Message, StringComparison.Ordinal);
            Assert.Contains("conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void OidcSignIn_B2CInstanceAndDomain_ComposesAuthorityFromInstanceDomainUserFlow()
        {
            // No MSAL counterpart; control: BuildAuthority composes "Instance/Domain/UserFlow/v2.0".
            var options = BuildAndGetOidcOptions(
                authority: null,
                instance: TestConstants.B2CInstance,
                tenantId: null,
                domain: TestConstants.B2CTenant,
                signUpSignInPolicyId: TestConstants.B2CSignUpSignInUserFlow);

            Assert.Contains(TestConstants.B2CTenant, options.Authority, StringComparison.Ordinal);
            Assert.Contains(TestConstants.B2CSignUpSignInUserFlow, options.Authority, StringComparison.Ordinal);
            Assert.DoesNotContain(BogusTenantGuid, options.Authority, StringComparison.Ordinal);
        }

        // ----- CIAM: precedence (behavior pinning) -----

        [Fact]
        public void OidcSignIn_CiamAuthorityAndInstance_ThrowsOnConflict()
        {
            // MSAL parity: ParseAuthorityIfNecessary_CiamAuthorityAndInstance_LogsWarning. Now both paths throw.
            var ex = Assert.Throws<InvalidOperationException>(() => BuildAndGetOidcOptions(
                authority: BogusCiamAuthority,
                instance: TestConstants.CIAMInstance,
                tenantId: null));

            Assert.Contains("Authority", ex.Message, StringComparison.Ordinal);
            Assert.Contains("conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void OidcSignIn_CiamAuthorityOnly_PropagatesAuthorityThroughCiamHelper()
        {
            // No MSAL counterpart; control: BuildCiamAuthorityIfNeeded preserves Authority, EnsureAuthorityIsV2 appends /v2.0.
            var options = BuildAndGetOidcOptions(
                authority: TestConstants.CIAMAuthorityV2,
                instance: null,
                tenantId: null);

            Assert.Contains("ciamlogin.com", options.Authority, StringComparison.Ordinal);
            Assert.Contains(TestConstants.CIAMTenant, options.Authority, StringComparison.Ordinal);
        }

        // ----- Log assertions (non-conflict scenarios still produce no warnings) -----

        [Fact]
        public void OidcSignIn_AuthorityOnly_DoesNotLogAuthorityUsedHint()
        {
            // MSAL parity: ParseAuthorityIfNecessary_AuthorityOnly_LogsAuthorityUsedHint (EventId 501, 1P hint). OIDC never surfaces this hint.
            var loggerProvider = new TestLoggerProvider();

            _ = BuildAndGetOidcOptions(
                authority: TestConstants.AuthorityWithTenantSpecifiedWithV2,
                instance: null,
                tenantId: null,
                loggerProvider: loggerProvider);

            Assert.DoesNotContain(loggerProvider.Logger.Entries, e => e.EventId.Id == 501);
        }

        [Fact]
        public void OidcSignIn_InstanceAndTenantIdOnly_DoesNotLogAnyAuthorityWarning()
        {
            // MSAL parity: ParseAuthorityIfNecessary_InstanceAndTenantIdOnly_NoWarning. Same outcome on both paths (no Authority -> no warnings).
            var loggerProvider = new TestLoggerProvider();

            _ = BuildAndGetOidcOptions(
                authority: null,
                instance: TestConstants.AadInstance,
                tenantId: TestConstants.TenantIdAsGuid,
                loggerProvider: loggerProvider);

            Assert.DoesNotContain(loggerProvider.Logger.Entries, e => e.EventId.Id == 500 || e.EventId.Id == 501);
        }

        // ----- Robustness -----

        [Fact]
        public void OidcSignIn_AuthorityAndInstance_ThrowsEvenWhenNoLoggerRegistered()
        {
            // Conflict throw does not depend on logger registration.
            Assert.Throws<InvalidOperationException>(() => BuildAndGetOidcOptions(
                authority: BogusAadAuthority,
                instance: TestConstants.AadInstance,
                tenantId: null,
                loggerProvider: null));
        }

        // ----- Helpers -----

        /// <summary>
        /// Builds AddMicrosoftIdentityWebApp from a synthetic config section and returns the
        /// resolved OpenIdConnectOptions. Pass a loggerProvider to capture log entries.
        /// </summary>
        private OpenIdConnectOptions BuildAndGetOidcOptions(
            string? authority,
            string? instance,
            string? tenantId,
            string? domain = null,
            string? signUpSignInPolicyId = null,
            TestLoggerProvider? loggerProvider = null)
        {
            var configSection = BuildConfigSection(
                ConfigSectionName,
                authority: authority,
                instance: instance,
                tenantId: tenantId,
                domain: domain,
                signUpSignInPolicyId: signUpSignInPolicyId,
                clientId: TestConstants.ClientId);

            var configMock = Substitute.For<IConfiguration>();
            configMock.Configure().GetSection(ConfigSectionName).Returns(configSection);

            var services = new ServiceCollection();
            services.AddSingleton(configMock);
            services.AddDataProtection();
            services.AddSingleton(_env);

            if (loggerProvider is not null)
            {
                services.AddLogging(b =>
                {
                    b.SetMinimumLevel(LogLevel.Trace);
                    b.AddProvider(loggerProvider);
                });
            }

            services.AddAuthentication()
                .AddMicrosoftIdentityWebApp(
                    configMock,
                    ConfigSectionName,
                    OidcScheme,
                    CookieScheme,
                    subscribeToOpenIdConnectMiddlewareDiagnosticsEvents: false);

            using var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OidcScheme);
        }

        /// <summary>
        /// In-memory IConfigurationSection populated only with non-null keys; ClientId is required
        /// to satisfy MergedOptionsValidation. Omitting keys leaves MergedOptions properties null,
        /// which is what selects the precedence branches under test.
        /// </summary>
        private static IConfigurationSection BuildConfigSection(
            string configSectionName,
            string? authority,
            string? instance,
            string? tenantId,
            string? domain,
            string? signUpSignInPolicyId,
            string clientId)
        {
            var configAsDictionary = new Dictionary<string, string?>
            {
                { configSectionName, null },
                { $"{configSectionName}:ClientId", clientId },
            };

            if (authority is not null)
            {
                configAsDictionary[$"{configSectionName}:Authority"] = authority;
            }

            if (instance is not null)
            {
                configAsDictionary[$"{configSectionName}:Instance"] = instance;
            }

            if (tenantId is not null)
            {
                configAsDictionary[$"{configSectionName}:TenantId"] = tenantId;
            }

            if (domain is not null)
            {
                configAsDictionary[$"{configSectionName}:Domain"] = domain;
            }

            if (signUpSignInPolicyId is not null)
            {
                configAsDictionary[$"{configSectionName}:SignUpSignInPolicyId"] = signUpSignInPolicyId;
            }

            var memoryConfigSource = new MemoryConfigurationSource { InitialData = configAsDictionary };

            var configBuilder = new ConfigurationBuilder();
            configBuilder.Add(memoryConfigSource);

            return configBuilder.Build().GetSection(configSectionName);
        }

        /// <summary>Single captured log entry.</summary>
        private sealed record TestLogEntry(LogLevel LogLevel, EventId EventId, string Message, string Category);

        /// <summary>
        /// ILogger that captures every call into a shared list (always enabled, all levels).
        /// </summary>
        private sealed class TestLogger : ILogger
        {
            private readonly string _category;
            public List<TestLogEntry> Entries { get; }

            public TestLogger(string category, List<TestLogEntry> entries)
            {
                _category = category;
                Entries = entries;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Entries.Add(new TestLogEntry(logLevel, eventId, formatter(state, exception), _category));
            }
        }

        /// <summary>
        /// ILoggerProvider routing every category-specific logger into a single shared
        /// TestLogger so tests can assert across categories via <see cref="Logger"/>.
        /// </summary>
        private sealed class TestLoggerProvider : ILoggerProvider
        {
            private readonly List<TestLogEntry> _entries = new();

            public TestLogger Logger { get; }

            public TestLoggerProvider()
            {
                Logger = new TestLogger(category: "(aggregate)", entries: _entries);
            }

            public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName, _entries);

            public void Dispose()
            {
            }
        }
    }
}
