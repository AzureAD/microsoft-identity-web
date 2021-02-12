using DotnetTool.AuthenticationParameters;
using DotnetTool.Project;
using System.Collections.Generic;

namespace DotnetTool.CodeReaderWriter
{
    public class ProjectAuthenticationSettings
    {
        public ProjectAuthenticationSettings(ProjectDescription projectDescription)
        {
            ProjectDescription = projectDescription;
        }

        public ApplicationParameters ApplicationParameters { get; } = new ApplicationParameters();

        public List<Replacement> Replacements { get; } = new List<Replacement>();

        public ProjectDescription ProjectDescription { get; private set; }
    }
}
