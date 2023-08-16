// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Web.Test.Common.Mocks
{
    public class ReplaceMockHttpMessageHandlerEventArgs : EventArgs
    {
        public MockHttpMessageHandler MockHttpMessageHandler { get; set; }
    }
}
