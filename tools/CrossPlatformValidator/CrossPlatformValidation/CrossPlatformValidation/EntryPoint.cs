// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

namespace CrossPlatformValidation
{
    /// <summary>
    /// 
    /// </summary>
    public class EntryPoint
    {
        public static RequestValidator requestValidator { get; } = new RequestValidator();

        [UnmanagedCallersOnly(EntryPoint = "Initialize")]
        public static void C_Initialize(IntPtr authority, IntPtr audience)
        { 
            string authorityString = Marshal.PtrToStringAnsi(authority);
            string audienceString = Marshal.PtrToStringAnsi(audience);

            requestValidator.Initialize(authorityString, audienceString);
        }

        [UnmanagedCallersOnly(EntryPoint = "Validate")]
        public static IntPtr C_Validate(IntPtr authorizationHeader)
        {
            string authorizationHeaderString = Marshal.PtrToStringAnsi(authorizationHeader);
            var result = requestValidator.Validate(authorizationHeaderString);
            //string json = JsonSerializer.Serialize(result, SourceGenerationContext.Default.TokenValidationResult);
            //return Marshal.StringToCoTaskMemAnsi(json);
            return Marshal.StringToCoTaskMemAnsi(result.Issuer);
        }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(TokenValidationResult))]
    [JsonSerializable(typeof(long))]
    internal partial class SourceGenerationContext : JsonSerializerContext { }
}
