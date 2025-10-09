# Scenario: Call a Downstream API

This guide demonstrates how to use the sidecar to acquire a token AND make the HTTP call to a downstream API in a single operation.

## Overview

In this scenario, the sidecar handles both:
1. Token acquisition (exchanging your incoming token for one scoped to the downstream API)
2. Making the HTTP request to the downstream API
3. Returning the response to your application

This simplifies your application code by delegating both authentication and HTTP communication to the sidecar.

## Prerequisites

- Sidecar deployed and running
- Downstream API configured in sidecar settings
- Valid bearer token from client

## Configuration

Configure the downstream API in your sidecar:

```yaml
env:
- name: DownstreamApis__Graph__BaseUrl
  value: "https://graph.microsoft.com/v1.0"
- name: DownstreamApis__Graph__Scopes
  value: "User.Read Mail.Read"
```

## Implementation Examples

### TypeScript/Node.js

```typescript
import fetch from 'node-fetch';

interface DownstreamApiResponse {
  statusCode: number;
  headers: Record<string, string>;
  content: string;
}

async function callDownstreamApi(
  incomingToken: string,
  serviceName: string,
  relativePath: string,
  method: string = 'GET',
  body?: any
): Promise<any> {
  const sidecarUrl = process.env.SIDECAR_URL || 'http://localhost:5000';
  
  const url = new URL(`${sidecarUrl}/DownstreamApi/${serviceName}`);
  url.searchParams.append('optionsOverride.RelativePath', relativePath);
  if (method !== 'GET') {
    url.searchParams.append('optionsOverride.HttpMethod', method);
  }
  
  const requestOptions: any = {
    method: method,
    headers: {
      'Authorization': incomingToken
    }
  };
  
  if (body) {
    requestOptions.headers['Content-Type'] = 'application/json';
    requestOptions.body = JSON.stringify(body);
  }
  
  const response = await fetch(url.toString(), requestOptions);
  
  if (!response.ok) {
    throw new Error(`Sidecar error: ${response.statusText}`);
  }
  
  const data = await response.json() as DownstreamApiResponse;
  
  if (data.statusCode >= 400) {
    throw new Error(`API error ${data.statusCode}: ${data.content}`);
  }
  
  return JSON.parse(data.content);
}

// Usage examples
async function getUserProfile(incomingToken: string) {
  return await callDownstreamApi(incomingToken, 'Graph', 'me');
}

async function listEmails(incomingToken: string) {
  return await callDownstreamApi(
    incomingToken,
    'Graph',
    'me/messages?$top=10&$select=subject,from,receivedDateTime'
  );
}

async function sendEmail(incomingToken: string, message: any) {
  return await callDownstreamApi(
    incomingToken,
    'Graph',
    'me/sendMail',
    'POST',
    { message }
  );
}

// Express.js API example
import express from 'express';

const app = express();
app.use(express.json());

app.get('/api/profile', async (req, res) => {
  try {
    const incomingToken = req.headers.authorization;
    if (!incomingToken) {
      return res.status(401).json({ error: 'No authorization token' });
    }
    
    const profile = await getUserProfile(incomingToken);
    res.json(profile);
  } catch (error) {
    console.error('Error:', error);
    res.status(500).json({ error: 'Failed to fetch profile' });
  }
});

app.get('/api/messages', async (req, res) => {
  try {
    const incomingToken = req.headers.authorization;
    if (!incomingToken) {
      return res.status(401).json({ error: 'No authorization token' });
    }
    
    const messages = await listEmails(incomingToken);
    res.json(messages);
  } catch (error) {
    console.error('Error:', error);
    res.status(500).json({ error: 'Failed to fetch messages' });
  }
});

app.post('/api/messages/send', async (req, res) => {
  try {
    const incomingToken = req.headers.authorization;
    if (!incomingToken) {
      return res.status(401).json({ error: 'No authorization token' });
    }
    
    const message = req.body;
    await sendEmail(incomingToken, message);
    res.json({ success: true });
  } catch (error) {
    console.error('Error:', error);
    res.status(500).json({ error: 'Failed to send message' });
  }
});

app.listen(8080, () => {
  console.log('Server running on port 8080');
});
```

