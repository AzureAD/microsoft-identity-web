// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT-License.

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1055:Uri return values should not be strings", Justification = "We want a string.", Scope = "member", Target = "~M:Microsoft.Identity.Web.CalledApiOptions.GetApiUrl~System.String")]
