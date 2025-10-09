# Scenario: Managed / Workload Identity

Eliminate secrets in Azure Kubernetes Service or Azure environments.

## Steps

1. Configure workload identity or pod identity binding for your pod/app registration.
2. Set `AzureAd:ClientId`.
3. Omit `ClientCredentials`.

Acquire header:
```
GET /AuthorizationHeader/Graph
```

Select user-assigned MI:
```
GET /AuthorizationHeader/Graph?optionsOverride.AcquireTokenOptions.ManagedIdentity.UserAssignedClientId=<client-id>
```

Add `optionsOverride.RequestAppToken=true` if you need an app token vs user token.
