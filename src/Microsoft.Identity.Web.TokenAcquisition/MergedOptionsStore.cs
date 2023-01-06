// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace Microsoft.Identity.Web
{
    internal class MergedOptionsStore : IMergedOptionsStore
    {
        private readonly ConcurrentDictionary<string, MergedOptions> _options;

        public MergedOptionsStore()
        {
            _options = new ConcurrentDictionary<string, MergedOptions>();
        }

        public MergedOptions Get(string name)
        {
            return _options.GetOrAdd(name, key => new MergedOptions());
        }
    }
}
