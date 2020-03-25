// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class Base64UrlHelpersTests
    {
        [Fact]
        public void Encode_NullByteArray_ReturnsNull()
        {
            byte[] byteArrayToEncode = null;
            Assert.Null(Base64UrlHelpers.Encode(byteArrayToEncode));
        }

        [Fact]
        public void Encode_UTF8ByteArrayOfDecodedString_ReturnsValidEncodedString()
        {
            var stringToEncodeNoPadding = "123456";
            var expectedEncodedString = "MTIzNDU2";

            var actualEncodedString = Base64UrlHelpers.Encode(Encoding.UTF8.GetBytes(stringToEncodeNoPadding));

            Assert.Equal(expectedEncodedString, actualEncodedString);

            var stringToEncode1Padding = "12345678";
            var expectedEncodedString1Padding = "MTIzNDU2Nzg";

            actualEncodedString = Base64UrlHelpers.Encode(Encoding.UTF8.GetBytes(stringToEncode1Padding));

            Assert.Equal(expectedEncodedString1Padding, actualEncodedString);

            var stringToEncode2Padding = "1234567";
            var expectedEncodedString2Padding = "MTIzNDU2Nw";

            actualEncodedString = Base64UrlHelpers.Encode(Encoding.UTF8.GetBytes(stringToEncode2Padding));

            Assert.Equal(expectedEncodedString2Padding, actualEncodedString);

            var stringToEncodeWithBase64Plus = "12>123";
            var expectedEncodedStringWithBase64Plus = "MTI-MTIz";

            actualEncodedString = Base64UrlHelpers.Encode(Encoding.UTF8.GetBytes(stringToEncodeWithBase64Plus));

            Assert.Equal(expectedEncodedStringWithBase64Plus, actualEncodedString);

            var stringToEncodeWithBase64Slash = "12?123";
            var expectedEncodedStringWithBase64Slash = "MTI_MTIz";

            actualEncodedString = Base64UrlHelpers.Encode(Encoding.UTF8.GetBytes(stringToEncodeWithBase64Slash));

            Assert.Equal(expectedEncodedStringWithBase64Slash, actualEncodedString);

            var emptyStringToEncode = string.Empty;

            actualEncodedString = Base64UrlHelpers.Encode(Encoding.UTF8.GetBytes(emptyStringToEncode));

            Assert.Equal(emptyStringToEncode, actualEncodedString);
        }

        [Fact]
        public void Encode_NullString_ReturnsNull()
        {
            string stringToEncode = null;
            Assert.Null(Base64UrlHelpers.Encode(stringToEncode));
        }

        [Fact]
        public void Encode_DecodedString_ReturnsEncodedString()
        {
            var stringToEncodeNoPadding = "123456";
            var expectedEncodedString = "MTIzNDU2";
            
            var actualEncodedString = Base64UrlHelpers.Encode(stringToEncodeNoPadding);

            Assert.Equal(expectedEncodedString, actualEncodedString);

            var stringToEncode1Padding = "12345678";
            var expectedEncodedString1Padding = "MTIzNDU2Nzg";

            actualEncodedString = Base64UrlHelpers.Encode(stringToEncode1Padding);

            Assert.Equal(expectedEncodedString1Padding, actualEncodedString);

            var stringToEncode2Padding = "1234567";
            var expectedEncodedString2Padding = "MTIzNDU2Nw";

            actualEncodedString = Base64UrlHelpers.Encode(stringToEncode2Padding);

            Assert.Equal(expectedEncodedString2Padding, actualEncodedString);

            var stringToEncodeWithBase64Plus = "12>123";
            var expectedEncodedStringWithBase64Plus = "MTI-MTIz";

            actualEncodedString = Base64UrlHelpers.Encode(stringToEncodeWithBase64Plus);

            Assert.Equal(expectedEncodedStringWithBase64Plus, actualEncodedString);

            var stringToEncodeWithBase64Slash = "12?123";
            var expectedEncodedStringWithBase64Slash = "MTI_MTIz";

            actualEncodedString = Base64UrlHelpers.Encode(stringToEncodeWithBase64Slash);

            Assert.Equal(expectedEncodedStringWithBase64Slash, actualEncodedString);

            var emptyStringToEncode = string.Empty;

            actualEncodedString = Base64UrlHelpers.Encode(emptyStringToEncode);

            Assert.Equal(emptyStringToEncode, actualEncodedString);
        }

        [Fact]
        public void DecodeToString_ValidBase64UrlString_ReturnsDecodedString()
        {
            var stringToDecodeNoPadding = "MTIzNDU2";
            var expectedDecodedString = "123456";

            var actualDecodedString = Base64UrlHelpers.DecodeToString(stringToDecodeNoPadding);

            Assert.Equal(expectedDecodedString, actualDecodedString);

            var stringToDecode1Padding = "MTIzNDU2Nzg";
            var expectedDecodedString1Padding = "12345678";

            actualDecodedString = Base64UrlHelpers.DecodeToString(stringToDecode1Padding);

            Assert.Equal(expectedDecodedString1Padding, actualDecodedString);

            var stringToDecode2Padding = "MTIzNDU2Nw";
            var expectedDecodedString2Padding = "1234567";

            actualDecodedString = Base64UrlHelpers.DecodeToString(stringToDecode2Padding);

            Assert.Equal(expectedDecodedString2Padding, actualDecodedString);

            var stringToDecodeWithBase64Plus = "MTI-MTIz";
            var expectedDecodedStringWithBase64Plus = "12>123";

            actualDecodedString = Base64UrlHelpers.DecodeToString(stringToDecodeWithBase64Plus);

            Assert.Equal(expectedDecodedStringWithBase64Plus, actualDecodedString);

            var stringToDecodeWithBase64Slash = "MTI_MTIz";
            var expectedEncodedStringWithBase64Slash = "12?123";

            actualDecodedString = Base64UrlHelpers.DecodeToString(stringToDecodeWithBase64Slash);

            Assert.Equal(expectedEncodedStringWithBase64Slash, actualDecodedString);

            var emptyStringToDecode = string.Empty;

            actualDecodedString = Base64UrlHelpers.DecodeToString(emptyStringToDecode);

            Assert.Equal(emptyStringToDecode, actualDecodedString);
        }

        [Fact]
        public void CreateString_UTF8Bytes_ReturnsValidString()
        {
            var stringToCreate = "123456";

            var resultString = Base64UrlHelpers.CreateString(Encoding.UTF8.GetBytes(stringToCreate));

            Assert.Equal(stringToCreate, resultString);

            stringToCreate = string.Empty;

            resultString = Base64UrlHelpers.CreateString(Encoding.UTF8.GetBytes(stringToCreate));

            Assert.Equal(stringToCreate, resultString);
        }

        [Fact]
        public void CreateString_NonUTF8Bytes_ReturnsInvalidString()
        {
            var stringToCreate = "123456";

            var resultString = Base64UrlHelpers.CreateString(Encoding.UTF32.GetBytes(stringToCreate));

            Assert.NotEqual(stringToCreate, resultString);

            resultString = Base64UrlHelpers.CreateString(Encoding.Unicode.GetBytes(stringToCreate));

            Assert.NotEqual(stringToCreate, resultString);
        }

        [Fact]
        public void DecodeToBytes_ValidBase64UrlString_ReturnsByteArray()
        {
            var stringToDecodeWithNoPadding = "MTIzNDU2";
            var expectedDecodedByteArray = Encoding.UTF8.GetBytes("123456");

            var actualDecodedByteArray = Base64UrlHelpers.DecodeToBytes(stringToDecodeWithNoPadding);

            Assert.Equal(expectedDecodedByteArray, actualDecodedByteArray);

            var stringToDecodeWith1Padding = "MTIzNDU2Nzg";
            expectedDecodedByteArray = Encoding.UTF8.GetBytes("12345678");

            actualDecodedByteArray = Base64UrlHelpers.DecodeToBytes(stringToDecodeWith1Padding);

            Assert.Equal(expectedDecodedByteArray, actualDecodedByteArray);

            var stringToDecodeWith2Padding = "MTIzNDU2Nw";
            expectedDecodedByteArray = Encoding.UTF8.GetBytes("1234567");

            actualDecodedByteArray = Base64UrlHelpers.DecodeToBytes(stringToDecodeWith2Padding);

            Assert.Equal(expectedDecodedByteArray, actualDecodedByteArray);

            var stringToDecodeWithBase64Plus = "MTI-MTIz";
            expectedDecodedByteArray = Encoding.UTF8.GetBytes("12>123");

            actualDecodedByteArray = Base64UrlHelpers.DecodeToBytes(stringToDecodeWithBase64Plus);

            Assert.Equal(expectedDecodedByteArray, actualDecodedByteArray);

            var stringToDecodeWithBase64Slash = "MTI_MTIz";
            expectedDecodedByteArray = Encoding.UTF8.GetBytes("12?123");

            actualDecodedByteArray = Base64UrlHelpers.DecodeToBytes(stringToDecodeWithBase64Slash);

            Assert.Equal(expectedDecodedByteArray, actualDecodedByteArray);

            var emptyStringToDecode = string.Empty;
            expectedDecodedByteArray = Encoding.UTF8.GetBytes(emptyStringToDecode);

            actualDecodedByteArray = Base64UrlHelpers.DecodeToBytes(emptyStringToDecode);

            Assert.Equal(expectedDecodedByteArray, actualDecodedByteArray);
        }

        [Fact]
        public void DecodeToBytes_InvalidBase64UrlStringLength_ThrowsException()
        {
            var stringToDecodeWithInvalidLength = "MTIzNDU21";

            Action decodeAction = () => Base64UrlHelpers.DecodeToBytes(stringToDecodeWithInvalidLength);

            var exception = Assert.Throws<ArgumentException>(decodeAction);
            Assert.Equal("Illegal base64url string! (Parameter 'arg')", exception.Message);
        }
    }
}
