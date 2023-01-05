// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Microsoft.Identity.Web.Test.Common.TestHelpers
{
    public class TestOptionsMonitor<T> : IOptionsMonitor<T>
        where T : class, new()
    {
        private Action<T, string>? _listener;

        public TestOptionsMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public T CurrentValue { get; private set; }

        public T Get(string? name)
        {
            return CurrentValue;
        }

        public void Set(T value)
        {
            CurrentValue = value;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            _listener?.Invoke(value, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        public IDisposable OnChange(Action<T, string> listener)
        {
            _listener = listener;
            return Substitute.For<IDisposable>();
        }
    }
}
