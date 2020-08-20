using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using ConfigureGeneratedApplications.Model;
using File = ConfigureGeneratedApplications.Model.File;

namespace ConfigureGeneratedApplications
{
    class Program
    {
        static void Main(string[] args)
        {
            string executionFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string repoRoot = Path.Combine(executionFolder, "..", "..", "..", "..", "..");
            string configurationDefaultFilePath = Path.Combine(repoRoot, "ProjectTemplates", "Configuration.json");
            string defaultFolderToConfigure = Path.Combine(repoRoot, @"ProjectTemplates\bin\Debug\tests");

            string configurationFilePath = args.Length > 0 ? args[0] : configurationDefaultFilePath;
            string folderToConfigure = args.Length > 1 ? args[1] :  defaultFolderToConfigure;

            string configurationFileContent = System.IO.File.ReadAllText(configurationFilePath);
            Configuration configuration = JsonSerializer.Deserialize<Configuration>(configurationFileContent);

            foreach(Project project in configuration.Projects)
            {
                ProcessProject(folderToConfigure, configuration, project);
            }

            ProcessReplacements();

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

            if (filePath.EndsWith(".json"))
            {
                string fileContent = System.IO.File.ReadAllText(filePath);
                JsonElement jsonContent = JsonSerializer.Deserialize<JsonElement>(fileContent, 
                        new JsonSerializerOptions() 
                        { ReadCommentHandling = JsonCommentHandling.Skip 
                        });

                foreach (PropertyMapping propertyMapping in file.Properties)
                {
                    string value = configuration.GetParameterValue(propertyMapping.SetFrom);
                    Console.WriteLine($"{propertyMapping.Property} = '{value}'");

                    string[] path = propertyMapping.Property.Split(':');

                    JsonElement element = jsonContent;
                    foreach (string segment in path)
                    {
                        JsonProperty prop = element.EnumerateObject().FirstOrDefault(e => e.Name == segment);
                        element = prop.Value;
                    }

                    string replaceFrom = element.ValueKind == JsonValueKind.Number ? element.GetInt32().ToString() : element.ToString();
                    int index = GetIndex(element);
                    int length = replaceFrom.Length;
                    string replaceBy = configuration.GetParameterValue(propertyMapping.SetFrom);

                    AddReplacement(filePath, index, length, replaceFrom, replaceBy);
                }

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
            foreach(var replacementsPerFile in replacements.GroupBy(r => r.FilePath))
            {
                string filePath = replacementsPerFile.Key;
                IEnumerable<Replacement> replacements = replacementsPerFile.OrderByDescending(r => r.Index).ToArray();

                string fileContent = System.IO.File.ReadAllText(filePath);
                foreach(Replacement r in replacements)
                {

                    // Does not work.
                    // string replacedString = fileContent.Substring(r.Index, r.Length);
                    // if (replacedString != r.ReplaceFrom)
                    // {
                    //    throw new ApplicationException("there must be an error in this tool's implementation");
                    //}
                    // fileContent = fileContent.Substring(0, r.Index) + r.ReplaceBy + fileContent.Substring(r.Index + r.Length);
                    fileContent = fileContent.Replace(r.ReplaceFrom, r.ReplaceBy);
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
