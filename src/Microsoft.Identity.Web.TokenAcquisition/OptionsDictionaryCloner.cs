// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Abstractions;

namespace Microsoft.Identity.Web
{
    internal static class OptionsDictionaryCloner
    {
        internal static IDictionary<string, TValue>? Clone<TValue>(IDictionary<string, TValue>? source)
        {
            return source switch
            {
                null => null,
                Dictionary<string, TValue> dictionary => new Dictionary<string, TValue>(dictionary, dictionary.Comparer),
                SortedDictionary<string, TValue> dictionary => new SortedDictionary<string, TValue>(dictionary, dictionary.Comparer),
                SortedList<string, TValue> dictionary => new SortedList<string, TValue>(dictionary, dictionary.Comparer),
                _ => new Dictionary<string, TValue>(source),
            };
        }

        internal static void Copy(AcquireTokenOptions source, AcquireTokenOptions target)
        {
            target.ExtraParameters = Clone(source.ExtraParameters);
            target.ExtraQueryParameters = Clone(source.ExtraQueryParameters);
            target.ExtraHeadersParameters = Clone(source.ExtraHeadersParameters);
        }
    }
}
