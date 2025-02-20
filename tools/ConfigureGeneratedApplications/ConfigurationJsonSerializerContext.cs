// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using System.Text.Json;

using ConfigureGeneratedApplications.Model;

namespace ConfigureGeneratedApplications
{
    [JsonSourceGenerationOptions(ReadCommentHandling = JsonCommentHandling.Skip)]
    [JsonSerializable(typeof(Configuration))]
    [JsonSerializable(typeof(Project))]
    [JsonSerializable(typeof(File))]
    [JsonSerializable(typeof(PropertyMapping))]
    [JsonSerializable(typeof(Replacement))]
    [JsonSerializable(typeof(JsonElement))]
    internal partial class ConfigurationJsonSerializerContext : JsonSerializerContext
    {
    }
}
