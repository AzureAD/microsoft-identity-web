// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Web.Test
{
    internal class TestTelemetryClient : ITelemetryClient
    {
        private const string EventName = "acquire_token";
        public TelemetryEventDetails TestTelemetryEventDetails { get; set; }

        public TestTelemetryClient(string clientId)
        {
            ClientId = clientId;
            TestTelemetryEventDetails = new MsalTelemetryEventDetails(EventName);
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
            return EventName.Equals(eventName, StringComparison.Ordinal);
        }

        public void TrackEvent(TelemetryEventDetails eventDetails)
        {
            TestTelemetryEventDetails = eventDetails;
        }

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        public void TrackEvent(string eventName, IDictionary<string, string> stringProperties, IDictionary<string, long> longProperties, IDictionary<string, bool> boolProperties, IDictionary<string, DateTime> dateTimeProperties, IDictionary<string, double> doubleProperties, IDictionary<string, Guid> guidProperties)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
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
