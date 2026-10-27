// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class RedirectUriHelperTests
    {
        [Theory]
        // Local paths — accepted.
        [InlineData("/", true)]
        [InlineData("/home", true)]
        [InlineData("/home?query=1", true)]
        [InlineData("/a/b/c", true)]
        // Null/empty — rejected.
        [InlineData(null, false)]
        [InlineData("", false)]
        // Protocol-relative — rejected.
        [InlineData("//some.example", false)]
        [InlineData("//some.example/path", false)]
        // Slash-backslash — rejected.
        [InlineData("/\\some.example", false)]
        [InlineData("/\\\\some.example", false)]
        // Absolute URLs — rejected.
        [InlineData("https://some.example/", false)]
        [InlineData("http://some.example/", false)]
        [InlineData("javascript:alert(1)", false)]
        // Bare hostnames / non-slash-prefixed — rejected.
        [InlineData("some.example", false)]
        [InlineData("home", false)]
        // Percent-encoded slash/backslash — rejected (reverse proxies may decode these).
        [InlineData("/%2Fsome.example", false)]
        [InlineData("/%2fsome.example", false)]
        [InlineData("/%5Csome.example", false)]
        [InlineData("/%5csome.example", false)]
        [InlineData("/%2f%2fsome.example/x", false)]
        [InlineData("/%2F%5Csome.example", false)]
        // Control characters — rejected (browsers strip tab/CR/LF per the WHATWG URL spec,
        // resolving "/\t/evil.example" to a protocol-relative URL).
        [InlineData("/\t/some.example", false)]
        [InlineData("/\r/some.example", false)]
        [InlineData("/\n/some.example", false)]
        [InlineData("/\t\tsome.example", false)]
        [InlineData("/home\t", false)]
        [InlineData("/home\u0000", false)]
        [InlineData("/home\u007F", false)]
        [InlineData("\t//some.example", false)]
        public void IsLocalUrl_ValidatesCorrectly(string? input, bool expected)
        {
            Assert.Equal(expected, RedirectUriHelper.IsLocalUrl(input));
        }

        [Theory]
        // Control characters present — detected.
        [InlineData("/\tsome.example", true)]
        [InlineData("/\rsome.example", true)]
        [InlineData("/\nsome.example", true)]
        [InlineData("/home\u0000", true)]
        [InlineData("/home\u001F", true)]
        [InlineData("/home\u007F", true)]
        [InlineData("\u0000", true)]
        // No control characters — not detected. Space (U+0020) is not a control character.
        [InlineData("/home", false)]
        [InlineData("/home?query=1 2", false)]
        [InlineData("/a/b/c", false)]
        [InlineData("/", false)]
        [InlineData("", false)]
        public void HasControlCharacter_DetectsControlCharacters(string input, bool expected)
        {
            Assert.Equal(expected, RedirectUriHelper.HasControlCharacter(input));
        }

        [Theory]
        [InlineData("/%2Fsome.example", true)]
        [InlineData("/%2fsome.example", true)]
        [InlineData("/%5Csome.example", true)]
        [InlineData("/%5csome.example", true)]
        [InlineData("/%2f%2fsome.example/x", true)]
        [InlineData("/%2F%5Csome.example", true)]
        [InlineData("/home", false)]
        [InlineData("/a/b/c", false)]
        [InlineData("/", false)]
        public void HasPercentEncodedSlashPrefix_DetectsEncodedSlashes(string input, bool expected)
        {
            Assert.Equal(expected, RedirectUriHelper.HasPercentEncodedSlashPrefix(input));
        }
    }
}
