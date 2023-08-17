// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Web.Test
{
    internal class TestTelemetryClient : ITelemetryClient
    {
        private const string _eventName = "acquire_token";
        public TelemetryEventDetails TestTelemetryEventDetails { get; set; }

        public TestTelemetryClient(string clientId)
        {
            ClientId = clientId;
            TestTelemetryEventDetails = new MsalTelemetryEventDetails(_eventName);
        }

        public string ClientId { get; set; }

        public void Initialize()
        {

        }

        public bool IsEnabled()
        {
            return true;
        }

        public bool IsEnabled(string eventName)
        {
            return _eventName.Equals(eventName, StringComparison.Ordinal);
        }

        public void TrackEvent(TelemetryEventDetails eventDetails)
        {
            TestTelemetryEventDetails = eventDetails;
        }

        public void TrackEvent(string eventName, IDictionary<string, string> stringProperties, IDictionary<string, long> longProperties, IDictionary<string, bool> boolProperties, IDictionary<string, DateTime> dateTimeProperties, IDictionary<string, double> doubleProperties, IDictionary<string, Guid> guidProperties)
        {
            throw new NotImplementedException();
        }
    }

    internal class MsalTelemetryEventDetails : TelemetryEventDetails
    {
        public MsalTelemetryEventDetails(string eventName)
        {
            Name = eventName;
        }
    }
}
