// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace client.Data
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
	public class Response<T> where T : class
	{
        /// <summary>
        /// 
        /// </summary>
		[JsonPropertyName("@odata.context")]
		public string? OdataContext { get; set; }

        /// <summary>
        /// 
        /// </summary>
		[JsonPropertyName("value")]
		public T[]? Value { get; set; }
	}
}
