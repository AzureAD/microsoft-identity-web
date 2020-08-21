// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ConfigureGeneratedApplications.Model
{
    public class File
    {
        public static PropertyMapping[] s_emptyPropertyMappings = new PropertyMapping[0];

        public static Replacement[] s_emptyReplacements = new Replacement[0];

        public string FileRelativePath { get; set; }

        public PropertyMapping[] Properties { get; set; } = s_emptyPropertyMappings;

        public Replacement[] Replacements { get; set; } = s_emptyReplacements;
    }
}
