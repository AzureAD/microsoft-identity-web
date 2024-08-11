// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Microsoft.Identity.Web
{
    internal static class IdHelper
    {
        private const string IDWebSku = "IDWeb.";

        private static readonly Lazy<string> s_idWebVersion = new Lazy<string>(
           () =>
           {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
               string fullVersion = typeof(IdHelper).GetTypeInfo().Assembly.FullName;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
               var regex = new Regex(@"Version=[\d]+.[\d+]+.[\d]+.[\d]+");
#pragma warning disable CS8604
               Match? match = regex.Match(fullVersion);
#pragma warning restore CS8604
               if (!match.Success)
               {
                   return string.Empty;
               }

               string[] version = match.Groups[0].Value.Split(
                   new[]
                   {
                        '=',
                   },
                   StringSplitOptions.None);
               return version[1];
           });

        public static string GetIdWebVersion()
        {             
            return s_idWebVersion.Value;
        }

        public static string CreateTelemetryInfo()
        {
            return string.Format(CultureInfo.InvariantCulture, IDWebSku + s_idWebVersion.Value);
        }
    }
}
