// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    internal class MergedOptionsStore : IMergedOptionsStore
    {
        private readonly ConcurrentDictionary<string, MergedOptions> _options;
        private readonly ILoggerFactory? _loggerFactory;

        public MergedOptionsStore(ILoggerFactory? loggerFactory)
        {
            _options = new ConcurrentDictionary<string, MergedOptions>();
            _loggerFactory = loggerFactory;
        }

        public MergedOptions Get(string name)
        {
            return _options.GetOrAdd(name, key => 
            {
                var mergedOptions = new MergedOptions();
                if (_loggerFactory != null)
                {
                    mergedOptions.Logger = _loggerFactory.CreateLogger<MergedOptions>();
                }
                return mergedOptions;
            });
        }
    }
}
