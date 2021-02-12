using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DotnetTool.Project
{
    public class ProjectDescriptionReader
    {
        public List<ProjectDescription> projectDescriptions { get; private set; } = new List<ProjectDescription>();

        public ProjectDescription? GetProjectDescription(string projectTypeIdentifier, string codeFolder)
        {
            string? projectTypeId = projectTypeIdentifier;
            if (string.IsNullOrEmpty(projectTypeId) || projectTypeId == "dotnet-")
            {
                projectTypeId = InferProjectType(codeFolder);
            }

            return projectTypeId != null ? ReadProjectDescription(projectTypeId) : null;
        }

        private ProjectDescription ReadProjectDescription(string projectTypeIdentifier)
        {
            ReadProjectDescriptions();

            return projectDescriptions.FirstOrDefault(projectDescription => projectDescription.Identifier == projectTypeIdentifier);
        }

        static JsonSerializerOptions serializerOptionsWithComments = new JsonSerializerOptions()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private ProjectDescription ReadDescriptionFromFileContent(byte[] fileContent)
        {
            string jsonText = Encoding.UTF8.GetString(fileContent);
            return JsonSerializer.Deserialize<ProjectDescription>(jsonText, serializerOptionsWithComments);
        }

        private string? InferProjectType(string codeFolder)
        {
            ReadProjectDescriptions();


            // TODO: could be both a Web app and WEB API.


            foreach (ProjectDescription projectDescription in projectDescriptions.Where(p => p.GetMergedMatchesForProjectType(projectDescriptions) != null))
            {
                var matchesForProjectTypes = projectDescription.GetMergedMatchesForProjectType(projectDescriptions);
                if (projectDescription.MatchesForProjectType != null)
                {
                    foreach (MatchesForProjectType matchesForProjectType in matchesForProjectTypes)
                    {
                        if (!string.IsNullOrEmpty(matchesForProjectType.FileRelativePath))
                        {
                            IEnumerable<string> files;

                            try
                            {
                                files = Directory.EnumerateFiles(codeFolder, matchesForProjectType.FileRelativePath);
                            }
                            catch (DirectoryNotFoundException)
                            {
                                files = new string[0];
                            }


                            foreach (string filePath in files)
                            {
                                // If there are matches, one at least needs to match
                                if (matchesForProjectType.MatchAny != null)
                                {
                                    string fileContent = File.ReadAllText(filePath);
                                    foreach (string match in matchesForProjectType.MatchAny!) // Valid project => 
                                    {
                                        if (fileContent.Contains(match))
                                        {
                                            return projectDescription.Identifier!;
                                        }
                                    }
                                }

                                // If MatchAny is null, then the presence of a file is enough
                                else
                                {
                                    return projectDescription.Identifier!;
                                }
                            }
                        }

                        if (matchesForProjectType.FolderRelativePath != null)
                        {
                            try
                            {
                                if (Directory.EnumerateDirectories(codeFolder, matchesForProjectType.FolderRelativePath).Any())
                                {
                                    return projectDescription.Identifier!;
                                }
                            }
                            catch
                            {
                                // No folder
                            }
                        }
                    }
                }
            }
            return null;
        }


        private void ReadProjectDescriptions()
        {
            if (projectDescriptions.Any())
            {
                return;
            }

            var properties = typeof(Microsoft.Identity.App.Properties.Resources).GetProperties(BindingFlags.Static | BindingFlags.NonPublic)
                .Where(p => p.PropertyType == typeof(byte[]))
                .ToArray();
            foreach (PropertyInfo propertyInfo in properties)
            {
                byte[] content = (propertyInfo.GetValue(null) as byte[])!;
                ProjectDescription projectDescription = ReadDescriptionFromFileContent(content);
                if (!projectDescription.IsValid())
                {
                    throw new FormatException($"Resource file {propertyInfo.Name} is missing Identitier or ProjectRelativeFolder is null.");
                }
                projectDescriptions.Add(projectDescription);
            }

            // TODO: provide an extension mechanism to add such files outside the tool.
            // In that case the validation would not be an exception? but would need to provide error messages
        }
    }

}