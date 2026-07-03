// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace IntegrationTestService
{
    public class Program
    {
        // Opt-in switch. When set to a truthy value, the test host loads the LabAuth
        // client certificate from the local certificate store instead of the msidlabs
        // KeyVault. Used on hosted agents that have no managed identity able to read the
        // KeyVault (the pipeline installs LabAuth into LocalMachine/My). Defaults to the
        // KeyVault source, so the official (Wilson) pipeline and local dev are unchanged.
        internal const string UseCertFromStoreEnvVar = "UseLabAuthCertFromStore";

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    if (ShouldUseCertFromStore())
                    {
                        config.AddInMemoryCollection(LabAuthCertFromStoreOverrides());
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static bool ShouldUseCertFromStore()
        {
            string? value = Environment.GetEnvironmentVariable(UseCertFromStoreEnvVar);
            return !string.IsNullOrEmpty(value) &&
                   (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1");
        }

        // Overrides the KeyVault-sourced client certificate in appsettings.json with the
        // LabAuth certificate installed in LocalMachine/My (CN=LabAuth.MSIDLab.com), for
        // both authentication schemes (AzureAd and AzureAd2).
        private static IEnumerable<KeyValuePair<string, string?>> LabAuthCertFromStoreOverrides()
        {
            foreach (string section in new[] { "AzureAd", "AzureAd2" })
            {
                string prefix = $"{section}:ClientCertificates:0:";
                yield return new(prefix + "SourceType", "StoreWithDistinguishedName");
                yield return new(prefix + "CertificateStorePath", "LocalMachine/My");
                yield return new(prefix + "CertificateDistinguishedName", "CN=LabAuth.MSIDLab.com");
                // Clear the KeyVault-specific keys inherited from appsettings.json.
                yield return new(prefix + "KeyVaultUrl", null);
                yield return new(prefix + "KeyVaultCertificateName", null);
            }
        }
    }
}
