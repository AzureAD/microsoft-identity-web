// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Abstractions;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class WithExtraBodyParametersTests
    {
        [Fact]
        public void WithExtraBodyParameters_NullOptions_ThrowsArgumentNullException()
        {
            AcquireTokenOptions options = null!;
            var dict = new Dictionary<string, string> { { "key", "value" } };

            Assert.Throws<ArgumentNullException>(() => options.WithExtraBodyParameters(dict));
        }

        [Fact]
        public void WithExtraBodyParameters_NullDictionary_ReturnsSameOptions()
        {
            var options = new AcquireTokenOptions();

            var result = options.WithExtraBodyParameters(null!);

            Assert.Same(options, result);
        }

        [Fact]
        public void WithExtraBodyParameters_EmptyDictionary_ReturnsSameOptions()
        {
            var options = new AcquireTokenOptions();

            var result = options.WithExtraBodyParameters(new Dictionary<string, string>());

            Assert.Same(options, result);
        }

        [Fact]
        public async Task WithExtraBodyParameters_AddsParametersToExtraParameters()
        {
            var options = new AcquireTokenOptions();
            var dict = new Dictionary<string, string> { { "key1", "val1" } };

            options.WithExtraBodyParameters(dict);

            Assert.NotNull(options.ExtraParameters);
            Assert.True(options.ExtraParameters.ContainsKey(Constants.ExtraBodyParametersKey));

            var asyncParams = options.ExtraParameters[Constants.ExtraBodyParametersKey]
                as Dictionary<string, Func<CancellationToken, Task<string>>>;
            Assert.NotNull(asyncParams);
            Assert.True(asyncParams.ContainsKey("key1"));
            Assert.Equal("val1", await asyncParams["key1"](CancellationToken.None));
        }

        [Fact]
        public void WithExtraBodyParameters_InitializesExtraParametersIfNull()
        {
            var options = new AcquireTokenOptions();
            Assert.Null(options.ExtraParameters);

            options.WithExtraBodyParameters(new Dictionary<string, string> { { "k", "v" } });

            Assert.NotNull(options.ExtraParameters);
        }

        [Fact]
        public async Task WithExtraBodyParameters_MergesWithExistingParameters()
        {
            var options = new AcquireTokenOptions();

            options.WithExtraBodyParameters(new Dictionary<string, string> { { "key1", "val1" } });
            options.WithExtraBodyParameters(new Dictionary<string, string> { { "key2", "val2" } });

            var asyncParams = options.ExtraParameters![Constants.ExtraBodyParametersKey]
                as Dictionary<string, Func<CancellationToken, Task<string>>>;
            Assert.NotNull(asyncParams);
            Assert.Equal(2, asyncParams.Count);
            Assert.Equal("val1", await asyncParams["key1"](CancellationToken.None));
            Assert.Equal("val2", await asyncParams["key2"](CancellationToken.None));
        }

        [Fact]
        public async Task WithExtraBodyParameters_OverwritesExistingKey()
        {
            var options = new AcquireTokenOptions();

            options.WithExtraBodyParameters(new Dictionary<string, string> { { "key1", "original" } });
            options.WithExtraBodyParameters(new Dictionary<string, string> { { "key1", "updated" } });

            var asyncParams = options.ExtraParameters![Constants.ExtraBodyParametersKey]
                as Dictionary<string, Func<CancellationToken, Task<string>>>;
            Assert.NotNull(asyncParams);
            Assert.Single(asyncParams);
            Assert.Equal("updated", await asyncParams["key1"](CancellationToken.None));
        }

        [Fact]
        public void WithExtraBodyParameters_PreservesOtherExtraParameters()
        {
            var options = new AcquireTokenOptions
            {
                ExtraParameters = new Dictionary<string, object>
                {
                    { "other_key", "other_value" }
                }
            };

            options.WithExtraBodyParameters(new Dictionary<string, string> { { "key1", "val1" } });

            Assert.True(options.ExtraParameters.ContainsKey("other_key"));
            Assert.Equal("other_value", options.ExtraParameters["other_key"]);
            Assert.True(options.ExtraParameters.ContainsKey(Constants.ExtraBodyParametersKey));
        }

        [Fact]
        public async Task WithExtraBodyParameters_AsyncFuncsReturnCorrectValues()
        {
            var options = new AcquireTokenOptions();
            var dict = new Dictionary<string, string>
            {
                { "param1", "value1" },
                { "param2", "value2" },
                { "param3", "value3" }
            };

            options.WithExtraBodyParameters(dict);

            var asyncParams = options.ExtraParameters![Constants.ExtraBodyParametersKey]
                as Dictionary<string, Func<CancellationToken, Task<string>>>;
            Assert.NotNull(asyncParams);
            Assert.Equal(3, asyncParams.Count);

            foreach (var kvp in dict)
            {
                string result = await asyncParams[kvp.Key](CancellationToken.None);
                Assert.Equal(kvp.Value, result);
            }
        }

        [Fact]
        public async Task WithExtraBodyParameters_DifferentParamsProduceDifferentCacheEntries()
        {
            var options1 = new AcquireTokenOptions();
            var options2 = new AcquireTokenOptions();

            options1.WithExtraBodyParameters(new Dictionary<string, string> { { "key", "valueA" } });
            options2.WithExtraBodyParameters(new Dictionary<string, string> { { "key", "valueB" } });

            var asyncParams1 = options1.ExtraParameters![Constants.ExtraBodyParametersKey]
                as Dictionary<string, Func<CancellationToken, Task<string>>>;
            var asyncParams2 = options2.ExtraParameters![Constants.ExtraBodyParametersKey]
                as Dictionary<string, Func<CancellationToken, Task<string>>>;

            Assert.NotNull(asyncParams1);
            Assert.NotNull(asyncParams2);
            Assert.NotSame(asyncParams1, asyncParams2);

            string val1 = await asyncParams1["key"](CancellationToken.None);
            string val2 = await asyncParams2["key"](CancellationToken.None);
            Assert.NotEqual(val1, val2);
            Assert.Equal("valueA", val1);
            Assert.Equal("valueB", val2);
        }
    }
}
