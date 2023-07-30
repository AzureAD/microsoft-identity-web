// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Identity.Web.Test.Common.TestHelpers
{
    public class ExternalApp
    {
        public static Process? Start(Type testType, string pathToAppToStart, string nameOfAppToStart, string ?arguments=null)
        {
            string thisTestAssemblyLocation = testType.Assembly.Location;
            string thisTestAssemblyFolder = Path.Combine(Path.GetDirectoryName(thisTestAssemblyLocation)!);
            string[] segments = thisTestAssemblyFolder.Split(Path.DirectorySeparatorChar);
            int numberSegments = segments.Length;
            int startLastSegments = numberSegments - 3;
            int endFirstSegments = startLastSegments - 2;
            string oidcProxy = Path.Combine(
                Path.Combine(segments.Take(endFirstSegments).ToArray()),
                pathToAppToStart,
                Path.Combine(segments.Skip(startLastSegments).ToArray()),
                nameOfAppToStart);
            ProcessStartInfo processStartInfo = new ProcessStartInfo(oidcProxy);

            if (!string.IsNullOrEmpty(arguments))
            {
                processStartInfo.Arguments = arguments;
            }
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            Process? process = Process.Start(processStartInfo);
            return process;
        }
    }
}
