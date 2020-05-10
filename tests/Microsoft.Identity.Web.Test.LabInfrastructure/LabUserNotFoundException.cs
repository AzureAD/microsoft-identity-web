// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Web.Test.LabInfrastructure
{
    public class LabUserNotFoundException : Exception
    {
        public LabUserNotFoundException(UserQuery parameters, string message)
            : base(message)
        {
            Parameters = parameters;
        }

        public UserQuery Parameters { get; set; }
    }
}
