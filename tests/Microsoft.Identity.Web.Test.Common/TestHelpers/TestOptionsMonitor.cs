// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Microsoft.Identity.Web.Test.Common.TestHelpers
{
    public class TestOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    {
        private Action<TOptions, string> _listener;

        public TestOptionsMonitor(TOptions currentValue)
        {
            CurrentValue = currentValue;
        }

        public TOptions CurrentValue { get; private set; }

        public TOptions Get(string name)
        {
            return CurrentValue;
        }

        public void Set(TOptions value)
        {
            CurrentValue = value;
            _listener.Invoke(value, null);
        }

        public IDisposable OnChange(Action<TOptions, string> listener)
        {
            _listener = listener;
            return Substitute.For<IDisposable>();
        }
    }
}
