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

#if NET6_0_OR_GREATER
        [UnmanagedCallersOnly(EntryPoint = "Initialize")]
#endif
        public static void C_Initialize(IntPtr authority, IntPtr audience)
        { 
            string authorityString = Marshal.PtrToStringAnsi(authority);
            string audienceString = Marshal.PtrToStringAnsi(audience);

            requestValidator.Initialize(authorityString, audienceString);
        }

#if NET6_0_OR_GREATER
       [UnmanagedCallersOnly(EntryPoint = "Validate")]
#endif
        public static IntPtr C_Validate(IntPtr authorizationHeader)
        {
            string authorizationHeaderString = Marshal.PtrToStringAnsi(authorizationHeader);
            try
            {
                var result = requestValidator.Validate(authorizationHeaderString);
                return Marshal.StringToCoTaskMemAnsi(result.Issuer);
            }
            catch(Exception ex) 
            {
                return Marshal.StringToCoTaskMemAnsi(ex.ToString());
            }
            //string json = JsonSerializer.Serialize(result, SourceGenerationContext.Default.TokenValidationResult);
            //return Marshal.StringToCoTaskMemAnsi(json);

            // Todos:
            // Serialize the result we want as JSON
        }
    }

#if NET6_0_OR_GREATER
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(TokenValidationResult))]
    [JsonSerializable(typeof(long))]
    internal partial class SourceGenerationContext : JsonSerializerContext { }
#endif
}