### Python

```python
import os
import json
import requests
from typing import Dict, Any, Optional

def call_downstream_api(
    incoming_token: str,
    service_name: str,
    relative_path: str,
    method: str = 'GET',
    body: Optional[Dict[str, Any]] = None
) -> Any:
    """Call a downstream API via the sidecar."""
    sidecar_url = os.getenv('SIDECAR_URL', 'http://localhost:5000')
    
    params = {
        'optionsOverride.RelativePath': relative_path
    }
    
    if method != 'GET':
        params['optionsOverride.HttpMethod'] = method
    
    headers = {'Authorization': incoming_token}
    json_body = None
    
    if body:
        headers['Content-Type'] = 'application/json'
        json_body = body
    
    response = requests.request(
        method,
        f"{sidecar_url}/DownstreamApi/{service_name}",
        params=params,
        headers=headers,
        json=json_body
    )
    
    if not response.ok:
        raise Exception(f"Sidecar error: {response.text}")
    
    data = response.json()
    
    if data['statusCode'] >= 400:
        raise Exception(f"API error {data['statusCode']}: {data['content']}")
    
    return json.loads(data['content'])

# Usage examples
def get_user_profile(incoming_token: str) -> Dict[str, Any]:
    return call_downstream_api(incoming_token, 'Graph', 'me')

def list_emails(incoming_token: str) -> Dict[str, Any]:
    return call_downstream_api(
        incoming_token,
        'Graph',
        'me/messages?$top=10&$select=subject,from,receivedDateTime'
    )

def send_email(incoming_token: str, message: Dict[str, Any]) -> None:
    call_downstream_api(
        incoming_token,
        'Graph',
        'me/sendMail',
        'POST',
        {'message': message}
    )

# Flask API example
from flask import Flask, request, jsonify

app = Flask(__name__)

@app.route('/api/profile')
def profile():
    incoming_token = request.headers.get('Authorization')
    if not incoming_token:
        return jsonify({'error': 'No authorization token'}), 401
    
    try:
        profile_data = get_user_profile(incoming_token)
        return jsonify(profile_data)
    except Exception as e:
        print(f"Error: {e}")
        return jsonify({'error': 'Failed to fetch profile'}), 500

@app.route('/api/messages')
def messages():
    incoming_token = request.headers.get('Authorization')
    if not incoming_token:
        return jsonify({'error': 'No authorization token'}), 401
    
    try:
        messages_data = list_emails(incoming_token)
        return jsonify(messages_data)
    except Exception as e:
        print(f"Error: {e}")
        return jsonify({'error': 'Failed to fetch messages'}), 500

@app.route('/api/messages/send', methods=['POST'])
def send_message():
    incoming_token = request.headers.get('Authorization')
    if not incoming_token:
        return jsonify({'error': 'No authorization token'}), 401
    
    try:
        message = request.json
        send_email(incoming_token, message)
        return jsonify({'success': True})
    except Exception as e:
        print(f"Error: {e}")
        return jsonify({'error': 'Failed to send message'}), 500

if __name__ == '__main__':
    app.run(port=8080)
```

### Go

