// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Identity.Web.Test
{
    public class TheoryDataBase : TheoryData
    {
        public TheoryDataBase(string testId)
        {
            TestId = testId;
        }

        public string TestId { get; set; } = string.Empty;

        public override string ToString()
        {
            return TestId;
        }
    }
}
