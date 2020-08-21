// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

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
            IEnumerable<File> files = GetBasedOnProject(projects)?.GetMergedFiles(projects) ?? emptyFiles;
            IEnumerable<File> allFiles = Files != null ? files.Union(Files) : files;
            var allFilesGrouped = allFiles.GroupBy(f => f.FileRelativePath);
            foreach(var fileGrouping in allFilesGrouped)
            {
                yield return new File
                {
                    FileRelativePath = fileGrouping.Key,
                    Properties = fileGrouping.SelectMany(f => f.Properties).ToArray(),
                    Replacements = fileGrouping.SelectMany(f => f.Replacements)
                                               .Select(r => new Replacement(fileGrouping.Key,
                                                                            0, 0, r.ReplaceFrom, r.ReplaceBy)
                                                          ).ToArray()
                };
            }
        }
    }
}