```go
package main

import (
    "bytes"
    "encoding/json"
    "fmt"
    "io"
    "net/http"
    "net/url"
    "os"
)

type DownstreamApiResponse struct {
    StatusCode int               `json:"statusCode"`
    Headers    map[string]string `json:"headers"`
    Content    string            `json:"content"`
}

func callDownstreamApi(
    incomingToken string,
    serviceName string,
    relativePath string,
    method string,
    body interface{},
) (interface{}, error) {
    sidecarURL := os.Getenv("SIDECAR_URL")
    if sidecarURL == "" {
        sidecarURL = "http://localhost:5000"
    }
    
    // Build URL with query parameters
    u, err := url.Parse(fmt.Sprintf("%s/DownstreamApi/%s", sidecarURL, serviceName))
    if err != nil {
        return nil, err
    }
    
    q := u.Query()
    q.Add("optionsOverride.RelativePath", relativePath)
    if method != "GET" {
        q.Add("optionsOverride.HttpMethod", method)
    }
    u.RawQuery = q.Encode()
    
    // Prepare request body
    var reqBody io.Reader
    if body != nil {
        jsonBody, err := json.Marshal(body)
        if err != nil {
            return nil, err
        }
        reqBody = bytes.NewBuffer(jsonBody)
    }
    
    // Create request
    req, err := http.NewRequest(method, u.String(), reqBody)
    if err != nil {
        return nil, err
    }
    
    req.Header.Set("Authorization", incomingToken)
    if body != nil {
        req.Header.Set("Content-Type", "application/json")
    }
    
    // Execute request
    client := &http.Client{}
    resp, err := client.Do(req)
    if err != nil {
        return nil, err
    }
    defer resp.Body.Close()
    
    if resp.StatusCode != http.StatusOK {
        respBody, _ := io.ReadAll(resp.Body)
        return nil, fmt.Errorf("sidecar error: %s", string(respBody))
    }
    
    // Parse response
    var apiResp DownstreamApiResponse
    if err := json.NewDecoder(resp.Body).Decode(&apiResp); err != nil {
        return nil, err
    }
    
    if apiResp.StatusCode >= 400 {
        return nil, fmt.Errorf("API error %d: %s", apiResp.StatusCode, apiResp.Content)
    }
    
    // Parse content
    var result interface{}
    if err := json.Unmarshal([]byte(apiResp.Content), &result); err != nil {
        return nil, err
    }
    
    return result, nil
}

// Usage examples
func getUserProfile(incomingToken string) (interface{}, error) {
    return callDownstreamApi(incomingToken, "Graph", "me", "GET", nil)
}

func listEmails(incomingToken string) (interface{}, error) {
    return callDownstreamApi(
        incomingToken,
        "Graph",
        "me/messages?$top=10&$select=subject,from,receivedDateTime",
        "GET",
        nil,
    )
}

func sendEmail(incomingToken string, message map[string]interface{}) error {
    _, err := callDownstreamApi(
        incomingToken,
        "Graph",
        "me/sendMail",
        "POST",
        map[string]interface{}{"message": message},
    )
    return err
}

// HTTP handlers
func profileHandler(w http.ResponseWriter, r *http.Request) {
    incomingToken := r.Header.Get("Authorization")
    if incomingToken == "" {
        http.Error(w, "No authorization token", http.StatusUnauthorized)
        return
    }
    
    profile, err := getUserProfile(incomingToken)
    if err != nil {
        http.Error(w, fmt.Sprintf("Failed to fetch profile: %v", err), http.StatusInternalServerError)
        return
    }
    
    w.Header().Set("Content-Type", "application/json")
    json.NewEncoder(w).Encode(profile)
}

func messagesHandler(w http.ResponseWriter, r *http.Request) {
    incomingToken := r.Header.Get("Authorization")
    if incomingToken == "" {
        http.Error(w, "No authorization token", http.StatusUnauthorized)
        return
    }
    
    messages, err := listEmails(incomingToken)
    if err != nil {
        http.Error(w, fmt.Sprintf("Failed to fetch messages: %v", err), http.StatusInternalServerError)
        return
    }
    
    w.Header().Set("Content-Type", "application/json")
    json.NewEncoder(w).Encode(messages)
}

func main() {
    http.HandleFunc("/api/profile", profileHandler)
    http.HandleFunc("/api/messages", messagesHandler)
    
    fmt.Println("Server starting on :8080")
    http.ListenAndServe(":8080", nil)
}
```

## POST/PUT/PATCH Requests

### Creating Resources

```typescript
// POST example - Create a calendar event
async function createEvent(incomingToken: string, event: any) {
  return await callDownstreamApi(
    incomingToken,
    'Graph',
    'me/events',
    'POST',
    event
  );
}

// Usage
const newEvent = {
  subject: "Team Meeting",
  start: {
    dateTime: "2024-01-15T14:00:00",
    timeZone: "Pacific Standard Time"
  },
  end: {
    dateTime: "2024-01-15T15:00:00",
    timeZone: "Pacific Standard Time"
  }
};

const createdEvent = await createEvent(incomingToken, newEvent);
```

