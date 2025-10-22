# Dependencies

## Why Microsoft.Identity.Web net462 targets depends on Microsoft.Extensions version 5+, whereas net472 targets depend on Microsoft.Extensions version 2+?

Microsoft.Identity.Web depends on Microsoft.Extensions. version 5+ for net462, but version 2+ for net472. Here is the reason:

When an application targeting .NET Framework consumes a NuGet package that ships only NETSTANDARD 2.0 target (as most of Microsoft.Extensions.* packages did before version 5), it will also pull all the .NET standard dependencies. These carry high risk to have versions conflicts when integrated with other SDKs, or legacy applications.

After the Microsoft.Extensions.* suite got moved to .NET runtime, the .NET team added specific targets (net462, net472) in addition to NETSTANDARD 2.0 (starting from version 5.0.0). Having versions of packages that have dedicated framework targets unblocked several big customers.

## Why targeting .NET 4.7.2 is better than .NET 4.6.2 when consuming NETSTANDARD 2.0 libraries.
In addition to the reason above, there are other issues with <NET 4.7.2 + NETSTANDARD2.0; there are a number of cryptography APIs that are present in the NETSTANDARD2.0 contract but actually don’t exist at targets < NET 4.7.2, which result in runtime exceptions (ECParams for instance).

Both dependency issues, and API contract with non-existent implementations are fixed with .NET 4.7.2 hence the recommendation to target >=4.7.2 when consuming .NETSTANDARD libraries.
