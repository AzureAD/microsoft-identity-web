# Scenario: Long-Running On-Behalf-Of (OBO)

This guide demonstrates how to implement long-running OBO scenarios where processes extend beyond the lifetime of the original user token.

## Overview

In long-running OBO scenarios:
1. User initiates an operation (e.g., data export, report generation)
2. Operation takes hours or days to complete
3. Background process needs to call APIs on behalf of the user
4. Original user token expires during processing

The sidecar handles token refresh automatically using refresh tokens.

## Prerequisites

- Sidecar deployed and running
- Application configured with appropriate refresh token lifetime
- Downstream APIs configured

## Configuration

Configure longer refresh token lifetime in Microsoft Entra ID:

```bash
# Set refresh token lifetime (via Microsoft Graph PowerShell)
Connect-MgGraph -Scopes "Policy.Read.All", "Policy.ReadWrite.ApplicationConfiguration"

# Create token lifetime policy (example: 90 days)
$params = @{
    Definition = @(
        '{"TokenLifetimePolicy":{"Version":1,"AccessTokenLifetime":"1:00:00","RefreshTokenMaxInactiveTime":"90.00:00:00","RefreshTokenMaxAge":"90.00:00:00"}}'
    )
    DisplayName = "LongRunningOBOPolicy"
    IsOrganizationDefault = $false
}

New-MgPolicyTokenLifetimePolicy -BodyParameter $params
```

## Implementation Pattern

### Store User Context

```typescript
// When user initiates long-running task
interface UserContext {
  userId: string;
  userPrincipalName: string;
  originalToken: string;
  taskId: string;
  createdAt: Date;
}

async function initiateLongRunningTask(incomingToken: string): Promise<string> {
  // Extract user information from token
  const tokenClaims = decodeToken(incomingToken);
  
  const taskId = generateTaskId();
  
  // Store user context
  const userContext: UserContext = {
    userId: tokenClaims.oid,
    userPrincipalName: tokenClaims.upn,
    originalToken: incomingToken,
    taskId: taskId,
    createdAt: new Date()
  };
  
  await storeUserContext(taskId, userContext);
  
  // Start background process
  await queueBackgroundTask(taskId);
  
  return taskId;
}
```

### Background Processing

```typescript
async function processLongRunningTask(taskId: string) {
  // Retrieve user context
  const userContext = await getUserContext(taskId);
  
  // Use stored token with sidecar - refresh handled automatically
  try {
    // Step 1: Process data
    const data = await fetchData(userContext.originalToken);
    
    // Step 2: Generate report (may take hours)
    const report = await generateReport(data);
    
    // Step 3: Upload to user's OneDrive
    await uploadToOneDrive(userContext.originalToken, report);
    
    // Step 4: Send notification
    await sendNotification(userContext.originalToken, userContext.userId);
    
    await markTaskComplete(taskId);
  } catch (error) {
    // Handle token expiration
    if (isTokenExpiredError(error)) {
      await markTaskFailed(taskId, 'User token expired and could not be refreshed');
    } else {
      await markTaskFailed(taskId, error.message);
    }
  }
}

async function uploadToOneDrive(token: string, report: Buffer) {
  // Sidecar automatically handles token refresh
  const response = await fetch(
    `${sidecarUrl}/DownstreamApi/Graph?optionsOverride.RelativePath=me/drive/root:/reports/report.pdf:/content`,
    {
      method: 'PUT',
      headers: {
        'Authorization': token,
        'Content-Type': 'application/pdf'
      },
      body: report
    }
  );
  
  return await response.json();
}
```

### Periodic Token Refresh

```typescript
// Proactively refresh tokens before expiration
async function refreshTokenPeriodically(taskId: string) {
  const userContext = await getUserContext(taskId);
  
  // Call sidecar to refresh token
  const response = await fetch(
    `${sidecarUrl}/AuthorizationHeader/Graph`,
    {
      headers: {
        'Authorization': userContext.originalToken
      }
    }
  );
  
  if (response.ok) {
    const data = await response.json();
    // Extract new token
    const newToken = data.authorizationHeader;
    
    // Update stored context
    userContext.originalToken = newToken;
    await updateUserContext(taskId, userContext);
  }
}
```

## Python Example

```python
import asyncio
from datetime import datetime, timedelta
import requests

class LongRunningTaskProcessor:
    def __init__(self, sidecar_url: str):
        self.sidecar_url = sidecar_url
    
    async def process_task(self, task_id: str, user_token: str):
        """Process a long-running task using the user's token."""
        try:
            # Step 1: Fetch data
            data = await self.fetch_data(user_token)
            
            # Step 2: Process (may take hours)
            await asyncio.sleep(3600)  # Simulate long processing
            result = await self.process_data(data)
            
            # Step 3: Upload result
            await self.upload_result(user_token, result)
            
            # Step 4: Notify user
            await self.notify_user(user_token, task_id)
            
        except Exception as e:
            print(f"Task {task_id} failed: {e}")
            # Handle failure
    
    async def fetch_data(self, token: str):
        """Fetch data from API - token refresh handled by sidecar."""
        response = requests.get(
            f"{self.sidecar_url}/DownstreamApi/Graph",
            params={'optionsOverride.RelativePath': 'me/messages'},
            headers={'Authorization': token}
        )
        response.raise_for_status()
        return response.json()
    
    async def upload_result(self, token: str, result):
        """Upload result to user's OneDrive."""
        response = requests.put(
            f"{self.sidecar_url}/DownstreamApi/Graph",
            params={'optionsOverride.RelativePath': 'me/drive/root:/results/output.json:/content'},
            headers={'Authorization': token},
            json=result
        )
        response.raise_for_status()
```

## Best Practices

1. **Store Minimal Context**: Only store necessary user information
2. **Encrypt Tokens**: Encrypt stored tokens at rest
3. **Set Expiration**: Set expiration on stored contexts
4. **Handle Refresh Failures**: Gracefully handle when refresh tokens expire
5. **Monitor Token Lifetime**: Track refresh token usage and lifetime
6. **User Notification**: Notify users of long-running task status
7. **Cleanup**: Remove completed task contexts

## Token Expiration Handling

```typescript
async function callApiWithRetry(
  token: string,
  apiCall: (token: string) => Promise<any>,
  maxRetries: number = 3
): Promise<any> {
  for (let attempt = 1; attempt <= maxRetries; attempt++) {
    try {
      return await apiCall(token);
    } catch (error) {
      if (attempt < maxRetries) {
        // Wait and retry
        await new Promise(resolve => setTimeout(resolve, 1000 * attempt));
        continue;
      }
      throw error;
    }
  }
}
```

## Security Considerations

1. **Secure Storage**: Encrypt tokens in storage
2. **Access Control**: Restrict access to stored contexts
3. **Audit Logging**: Log all token usage
4. **Expiration Policies**: Implement maximum task duration
5. **User Consent**: Inform users of long-running operations
6. **Revocation**: Support user-initiated task cancellation

## Limitations

- Refresh tokens have maximum lifetime (typically 90 days)
- User may revoke consent during processing
- Conditional access policies may change
- MFA requirements may interrupt processing

## Next Steps

- [Configuration Reference](../configuration.md) - Token lifetime settings
- [Security Best Practices](../security.md) - Secure token storage
- [Troubleshooting](../troubleshooting.md) - Handle token issues
- [Agent Identities](../agent-identities.md) - Alternative patterns
