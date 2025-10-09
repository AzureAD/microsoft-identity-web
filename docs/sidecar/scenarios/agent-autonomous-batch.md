# Scenario: Agent Autonomous Batch Processing

This guide demonstrates using agent identities for autonomous batch processing without user context.

## Overview

Autonomous agents operate in application context to:
- Process batch jobs
- Perform scheduled tasks
- Execute background operations
- Handle system-to-system workflows

## Prerequisites

- Sidecar deployed with agent identity configuration
- Agent identity registered in Microsoft Entra ID
- Application permissions granted

## Agent Identity Setup

### Create Agent Identity

```bash
# Create agent identity app registration
az ad app create --display-name "Batch Processing Agent"

AGENT_ID=$(az ad app list --display-name "Batch Processing Agent" --query [0].appId -o tsv)

# Grant application permissions
az ad app permission add \
  --id $AGENT_ID \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions df021288-bdef-4463-88db-98f22de89214=Role  # User.Read.All

# Grant admin consent
az ad app permission admin-consent --id $AGENT_ID
```

## Implementation

### TypeScript

```typescript
async function processBatchWithAgent(
  incomingToken: string,
  agentIdentity: string
) {
  const sidecarUrl = process.env.SIDECAR_URL!;
  
  // Get users list using agent identity (autonomous)
  const response = await fetch(
    `${sidecarUrl}/DownstreamApi/Graph?` +
    `AgentIdentity=${agentIdentity}&` +
    `optionsOverride.RelativePath=users&` +
    `optionsOverride.RequestAppToken=true`,
    {
      headers: {
        'Authorization': incomingToken
      }
    }
  );
  
  const result = await response.json();
  const users = JSON.parse(result.content);
  
  // Process each user
  for (const user of users.value) {
    await processUser(user);
  }
}

async function scheduledReportGeneration() {
  const agentIdentity = process.env.AGENT_CLIENT_ID!;
  const token = await getSystemToken();
  
  // Generate reports for all departments
  const departments = await getDepartments(token, agentIdentity);
  
  for (const dept of departments) {
    await generateDepartmentReport(token, agentIdentity, dept);
  }
}
```

### Python

```python
def process_batch_with_agent(incoming_token: str, agent_identity: str):
    """Process batch using autonomous agent."""
    sidecar_url = os.getenv('SIDECAR_URL', 'http://localhost:5000')
    
    # Get users using agent identity
    response = requests.get(
        f"{sidecar_url}/DownstreamApi/Graph",
        params={
            'AgentIdentity': agent_identity,
            'optionsOverride.RelativePath': 'users',
            'optionsOverride.RequestAppToken': 'true'
        },
        headers={'Authorization': incoming_token}
    )
    
    result = response.json()
    users = json.loads(result['content'])
    
    # Process each user
    for user in users['value']:
        process_user(user)
```

## Batch Processing Patterns

### Scheduled Job

```typescript
// Cron-based batch processor
import cron from 'node-cron';

// Run every day at 2 AM
cron.schedule('0 2 * * *', async () => {
  console.log('Starting nightly batch process');
  
  try {
    await runAutonomousBatch(
      process.env.AGENT_CLIENT_ID!
    );
    console.log('Batch completed successfully');
  } catch (error) {
    console.error('Batch failed:', error);
  }
});
```

### Queue-Based Processing

```typescript
// Process messages from queue
async function processQueueMessages(queueClient, agentIdentity: string) {
  while (true) {
    const messages = await queueClient.receiveMessages(10);
    
    for (const message of messages) {
      try {
        await processMessage(message, agentIdentity);
        await queueClient.deleteMessage(message);
      } catch (error) {
        console.error('Message processing failed:', error);
      }
    }
    
    await sleep(5000);
  }
}
```

## Best Practices

1. **Use Application Permissions**: Grant appropriate app permissions
2. **Implement Retry Logic**: Handle transient failures
3. **Monitor Progress**: Track batch job progress
4. **Limit Scope**: Request only necessary permissions
5. **Audit Operations**: Log all agent actions
6. **Handle Throttling**: Respect API rate limits

## Next Steps

- [Agent Identities](../agent-identities.md) - Detailed agent identity documentation
- [Configuration Reference](../configuration.md) - Configure agent settings
- [Security Best Practices](../security.md) - Secure agent operations
