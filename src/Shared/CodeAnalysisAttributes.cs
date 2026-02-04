// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This file provides polyfills for AOT-related attributes on older target frameworks
// that don't have them built-in. It is shared across multiple projects via MSBuild linking.

// Suppress public API analyzer warnings for these internal polyfill types
#pragma warning disable RS0016 // Symbol is not part of the declared public API
#pragma warning disable RS0036 // Symbol is not part of the declared internal API
#pragma warning disable RS0051 // Symbol is not part of the declared API

// These attributes are available in .NET 5.0+ but need polyfills for netstandard2.0 and .NET Framework
#if NETSTANDARD2_0 || NETFRAMEWORK

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Indicates that the specified method requires dynamic access to code that is not referenced statically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false)]
    internal sealed class RequiresDynamicCodeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiresDynamicCodeAttribute"/> class.
        /// </summary>
        /// <param name="message">A message that contains information about the usage of dynamic code.</param>
        public RequiresDynamicCodeAttribute(string message)
        {
            Message = message;
        }

        /// <summary>
        /// Gets a message that contains information about the usage of dynamic code.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets or sets an optional URL that contains more information about the method.
        /// </summary>
        public string? Url { get; set; }
    }

    /// <summary>
    /// Indicates that the specified method requires the ability to generate new code at runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false)]
    internal sealed class RequiresUnreferencedCodeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiresUnreferencedCodeAttribute"/> class.
        /// </summary>
        /// <param name="message">A message that contains information about the usage of unreferenced code.</param>
        public RequiresUnreferencedCodeAttribute(string message)
        {
            Message = message;
        }

        /// <summary>
        /// Gets a message that contains information about the usage of unreferenced code.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets or sets an optional URL that contains more information about the method.
        /// </summary>
        public string? Url { get; set; }
    }

    /// <summary>
    /// Specifies the types of members that are dynamically accessed.
    /// </summary>
    [Flags]
    internal enum DynamicallyAccessedMemberTypes
    {
        /// <summary>Specifies no members.</summary>
        None = 0,
        /// <summary>Specifies the default, parameterless public constructor.</summary>
        PublicParameterlessConstructor = 1,
        /// <summary>Specifies all public constructors.</summary>
        PublicConstructors = 3,
        /// <summary>Specifies all non-public constructors.</summary>
        NonPublicConstructors = 4,
        /// <summary>Specifies all public methods.</summary>
        PublicMethods = 8,
        /// <summary>Specifies all non-public methods.</summary>
        NonPublicMethods = 16,
        /// <summary>Specifies all public fields.</summary>
        PublicFields = 32,
        /// <summary>Specifies all non-public fields.</summary>
        NonPublicFields = 64,
        /// <summary>Specifies all public nested types.</summary>
        PublicNestedTypes = 128,
        /// <summary>Specifies all non-public nested types.</summary>
        NonPublicNestedTypes = 256,
        /// <summary>Specifies all public properties.</summary>
        PublicProperties = 512,
        /// <summary>Specifies all non-public properties.</summary>
        NonPublicProperties = 1024,
        /// <summary>Specifies all public events.</summary>
        PublicEvents = 2048,
        /// <summary>Specifies all non-public events.</summary>
        NonPublicEvents = 4096,
        /// <summary>Specifies all interfaces implemented by the type.</summary>
        Interfaces = 8192,
        /// <summary>Specifies all members.</summary>
        All = -1
    }

    /// <summary>
    /// Indicates that certain members on a specified Type are accessed dynamically.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter |
        AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method |
        AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
        Inherited = false)]
    internal sealed class DynamicallyAccessedMembersAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicallyAccessedMembersAttribute"/> class.
        /// </summary>
        /// <param name="memberTypes">The types of members dynamically accessed.</param>
        public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
        {
            MemberTypes = memberTypes;
        }

        /// <summary>
        /// Gets the <see cref="DynamicallyAccessedMemberTypes"/> which specifies the type of members dynamically accessed.
        /// </summary>
        public DynamicallyAccessedMemberTypes MemberTypes { get; }
    }

    /// <summary>
    /// Suppresses reporting of a specific rule violation, allowing multiple suppressions on a single code artifact.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    internal sealed class UnconditionalSuppressMessageAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnconditionalSuppressMessageAttribute"/> class.
        /// </summary>
        /// <param name="category">The category for the attribute.</param>
        /// <param name="checkId">The identifier of the analysis tool rule to be suppressed.</param>
        public UnconditionalSuppressMessageAttribute(string category, string checkId)
        {
            Category = category;
            CheckId = checkId;
        }

        /// <summary>Gets the category identifying the classification of the attribute.</summary>
        public string Category { get; }

        /// <summary>Gets the identifier of the analysis tool rule to be suppressed.</summary>
        public string CheckId { get; }

        /// <summary>Gets or sets the scope of the code that is relevant for the attribute.</summary>
        public string? Scope { get; set; }

        /// <summary>Gets or sets a fully qualified path that represents the target of the attribute.</summary>
        public string? Target { get; set; }

        /// <summary>Gets or sets an optional argument expanding on exclusion criteria.</summary>
        public string? MessageId { get; set; }

        /// <summary>Gets or sets the justification for suppressing the code analysis message.</summary>
        public string? Justification { get; set; }
    }
}

#pragma warning restore RS0016
#pragma warning restore RS0036
#pragma warning restore RS0051

#endif
