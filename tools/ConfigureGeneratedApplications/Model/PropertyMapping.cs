// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ConfigureGeneratedApplications.Model
{
    public class PropertyMapping
    {
        public string Property { get; set; }

        public string SetFrom { get; set; }

        public override string ToString()
        {
            return Property;
        }
    }
}
