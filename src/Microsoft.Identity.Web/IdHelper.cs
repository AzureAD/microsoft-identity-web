// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Microsoft.Identity.Web
{
    internal static class IdHelper
    {
        private static readonly Lazy<string> s_idWebVersion = new Lazy<string>(
           () =>
           {
               string? fullVersion = typeof(IdHelper).GetTypeInfo().Assembly.FullName;
               var regex = new Regex(@"Version=[\d]+.[\d+]+.[\d]+.[\d]+");
               var match = regex.Match(fullVersion);
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
    }
}
