// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using ConfigureGeneratedApplications.Model;
using File = ConfigureGeneratedApplications.Model.File;

namespace ConfigureGeneratedApplications
{
    class Program
    {
        static void Main(string[] args)
        {
            string executionFolder = Path.GetDirectoryName(System.AppContext.BaseDirectory);
            string repoRoot = Path.Combine(executionFolder, "..", "..", "..", "..", "..");
            string configurationDefaultFilePath = Path.Combine(repoRoot, "ProjectTemplates", "Configuration.json");
            string defaultFolderToConfigure = Path.Combine(repoRoot, @"ProjectTemplates\bin\Debug\tests");

            string configurationFilePath = args.Length > 0 ? args[0] : configurationDefaultFilePath;
            string folderToConfigure = args.Length > 1 ? args[1] : defaultFolderToConfigure;

            Configuration configuration = JsonSerializer.Deserialize(System.IO.File.ReadAllText(configurationFilePath),ConfigurationJsonSerializerContext.Default.Configuration);
            foreach (Project project in configuration.Projects)
            {
                ProcessProject(folderToConfigure, configuration, project);
            }

            ProcessReplacements();

            GenerateIssueMdText(configuration, defaultFolderToConfigure);

        }

        private static void GenerateIssueMdText(Configuration configuration, string folder)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("## Test the generated templates");
            builder.AppendLine(@"1. Use `TESTING.md` file for steps for testing locally and for testing a release.");
            builder.AppendLine("2. Test the following projects: ");
            foreach (Project p in configuration.Projects)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"   - [ ] {p.ProjectRelativeFolder}");
            }

            System.IO.File.WriteAllText(Path.Combine(folder, "issue.md"), builder.ToString());
        }

        static List<Replacement> replacements = new List<Replacement>();

        private static void ProcessProject(string folderToConfigure, Configuration configuration, Project project)
        {
            string projectPath = Path.Combine(folderToConfigure, project.ProjectRelativeFolder);

            foreach (File file in project.GetMergedFiles(configuration.Projects))
            {
                string filePath = Path.Combine(projectPath, file.FileRelativePath).Replace('/', '\\');
                ProcessFile(configuration, filePath, file);
            }
        }

        private static void ProcessFile(Configuration configuration, string filePath, File file)
        {
            Console.WriteLine($"{filePath}");

            if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                if (System.IO.File.Exists(filePath))
                {
                    string fileContent = System.IO.File.ReadAllText(filePath);
                    JsonElement jsonContent = JsonSerializer.Deserialize(fileContent,ConfigurationJsonSerializerContext.Default.JsonElement);

                    foreach (PropertyMapping propertyMapping in file.Properties)
                    {
                        string value = configuration.GetParameterValue(propertyMapping.SetFrom);
                        Console.WriteLine($"{propertyMapping.Property} = '{value}'");
                        string[] path = propertyMapping.Property.Split(':');
                        var element = jsonContent;

                        foreach (string segment in path)
                        {
                            JsonProperty prop = element.EnumerateObject().FirstOrDefault(e => e.Name == segment);
                            element = prop.Value;
                        }

                        string replaceFrom = element.ValueKind == JsonValueKind.Number ? element.GetInt32().ToString(CultureInfo.InvariantCulture) : element.ToString();
                        int index = GetIndex(element);
                        int length = replaceFrom.Length;
                        string replaceBy = configuration.GetParameterValue(propertyMapping.SetFrom);

                        AddReplacement(filePath, index, length, replaceFrom, replaceBy);
                    }
                }
                else
                {
                    Console.Error.WriteLine($"File doesn't exist `{filePath}`");
                }
            }

            if (file.Replacements != null && file.Replacements.Any())
            {
                foreach (Replacement r in file.Replacements)
                {
                    r.FilePath = filePath;
                }
                replacements.AddRange(file.Replacements);
            }
        }

        private static int GetIndex(JsonElement element)
        {
            Type type = element.GetType();
            object _idx = type.GetField("_idx",
                                        BindingFlags.NonPublic | BindingFlags.Instance).GetValue(element);
            return (int)_idx;
        }

        private static void AddReplacement(string filePath, int index, int length, string replaceFrom, string replaceBy)
        {
            replacements.Add(new Replacement(filePath, index, length, replaceFrom, replaceBy));
        }

        private static void ProcessReplacements()
        {
            foreach (var replacementsPerFile in replacements.GroupBy(r => r.FilePath))
            {
                string filePath = replacementsPerFile.Key;
                IEnumerable<Replacement> replacements = replacementsPerFile.OrderByDescending(r => r.Index).ToArray();

                string fileContent = System.IO.File.ReadAllText(filePath);
                foreach (Replacement r in replacements)
                {
                    if (r.ReplaceFrom != "")
                    {
                        fileContent = fileContent.Replace(r.ReplaceFrom, r.ReplaceBy, StringComparison.OrdinalIgnoreCase);
                    }
                }

                if (!System.IO.File.Exists(filePath + "%"))
                {
                    System.IO.File.Move(filePath, filePath + "%");
                }
                System.IO.File.WriteAllText(filePath, fileContent);
            }
        }
    }
}
