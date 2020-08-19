using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json;
using ConfigureGeneratedApplications.Model;
using File = ConfigureGeneratedApplications.Model.File;
using System.Linq;

namespace ConfigureGeneratedApplications
{
    class Program
    {
        static void Main(string[] args)
        {
            string configurationFilePath = args.Length > 0 ? args[0] : @"C:\gh\microsoft-identity-web\configuration.json";
            string folderToConfigure = @"C:\gh\microsoft-identity-web\ProjectTemplates\bin\Debug\tests";

            string configurationFileContent = System.IO.File.ReadAllText(configurationFilePath);
            Configuration configuration = JsonSerializer.Deserialize<Configuration>(configurationFileContent);

            foreach(Project project in configuration.Projects)
            {
                string projectPath = Path.Combine(folderToConfigure, project.ProjectRelativeFolder);
                
                foreach(File file in project.GetMergedFiles(configuration.Projects))
                {
                    string filePath = Path.Combine(projectPath, file.FileRelativePath).Replace('/', '\\');
                    Console.WriteLine($"{filePath}");

                    if (filePath.EndsWith(".json"))
                    {
                        string fileContent = System.IO.File.ReadAllText(filePath);
                        JsonElement jsonContent = JsonSerializer.Deserialize<JsonElement>(fileContent, new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip })
                        ;

                        foreach (PropertyMapping propertyMapping in file.Properties)
                        {
                            string value = configuration.GetParameterValue(propertyMapping.SetFrom);
                            Console.WriteLine($"{propertyMapping.Property} = '{value}'");

                            string[] path = propertyMapping.Property.Split(':');

                            JsonElement element = jsonContent;
                            foreach(string segment in path)
                            {
                                JsonProperty prop = element.EnumerateObject().FirstOrDefault(e => e.Name == segment);
                                element = prop.Value;
                            }
                        }

                    } 
                }
            }
        }
    }
}
