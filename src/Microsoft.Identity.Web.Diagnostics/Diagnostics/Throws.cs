// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// Defines static methods used to throw exceptions.
    /// </summary>
    /// <remarks>
    /// The main purpose is to reduce code size, improve performance, and standardize exception
    /// messages.
    /// </remarks>
    [SuppressMessage("Minor Code Smell", "S4136:Method overloads should be grouped together", Justification = "Doesn't work with the region layout")]
    internal static partial class Throws
    {
        #region For Object

        /// <summary>
        /// Throws an <see cref="System.ArgumentNullException"/> if the specified argument is <see langword="null"/>.
        /// </summary>
        /// <typeparam name="T">Argument type to be checked for <see langword="null"/>.</typeparam>
        /// <param name="argument">Object to be checked for <see langword="null"/>.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <returns>The original value of <paramref name="argument"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNull]
        public static T IfNull<T>([NotNull] T argument, [CallerArgumentExpression("argument")] string paramName = "")
        {
            if (argument is null)
            {
                ArgumentNullException(paramName);
            }

            return argument;
        }

        /// <summary>
        /// Throws an <see cref="System.ArgumentNullException"/> if the specified argument is <see langword="null"/>,
        /// or <see cref="System.ArgumentException" /> if the specified member is <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TParameter">Argument type to be checked for <see langword="null"/>.</typeparam>
        /// <typeparam name="TMember">Member type to be checked for <see langword="null"/>.</typeparam>
        /// <param name="argument">Argument to be checked for <see langword="null"/>.</param>
        /// <param name="member">Object member to be checked for <see langword="null"/>.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <param name="memberName">The name of the member.</param>
        /// <returns>The original value of <paramref name="member"/>.</returns>
        /// <example>
        /// <code>
        /// Throws.IfNullOrMemberNull(myObject, myObject?.MyProperty)
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNull]
        public static TMember IfNullOrMemberNull<TParameter, TMember>(
            [NotNull] TParameter argument,
            [NotNull] TMember member,
            [CallerArgumentExpression("argument")] string paramName = "",
            [CallerArgumentExpression("member")] string memberName = "")
        {
            if (argument is null)
            {
                ArgumentNullException(paramName);
            }

            if (member is null)
            {
                ArgumentException(paramName, $"Member {memberName} of {paramName} is null");
            }

            return member;
        }

        /// <summary>
        /// Throws an <see cref="System.ArgumentException" /> if the specified member is <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TParameter">Argument type.</typeparam>
        /// <typeparam name="TMember">Member type to be checked for <see langword="null"/>.</typeparam>
        /// <param name="argument">Argument to which member belongs.</param>
        /// <param name="member">Object member to be checked for <see langword="null"/>.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <param name="memberName">The name of the member.</param>
        /// <returns>The original value of <paramref name="member"/>.</returns>
        /// <example>
        /// <code>
        /// Throws.IfMemberNull(myObject, myObject.MyProperty)
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNull]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Analyzer isn't seeing the reference to 'argument' in the attribute")]
        public static TMember IfMemberNull<TParameter, TMember>(
            TParameter argument,
            [NotNull] TMember member,
            [CallerArgumentExpression("argument")] string paramName = "",
            [CallerArgumentExpression("member")] string memberName = "")
            where TParameter : notnull
        {
            if (member is null)
            {
                ArgumentException(paramName, $"Member {memberName} of {paramName} is null");
            }

            return member;
        }

        #endregion

        #region For String

        /// <summary>
        /// Throws either an <see cref="System.ArgumentNullException"/> or an <see cref="System.ArgumentException"/>
        /// if the specified string is <see langword="null"/> or whitespace respectively.
        /// </summary>
        /// <param name="argument">String to be checked for <see langword="null"/> or whitespace.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <returns>The original value of <paramref name="argument"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNull]
        public static string IfNullOrWhitespace([NotNull] string? argument, [CallerArgumentExpression("argument")] string paramName = "")
        {
#if !NET6_0_OR_GREATER
        if (argument == null)
        {
            ArgumentNullException(paramName);
        }
#endif

            if (string.IsNullOrWhiteSpace(argument))
            {
                if (argument == null)
                {
                    ArgumentNullException(paramName);
                }
                else
                {
                    ArgumentException(paramName, "Argument is whitespace");
                }
            }

            return argument;
        }

        /// <summary>
        /// Throws an <see cref="System.ArgumentNullException"/> if the string is <see langword="null"/>,
        /// or <see cref="System.ArgumentException"/> if it is empty.
        /// </summary>
        /// <param name="argument">String to be checked for <see langword="null"/> or empty.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <returns>The original value of <paramref name="argument"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNull]
        public static string IfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression("argument")] string paramName = "")
        {
#if !NET6_0_OR_GREATER
        if (argument == null)
        {
            ArgumentNullException(paramName);
        }
#endif

            if (string.IsNullOrEmpty(argument))
            {
                if (argument == null)
                {
                    ArgumentNullException(paramName);
                }
                else
                {
                    ArgumentException(paramName, "Argument is an empty string");
                }
            }

            return argument;
        }

        #endregion

        #region For Buffer

        /// <summary>
        /// Throws an <see cref="System.ArgumentException"/> if the argument's buffer size is less than the required buffer size.
        /// </summary>
        /// <param name="bufferSize">The actual buffer size.</param>
        /// <param name="requiredSize">The required buffer size.</param>
        /// <param name="paramName">The name of the parameter to be checked.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfBufferTooSmall(int bufferSize, int requiredSize, string paramName = "")
        {
            if (bufferSize < requiredSize)
            {
                ArgumentException(paramName, $"Buffer too small, needed a size of {requiredSize} but got {bufferSize}");
            }
        }

        #endregion

        #region For Enums

        /// <summary>
        /// Throws an <see cref="System.ArgumentOutOfRangeException"/> if the enum value is not valid.
        /// </summary>
        /// <param name="argument">The argument to evaluate.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <typeparam name="T">The type of the enumeration.</typeparam>
        /// <returns>The original value of <paramref name="argument"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T IfOutOfRange<T>(T argument, [CallerArgumentExpression("argument")] string paramName = "")
            where T : struct, Enum
        {
#if NET5_0_OR_GREATER
        if (!Enum.IsDefined<T>(argument))
#else
            if (!Enum.IsDefined(typeof(T), argument))
#endif
            {
                ArgumentOutOfRangeException(paramName, $"{argument} is an invalid value for enum type {typeof(T)}");
            }

            return argument;
        }

        #endregion

        #region For Collections

        /// <summary>
        /// Throws an <see cref="System.ArgumentNullException"/> if the collection is <see langword="null"/>,
        /// or <see cref="System.ArgumentException"/> if it is empty.
        /// </summary>
        /// <param name="argument">The collection to evaluate.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <typeparam name="T">The type of objects in the collection.</typeparam>
        /// <returns>The original value of <paramref name="argument"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNull]
        public static ICollection<T> IfNullOrEmpty<T>([NotNull] ICollection<T>? argument, [CallerArgumentExpression("argument")] string paramName = "")
        {
            if (argument == null)
            {
                ArgumentNullException(paramName);
            }
            else if (argument.Count == 0)
            {
                ArgumentException(paramName, "Collection is empty");
            }

            return argument;
        }

        /// <summary>
        /// Throws an <see cref="System.ArgumentNullException"/> if the collection is <see langword="null"/>,
        /// or <see cref="System.ArgumentException"/> if it is empty.
        /// </summary>
        /// <param name="argument">The collection to evaluate.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <typeparam name="T">The type of objects in the collection.</typeparam>
        /// <returns>The original value of <paramref name="argument"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNull]
        public static IReadOnlyCollection<T> IfNullOrEmpty<T>([NotNull] IReadOnlyCollection<T>? argument, [CallerArgumentExpression("argument")] string paramName = "")
        {
            if (argument == null)
            {
                ArgumentNullException(paramName);
            }
            else if (argument.Count == 0)
            {
                ArgumentException(paramName, "Collection is empty");
            }

            return argument;
        }

        #endregion

        #region Exceptions

        /// <summary>
        /// Throws an <see cref="System.ArgumentNullException"/>.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        public static void ArgumentNullException(string paramName)
            => throw new ArgumentNullException(paramName);

        /// <summary>
        /// Throws an <see cref="System.ArgumentNullException"/>.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <param name="message">A message that describes the error.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        public static void ArgumentNullException(string paramName, string? message)
            => throw new ArgumentNullException(paramName, message);

        /// <summary>
        /// Throws an <see cref="System.ArgumentOutOfRangeException"/>.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        public static void ArgumentOutOfRangeException(string paramName)
            => throw new ArgumentOutOfRangeException(paramName);

        /// <summary>
        /// Throws an <see cref="System.ArgumentOutOfRangeException"/>.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <param name="message">A message that describes the error.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        public static void ArgumentOutOfRangeException(string paramName, string? message)
            => throw new ArgumentOutOfRangeException(paramName, message);

        /// <summary>
        /// Throws an <see cref="System.ArgumentOutOfRangeException"/>.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <param name="actualValue">The value of the argument that caused this exception.</param>
        /// <param name="message">A message that describes the error.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        public static void ArgumentOutOfRangeException(string paramName, object? actualValue, string? message)
            => throw new ArgumentOutOfRangeException(paramName, actualValue, message);

        /// <summary>
        /// Throws an <see cref="System.ArgumentException"/>.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <param name="message">A message that describes the error.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        public static void ArgumentException(string paramName, string? message)
            => throw new ArgumentException(message, paramName);

        /// <summary>
        /// Throws an <see cref="System.ArgumentException"/>.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <param name="message">A message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <remarks>
        /// If the <paramref name="innerException"/> is not a <see langword="null"/>, the current exception is raised in a catch
        /// block that handles the inner exception.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        public static void ArgumentException(string paramName, string? message, Exception? innerException)
            => throw new ArgumentException(message, paramName, innerException);

        /// <summary>
        /// Throws an <see cref="System.InvalidOperationException"/>.
        /// </summary>
        /// <param name="message">A message that describes the error.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        public static void InvalidOperationException(string message)
            => throw new InvalidOperationException(message);

        /// <summary>
        /// Throws an <see cref="System.InvalidOperationException"/>.
        /// </summary>
        /// <param name="message">A message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        public static void InvalidOperationException(string message, Exception? innerException)
            => throw new InvalidOperationException(message, innerException);

        #endregion
    }
}
