// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Web
{
	internal static class ErrorCodes
	{
		public const string MissingClientCredentials = "missing_client_credentials";
		public const string DuplicateClientCredentials = "duplicate_client_credentials";

		// AzureAD B2C
		public const string B2CPasswordResetErrorCode = "AADSTS50013";
		public const string B2CForgottenPassword = "AADB2C90118";
		public const string AccessDenied = "access_denied";
	}
}
