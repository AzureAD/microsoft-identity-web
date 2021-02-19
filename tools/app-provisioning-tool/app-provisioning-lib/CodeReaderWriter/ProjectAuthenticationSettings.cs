// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.App.AuthenticationParameters;
using Microsoft.Identity.App.Project;
using System.Collections.Generic;

namespace Microsoft.Identity.App.CodeReaderWriter
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
