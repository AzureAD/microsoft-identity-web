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

        private static IEnumerable<KeyValuePair<string, string?>> LabAuthCertFromStoreOverrides()
        {
            foreach (string section in new[] { "AzureAd", "AzureAd2" })
            {
                string prefix = $"{section}:ClientCertificates:0:";
                yield return new(prefix + "SourceType", "StoreWithDistinguishedName");
                yield return new(prefix + "CertificateStorePath", "LocalMachine/My");
                yield return new(prefix + "CertificateDistinguishedName", "CN=LabAuth.MSIDLab.com");
                yield return new(prefix + "KeyVaultUrl", null);
                yield return new(prefix + "KeyVaultCertificateName", null);
            }
        }
    }
}
