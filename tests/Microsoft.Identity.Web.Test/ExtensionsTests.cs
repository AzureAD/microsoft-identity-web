// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class ExtensionsTests
    {
        [Fact]
        public void ContainsAny_CollectionContainsInput_ReturnsTrue()
        {
            Assert.True(string.Empty.ContainsAny(string.Empty));
            Assert.True("search".ContainsAny(string.Empty));
            Assert.True("search".ContainsAny("search"));
            Assert.True("searchString".ContainsAny("search"));
            Assert.True("search string".ContainsAny("string"));
            Assert.True("search string".ContainsAny("ch str"));
            Assert.True("search search string".ContainsAny("search"));
            Assert.True("search string".ContainsAny("string", "search"));
            Assert.True("search string".ContainsAny("string", "alsoString"));
        }

        [Fact]
        public void ContainsAny_CollectionDoesntContainInput_ReturnsFalse()
        {
            Assert.False(string.Empty.ContainsAny("s"));
            Assert.False("search".ContainsAny("string"));
            Assert.False("searchString".ContainsAny("notSearch"));
            Assert.False("search string".ContainsAny("  "));
            Assert.False("search string".ContainsAny("notIncludedString", "alsoString"));
        }
    }
}
