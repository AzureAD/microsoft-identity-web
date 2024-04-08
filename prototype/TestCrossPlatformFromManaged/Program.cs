// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace TestCrossPlatformFromManaged
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var options = new MicrosoftIdentityApplicationOptions()
            {
                Authority = Marshal.StringToHGlobalAnsi("https://login.microsoftonline.com/common"),
                Instance = Marshal.StringToHGlobalAnsi("https://login.microsoftonline.com/"),
                TenantId = Marshal.StringToHGlobalAnsi("common"),
                Audience = Marshal.StringToHGlobalAnsi("api://a1b2c3d4"),
                Audiences = IntPtr.Zero,
                AudiencesCount = 0,
            };
            IntPtr ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(options));
            Marshal.StructureToPtr(options, ptr, false);
            DllImports.IdentityWebConfigure(ptr);

            IntPtr ptr2 = Marshal.AllocCoTaskMem(Marshal.SizeOf<MicrosoftIdentityValidationInput>());
            Marshal.StructureToPtr(new MicrosoftIdentityValidationInput()
            {
                AuthorizationHeader = Marshal.StringToCoTaskMemUTF8("Bearer token")
            }, ptr2, false);

            IntPtr result = DllImports.Validate(ptr2);
            var output = Marshal.PtrToStructure<MicrosoftIdentityValidationOutput>(result);
            Console.WriteLine(output.HttpResponseStatusCode);
            Console.WriteLine(Marshal.PtrToStringUni(output.ErrorDescription));
            Console.WriteLine(Marshal.PtrToStringUni(output.WwwAuthenticate));
            Console.WriteLine(output.ClaimsCount);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MicrosoftIdentityApplicationOptions
    {
        public IntPtr Authority;
        public IntPtr Instance;
        public IntPtr TenantId;
        public IntPtr Audience;
        public IntPtr Audiences;
        public int AudiencesCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MicrosoftIdentityValidationInput
    {
        public IntPtr AuthorizationHeader;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MicrosoftIdentityValidationOutput
    {
        public int HttpResponseStatusCode;
        public IntPtr ErrorDescription;
        public IntPtr WwwAuthenticate;
        public IntPtr Claims;
        public int ClaimsCount;
    }
}
