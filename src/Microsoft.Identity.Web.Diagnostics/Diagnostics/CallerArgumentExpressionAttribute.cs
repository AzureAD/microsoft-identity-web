#if !NETCOREAPP3_1_OR_GREATER
#pragma warning disable IDE0073 // The file header does not match the required text
                               // Licensed to the .NET Foundation under one or more agreements.
                               // The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
#pragma warning restore IDE0073 // The file header does not match the required text
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }
}
#endif