### Updating Resources

```typescript
// PATCH example - Update user profile
async function updateProfile(incomingToken: string, updates: any) {
  return await callDownstreamApi(
    incomingToken,
    'Graph',
    'me',
    'PATCH',
    updates
  );
}

// Usage
await updateProfile(incomingToken, {
  mobilePhone: "+1 555 0100",
  officeLocation: "Building 2, Room 201"
});
```

## Advanced Scenarios

### Custom Headers

Add custom headers to the downstream API request:

```typescript
const url = new URL(`${sidecarUrl}/DownstreamApi/MyApi`);
url.searchParams.append('optionsOverride.RelativePath', 'items');
url.searchParams.append('optionsOverride.CustomHeader.X-Custom-Header', 'custom-value');
url.searchParams.append('optionsOverride.CustomHeader.X-Request-Id', requestId);
```

### Override Scopes

```typescript
const url = new URL(`${sidecarUrl}/DownstreamApi/Graph`);
url.searchParams.append('optionsOverride.RelativePath', 'me');
url.searchParams.append('optionsOverride.Scopes', 'User.ReadWrite');
url.searchParams.append('optionsOverride.Scopes', 'Mail.Send');
```

### With Agent Identity

```typescript
const url = new URL(`${sidecarUrl}/DownstreamApi/Graph`);
url.searchParams.append('optionsOverride.RelativePath', 'users');
url.searchParams.append('AgentIdentity', agentClientId);
url.searchParams.append('AgentUsername', 'admin@contoso.com');
```

## Error Handling

```typescript
async function callDownstreamApiWithRetry(
  incomingToken: string,
  serviceName: string,
  relativePath: string,
  method: string = 'GET',
  body?: any,
  maxRetries: number = 3
): Promise<any> {
  let lastError: Error;
  
  for (let attempt = 1; attempt <= maxRetries; attempt++) {
    try {
      return await callDownstreamApi(
        incomingToken,
        serviceName,
        relativePath,
        method,
        body
      );
    } catch (error) {
      lastError = error as Error;
      
      // Don't retry on client errors (4xx)
      if (error.message.includes('API error 4')) {
        throw error;
      }
      
      // Retry on server errors (5xx) or network errors
      if (attempt < maxRetries) {
        const delay = Math.pow(2, attempt) * 100;
        await new Promise(resolve => setTimeout(resolve, delay));
      }
    }
  }
  
  throw new Error(`Failed after ${maxRetries} retries: ${lastError!.message}`);
}
```

## Comparison with AuthorizationHeader Approach

| Aspect | /DownstreamApi | /AuthorizationHeader |
|--------|----------------|----------------------|
| Token Acquisition | ✅ Handled | ✅ Handled |
| HTTP Request | ✅ Handled | ❌ Your responsibility |
| Response Parsing | ⚠️ String content | ✅ Direct access |
| Custom Headers | ⚠️ Via query params | ✅ Full control |
| Request Body | ✅ Forwarded | ✅ Full control |
| Error Handling | ⚠️ Wrapped | ✅ Direct |

**Use /DownstreamApi when:**
- Simple API calls
- Standard HTTP methods
- Minimizing application code

**Use /AuthorizationHeader when:**
- Complex HTTP clients
- Custom request handling
- Direct error handling
- Fine-grained control

## Best Practices

1. **Reuse HTTP Clients**: Create once and reuse
2. **Handle Errors**: Implement retry logic for transient failures
3. **Parse Responses**: Check statusCode before parsing content
4. **Set Timeouts**: Configure appropriate timeouts
5. **Log Requests**: Include correlation IDs for tracing
6. **Validate Input**: Sanitize data before sending to API
7. **Monitor Performance**: Track API call latency

## Next Steps

- [Obtain Authorization Header](obtain-authorization-header.md) - Get just the token
- [Using from TypeScript](using-from-typescript.md) - TypeScript-specific patterns
- [Using from Python](using-from-python.md) - Python-specific patterns
- [Endpoints Reference](../endpoints.md) - Complete API documentation
