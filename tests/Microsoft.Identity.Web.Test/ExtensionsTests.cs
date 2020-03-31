// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class ExtensionsTests
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("search", "")]
        [InlineData("search", "search")]
        [InlineData("searchString", "search")]
        [InlineData("search string", "string")]
        [InlineData("search string", "ch str")]
        [InlineData("search search string", "search")]
        [InlineData("search string", "string", "search")]
        [InlineData("search string", "string", "alsoString")]
        public void ContainsAny_CollectionContainsInput_ReturnsTrue(string str, params string[] stringCollection)
        {
            Assert.True(str.ContainsAny(stringCollection));
        }

        [Theory]
        [InlineData("", "s")]
        [InlineData("search", "string")]
        [InlineData("searchString", "notSearch")]
        [InlineData("search string", "  ")]
        [InlineData("search string", "notIncludedString", "alsoString")]
        public void ContainsAny_CollectionDoesntContainInput_ReturnsFalse(string str, params string[] stringCollection)
        {
            Assert.False(str.ContainsAny(stringCollection));
        }
    }
}
