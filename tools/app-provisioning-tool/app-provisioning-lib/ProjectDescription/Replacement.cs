// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.App.Project
{
    public class Replacement
    {
        public Replacement()
        {
            FilePath = string.Empty;
            ReplaceFrom = string.Empty;
            ReplaceBy = string.Empty;
            Property = string.Empty;
        }

        public Replacement(string filePath, int index, int length, string replaceFrom, string replaceBy, string property)
        {
            FilePath = filePath;
            Index = index;
            Length = length;
            ReplaceFrom = replaceFrom;
            ReplaceBy = replaceBy;
            Property = property;
        }
        public string FilePath { get; set; }
        public int Index { get; set; }
        public int Length { get; set; }
        public string ReplaceFrom { get; set; }
        public string ReplaceBy { get; set; }
        public string Property { get; set; }

        public override string ToString()
        {
            return $"Replace '{Property}' from '{ReplaceFrom}' by '{ReplaceBy}'";
        }
    }
}
