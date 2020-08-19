using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigureGeneratedApplications.Model
{
    public class Project
    {
        static File[] emptyFiles = new File[0];

        public string ProjectRelativeFolder { get; set; }
        public string BasedOn { get; set; }

        public override string ToString()
        {
            return ProjectRelativeFolder;
        }

        public File[] Files { get; set; }

        public Project GetBasedOnProject(IEnumerable<Project> projects)
        {
            Project baseProject = projects.FirstOrDefault(p => p.ProjectRelativeFolder == BasedOn);
            if (!string.IsNullOrEmpty(BasedOn) && baseProject == null)
            {
                throw new FormatException($"In Project {ProjectRelativeFolder} BasedOn = {BasedOn} could not be found");
            }
            return baseProject;
        }

        /// <summary>
        /// Get all the files with including merged from BaseOn project recursively
        /// merging all the properties
        /// </summary>
        /// <param name="projects"></param>
        /// <returns></returns>
        public IEnumerable<File> GetMergedFiles(IEnumerable<Project> projects)
        {
            IEnumerable<File> files = GetBasedOnProject(projects)?.Files ?? emptyFiles;
            IEnumerable<File> allFiles = Files != null ? files.Union(Files) : files;
            var allFilesGrouped = allFiles.GroupBy(f => f.FileRelativePath);
            foreach(var fileGrouping in allFilesGrouped)
            {
                yield return new File
                {
                    FileRelativePath = fileGrouping.Key,
                    Properties = fileGrouping.SelectMany(f => f.Properties).ToArray()
                };
            }
        }
    }
}
