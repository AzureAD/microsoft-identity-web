// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace DotnetTool
{
    public class Change
    {
        public Change(string description)
        {
            Description = description;
        }

        public string Description { get; set; }
    }
}
