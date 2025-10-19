# Scenario: Using the Sidecar from TypeScript

Complete guide for TypeScript/Node.js applications.

## Setup

```bash
npm install node-fetch
npm install --save-dev @types/node-fetch
```

## Sidecar Client Library

```typescript
// sidecar-client.ts
import fetch from 'node-fetch';

export interface SidecarConfig {
  baseUrl: string;
  timeout?: number;
}

export class SidecarClient {
  private readonly baseUrl: string;
  private readonly timeout: number;
  
  constructor(config: SidecarConfig) {
    this.baseUrl = config.baseUrl || process.env.SIDECAR_URL || 'http://localhost:5000';
    this.timeout = config.timeout || 10000;
  }
  
  async getAuthorizationHeader(
    incomingToken: string,
    serviceName: string,
    options?: {
      scopes?: string[];
      tenant?: string;
      agentIdentity?: string;
      agentUsername?: string;
    }
  ): Promise<string> {
    const url = new URL(`${this.baseUrl}/AuthorizationHeader/${serviceName}`);
    
    if (options?.scopes) {
      options.scopes.forEach(scope => 
        url.searchParams.append('optionsOverride.Scopes', scope)
      );
    }
    
    if (options?.tenant) {
      url.searchParams.append('optionsOverride.AcquireTokenOptions.Tenant', options.tenant);
    }
    
    if (options?.agentIdentity) {
      url.searchParams.append('AgentIdentity', options.agentIdentity);
      if (options.agentUsername) {
        url.searchParams.append('AgentUsername', options.agentUsername);
      }
    }
    
    const response = await fetch(url.toString(), {
      headers: { 'Authorization': incomingToken },
      signal: AbortSignal.timeout(this.timeout)
    });
    
    if (!response.ok) {
      throw new Error(`Sidecar error: ${response.statusText}`);
    }
    
    const data = await response.json();
    return data.authorizationHeader;
  }
  
  async callDownstreamApi<T>(
    incomingToken: string,
    serviceName: string,
    relativePath: string,
    options?: {
      method?: string;
      body?: any;
      scopes?: string[];
    }
  ): Promise<T> {
    const url = new URL(`${this.baseUrl}/DownstreamApi/${serviceName}`);
    url.searchParams.append('optionsOverride.RelativePath', relativePath);
    
    if (options?.method && options.method !== 'GET') {
      url.searchParams.append('optionsOverride.HttpMethod', options.method);
    }
    
    if (options?.scopes) {
      options.scopes.forEach(scope => 
        url.searchParams.append('optionsOverride.Scopes', scope)
      );
    }
    
    const fetchOptions: any = {
      method: options?.method || 'GET',
      headers: { 'Authorization': incomingToken },
      signal: AbortSignal.timeout(this.timeout)
    };
    
    if (options?.body) {
      fetchOptions.headers['Content-Type'] = 'application/json';
      fetchOptions.body = JSON.stringify(options.body);
    }
    
    const response = await fetch(url.toString(), fetchOptions);
    
    if (!response.ok) {
      throw new Error(`Sidecar error: ${response.statusText}`);
    }
    
    const data = await response.json();
    
    if (data.statusCode >= 400) {
      throw new Error(`API error ${data.statusCode}: ${data.content}`);
    }
    
    return JSON.parse(data.content) as T;
  }
}

// Usage
const sidecar = new SidecarClient({ baseUrl: 'http://localhost:5000' });

// Get authorization header
const authHeader = await sidecar.getAuthorizationHeader(token, 'Graph');

// Call API
interface UserProfile {
  displayName: string;
  mail: string;
  userPrincipalName: string;
}

const profile = await sidecar.callDownstreamApi<UserProfile>(
  token,
  'Graph',
  'me'
);
```

## Express.js Integration

```typescript
import express from 'express';
import { SidecarClient } from './sidecar-client';

const app = express();
app.use(express.json());

const sidecar = new SidecarClient({ baseUrl: process.env.SIDECAR_URL! });

// Middleware to extract token
app.use((req, res, next) => {
  const token = req.headers.authorization;
  if (!token && !req.path.startsWith('/health')) {
    return res.status(401).json({ error: 'No authorization token' });
  }
  req.userToken = token;
  next();
});

// Routes
app.get('/api/profile', async (req, res) => {
  try {
    const profile = await sidecar.callDownstreamApi(
      req.userToken,
      'Graph',
      'me'
    );
    res.json(profile);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

app.get('/api/messages', async (req, res) => {
  try {
    const messages = await sidecar.callDownstreamApi(
      req.userToken,
      'Graph',
      'me/messages?$top=10'
    );
    res.json(messages);
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

app.listen(8080, () => {
  console.log('Server running on port 8080');
});
```

## NestJS Integration

```typescript
import { Injectable } from '@nestjs/common';
import { SidecarClient } from './sidecar-client';

@Injectable()
export class GraphService {
  private readonly sidecar: SidecarClient;
  
  constructor() {
    this.sidecar = new SidecarClient({ 
      baseUrl: process.env.SIDECAR_URL! 
    });
  }
  
  async getUserProfile(token: string) {
    return await this.sidecar.callDownstreamApi(
      token,
      'Graph',
      'me'
    );
  }
  
  async getUserMessages(token: string, top: number = 10) {
    return await this.sidecar.callDownstreamApi(
      token,
      'Graph',
      `me/messages?$top=${top}`
    );
  }
}
```

## Best Practices

1. **Reuse Client Instance**: Create SidecarClient once and reuse
2. **Set Appropriate Timeouts**: Configure based on API latency
3. **Handle Errors**: Implement proper error handling and retry logic
4. **Type Safety**: Use TypeScript interfaces for API responses
5. **Connection Pooling**: Use HTTP agent for connection reuse

## Next Steps

- [Call Downstream API](call-downstream-api.md) - Detailed API calling examples
- [Obtain Authorization Header](obtain-authorization-header.md) - Get tokens directly
- [Agent Identities](../agent-identities.md) - Use agent identities
