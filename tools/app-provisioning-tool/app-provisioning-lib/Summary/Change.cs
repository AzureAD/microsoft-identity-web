// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.App
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
