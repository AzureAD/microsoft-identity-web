// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Text.Json;

namespace ConfigureGeneratedApplications.Model
{
    public class Configuration
    {
        public JsonElement Parameters { get; set; }

        public Project[] Projects { get; set; }

        public string GetParameterValue(string parameterName)
        {
            JsonProperty jsonProperty = Parameters.EnumerateObject().FirstOrDefault(p => p.Name == parameterName);
            return jsonProperty.Value.GetString();
        }
    }
}
