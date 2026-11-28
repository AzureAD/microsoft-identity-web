// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Naming", "VSTHRD200:Use Async suffix for async methods", Justification = "Native ABI Async exports complete through unmanaged callbacks, not awaitable tasks; managed method names must match the exported entry points.", Scope = "type", Target = "~T:Microsoft.Identity.Web.NativeAOT.Entrypoint")]
