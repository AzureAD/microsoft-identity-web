// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.App.AuthenticationParameters;
using Microsoft.Identity.App.Project;
using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Identity.App.CodeReaderWriter
{
    public class CodeWriter
    {
        internal void WriteConfiguration(Summary summary, IEnumerable<Replacement> replacements, ApplicationParameters reconcialedApplicationParameters)
        {
            foreach (var replacementsInFile in replacements.GroupBy(r => r.FilePath))
            {
                string filePath = replacementsInFile.Key;

                string fileContent = File.ReadAllText(filePath);
                bool updated = false;

                if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    updated = ReplaceInJSonFile(reconcialedApplicationParameters, replacementsInFile, ref fileContent);
                }
                else
                {
                    updated = ReplaceInTextFile(summary, reconcialedApplicationParameters, replacementsInFile, filePath, ref fileContent);
                }

                if (updated)
                {
                    // Keep a copy of the original
                    if (!File.Exists(filePath + "%"))
                    {
                        File.Copy(filePath, filePath + "%");
                    }
                    File.WriteAllText(filePath, fileContent);
                }
            }
        }

        private bool ReplaceInTextFile(Summary summary, ApplicationParameters reconcialedApplicationParameters, IGrouping<string, Replacement> replacementsInFile, string filePath, ref string fileContent)
        {
            bool updated = false;

            foreach (Replacement r in replacementsInFile.OrderByDescending(r => r.Index))
            {
                string? replaceBy = ComputeReplacement(r.ReplaceBy, reconcialedApplicationParameters);
                if (replaceBy != null && replaceBy != r.ReplaceFrom)
                {
                    int index = fileContent.IndexOf(r.ReplaceFrom /*, r.Index*/, StringComparison.OrdinalIgnoreCase);
                    if (index != -1)
                    {
                        fileContent = fileContent.Substring(0, index)
                            + replaceBy
                            + fileContent.Substring(index + r.Length);
                        updated = true;
                        summary.changes.Add(new Change($"{filePath}: updating {r.ReplaceBy}"));
                    }
                }
            }
            return updated;
        }

        private bool ReplaceInJSonFile(ApplicationParameters reconcialedApplicationParameters, IGrouping<string, Replacement> replacementsInFile, ref string fileContent)
        {
            bool updated = false;
            JsonNode jsonNode = JsonNode.Parse(fileContent, new JsonNodeOptions() { }, new JsonDocumentOptions() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip })!;
            foreach (var replacement in replacementsInFile.Where(r => r.ReplaceBy != null))
            {
                string? newValue = ComputeReplacement(replacement.ReplaceBy, reconcialedApplicationParameters);
                if (newValue == null)
                {
                    continue;
                }

                IEnumerable<string> pathToParent = replacement.Property.Split(":");
                string propertyName = pathToParent.Last();
                pathToParent = pathToParent.Take(pathToParent.Count() - 1);

                JsonNode? parent = jsonNode;
                foreach (string nodeName in pathToParent)
                {
                    if (parent != null)
                    {
                        parent = parent[nodeName];
                    }
                }

                if (parent == null)
                {
                    continue;
                }

                JsonValue? propertyNode = parent[propertyName] as JsonValue;

                if (propertyNode == null)
                {
                    parent.AsObject().Add(propertyName, newValue);
                    updated = true;
                }
                else if (newValue == "*Remove*")
                {
                    JsonObject parentAsObject = parent.AsObject();
                    parentAsObject.Remove(propertyName);
                    updated = true;
                }
                else
                {
                    if (propertyNode.TryGetValue(out string? value))
                    {
                        if (value != newValue)
                        {
                            parent[propertyName] = newValue;
                            updated = true;
                        }
                    }
                }
            }

            fileContent = jsonNode.ToJsonString(new JsonSerializerOptions() { WriteIndented = true });
            return updated;
        }

        private string? ComputeReplacement(string replaceBy, ApplicationParameters reconciledApplicationParameters)
        {
            string? replacement = replaceBy;
            switch (replaceBy)
            {
                case "Application.ClientSecret":
                    string? password = reconciledApplicationParameters.PasswordCredentials.LastOrDefault();
                    if (!string.IsNullOrEmpty(reconciledApplicationParameters.SecretsId))
                    {
                        string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        string secretsPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? @"AppData\Roaming\Microsoft\UserSecrets\"
                            : @".microsoft/usersecrets/";
                        string path = Path.Combine(
                            userHome,
                            secretsPath,
                            reconciledApplicationParameters.SecretsId,
                            "secrets.json")!;
                        if (!File.Exists(path))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                            string section = reconciledApplicationParameters.IsB2C ? "AzureADB2C" : "AzureAD";
                            File.WriteAllText(path, $"{{\n    \"{section}:ClientSecret\": \"{password}\"\n}}");
                            replacement = "See user secrets";
                        }
                        else
                        {
                            replacement = password;
                        }
                    }
                    else
                    {
                        replacement = password;
                    }
                    break;
                case "Application.ClientId":
                    replacement = reconciledApplicationParameters.ClientId;
                    break;
                case "Directory.TenantId":
                    replacement = reconciledApplicationParameters.IsCiam ? "*Remove*" : reconciledApplicationParameters.TenantId;
                    break;
                case "Directory.Domain":
                    replacement = reconciledApplicationParameters.IsCiam ? "*Remove*" : reconciledApplicationParameters.Domain;
                    break;
                case "Application.SusiPolicy":
                    replacement = reconciledApplicationParameters.SusiPolicy;
                    break;
                case "Application.CallbackPath":
                    replacement = reconciledApplicationParameters.CallbackPath;
                    break;
                case "profilesApplicationUrls":
                case "iisSslPort":
                case "iisApplicationUrl":
                    replacement = null;
                    break;
                case "secretsId":
                    replacement = reconciledApplicationParameters.SecretsId;
                    break;
                case "targetFramework":
                    replacement = reconciledApplicationParameters.TargetFramework;
                    break;
                case "Application.Authority":
                    if (reconciledApplicationParameters.IsCiam && reconciledApplicationParameters.Domain!=null)
                    {
                        replacement = "https://"
                            + reconciledApplicationParameters.Domain.Replace(".onmicrosoft.com", ".ciamlogin.com", StringComparison.OrdinalIgnoreCase) + "/";
                    }
                    else
                    {
                        replacement = reconciledApplicationParameters.Authority;
                        // Blazor b2C
                        replacement = replacement?.Replace("onmicrosoft.com.b2clogin.com", "b2clogin.com", StringComparison.OrdinalIgnoreCase);
                    }
                    break;
                case "MsalAuthenticationOptions":
                    // Todo generalize with a directive: Ensure line after line, or ensure line
                    // between line and line
                    replacement = reconciledApplicationParameters.MsalAuthenticationOptions;
                    if (reconciledApplicationParameters.AppIdUri == null)
                    {
                        replacement +=
                            "\n                options.ProviderOptions.DefaultAccessTokenScopes.Add(\"User.Read\");";

                    }
                    break;
                case "Application.CalledApiScopes":
                    replacement = reconciledApplicationParameters.CalledApiScopes
                        ?.Replace("openid", string.Empty, StringComparison.OrdinalIgnoreCase)
                        ?.Replace("offline_access", string.Empty, StringComparison.OrdinalIgnoreCase)
                        ?.Trim();
                    break;

                case "Application.Instance":
                    if (reconciledApplicationParameters.IsCiam)
                    {
                        replacement = "*Remove*";
                    }
                    else
                    {

                        if (reconciledApplicationParameters.Instance == "https://login.microsoftonline.com/tfp/"
                        && reconciledApplicationParameters.IsB2C
                        && !string.IsNullOrEmpty(reconciledApplicationParameters.Domain)
                        && reconciledApplicationParameters.Domain.EndsWith(".onmicrosoft.com", StringComparison.OrdinalIgnoreCase))
                        {
                            replacement = "https://" + reconciledApplicationParameters.Domain.Replace(".onmicrosoft.com", ".b2clogin.com", StringComparison.OrdinalIgnoreCase)
                                .Replace("aadB2CInstance", reconciledApplicationParameters.Domain1, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            replacement = reconciledApplicationParameters.Instance;
                        }
                    }
                    break;
                case "Application.ConfigurationSection":
                    replacement = null;
                    break;
                case "Application.AppIdUri":
                    replacement = reconciledApplicationParameters.AppIdUri;
                    break;
                case "Application.ExtraQueryParameters":
                    replacement = null;
                    break;


                default:
                    Console.WriteLine($"{replaceBy} not known");
                    break;
            }
            return replacement;
        }
    }
}
