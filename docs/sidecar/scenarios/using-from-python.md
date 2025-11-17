# Scenario: Using the Sidecar from Python

Complete guide for Python applications.

## Setup

```bash
pip install requests
```

## Sidecar Client Library

```python
# sidecar_client.py
import os
import json
import requests
from typing import Dict, Any, Optional, List
from urllib.parse import urlencode

class SidecarClient:
    """Client for interacting with the Microsoft Entra Identity Sidecar."""
    
    def __init__(self, base_url: Optional[str] = None, timeout: int = 10):
        self.base_url = base_url or os.getenv('SIDECAR_URL', 'http://localhost:5000')
        self.timeout = timeout
    
    def get_authorization_header(
        self,
        incoming_token: str,
        service_name: str,
        scopes: Optional[List[str]] = None,
        tenant: Optional[str] = None,
        agent_identity: Optional[str] = None,
        agent_username: Optional[str] = None
    ) -> str:
        """Get authorization header from sidecar."""
        params = {}
        
        if scopes:
            params['optionsOverride.Scopes'] = scopes
        
        if tenant:
            params['optionsOverride.AcquireTokenOptions.Tenant'] = tenant
        
        if agent_identity:
            params['AgentIdentity'] = agent_identity
            if agent_username:
                params['AgentUsername'] = agent_username
        
        response = requests.get(
            f"{self.base_url}/AuthorizationHeader/{service_name}",
            params=params,
            headers={'Authorization': incoming_token},
            timeout=self.timeout
        )
        
        response.raise_for_status()
        data = response.json()
        return data['authorizationHeader']
    
    def call_downstream_api(
        self,
        incoming_token: str,
        service_name: str,
        relative_path: str,
        method: str = 'GET',
        body: Optional[Dict[str, Any]] = None,
        scopes: Optional[List[str]] = None
    ) -> Any:
        """Call downstream API via sidecar."""
        params = {'optionsOverride.RelativePath': relative_path}
        
        if method != 'GET':
            params['optionsOverride.HttpMethod'] = method
        
        if scopes:
            params['optionsOverride.Scopes'] = scopes
        
        headers = {'Authorization': incoming_token}
        json_body = None
        
        if body:
            headers['Content-Type'] = 'application/json'
            json_body = body
        
        response = requests.request(
            method,
            f"{self.base_url}/DownstreamApi/{service_name}",
            params=params,
            headers=headers,
            json=json_body,
            timeout=self.timeout
        )
        
        response.raise_for_status()
        data = response.json()
        
        if data['statusCode'] >= 400:
            raise Exception(f"API error {data['statusCode']}: {data['content']}")
        
        return json.loads(data['content'])

# Usage
sidecar = SidecarClient(base_url='http://localhost:5000')

# Get authorization header
auth_header = sidecar.get_authorization_header(token, 'Graph')

# Call API
profile = sidecar.call_downstream_api(token, 'Graph', 'me')
```

## Flask Integration

```python
from flask import Flask, request, jsonify
from sidecar_client import SidecarClient

app = Flask(__name__)
sidecar = SidecarClient()

def get_token():
    """Extract token from request."""
    token = request.headers.get('Authorization')
    if not token:
        raise ValueError('No authorization token provided')
    return token

@app.route('/api/profile')
def profile():
    try:
        token = get_token()
        profile_data = sidecar.call_downstream_api(
            token,
            'Graph',
            'me'
        )
        return jsonify(profile_data)
    except ValueError as e:
        return jsonify({'error': str(e)}), 401
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@app.route('/api/messages')
def messages():
    try:
        token = get_token()
        messages_data = sidecar.call_downstream_api(
            token,
            'Graph',
            'me/messages?$top=10'
        )
        return jsonify(messages_data)
    except ValueError as e:
        return jsonify({'error': str(e)}), 401
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@app.route('/api/messages/send', methods=['POST'])
def send_message():
    try:
        token = get_token()
        message = request.json
        
        result = sidecar.call_downstream_api(
            token,
            'Graph',
            'me/sendMail',
            method='POST',
            body={'message': message}
        )
        
        return jsonify({'success': True, 'result': result})
    except ValueError as e:
        return jsonify({'error': str(e)}), 401
    except Exception as e:
        return jsonify({'error': str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=8080)
```

## FastAPI Integration

```python
from fastapi import FastAPI, Header, HTTPException
from sidecar_client import SidecarClient
from typing import Optional

app = FastAPI()
sidecar = SidecarClient()

async def get_token(authorization: Optional[str] = Header(None)):
    if not authorization:
        raise HTTPException(status_code=401, detail="No authorization token")
    return authorization

@app.get("/api/profile")
async def get_profile(token: str = Depends(get_token)):
    try:
        return sidecar.call_downstream_api(token, 'Graph', 'me')
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/api/messages")
async def get_messages(token: str = Depends(get_token)):
    try:
        return sidecar.call_downstream_api(
            token,
            'Graph',
            'me/messages?$top=10'
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
```

## Django Integration

```python
# views.py
from django.http import JsonResponse
from django.views import View
from sidecar_client import SidecarClient

sidecar = SidecarClient()

class ProfileView(View):
    def get(self, request):
        token = request.META.get('HTTP_AUTHORIZATION')
        if not token:
            return JsonResponse({'error': 'No authorization token'}, status=401)
        
        try:
            profile = sidecar.call_downstream_api(token, 'Graph', 'me')
            return JsonResponse(profile)
        except Exception as e:
            return JsonResponse({'error': str(e)}, status=500)

class MessagesView(View):
    def get(self, request):
        token = request.META.get('HTTP_AUTHORIZATION')
        if not token:
            return JsonResponse({'error': 'No authorization token'}, status=401)
        
        try:
            messages = sidecar.call_downstream_api(
                token,
                'Graph',
                'me/messages?$top=10'
            )
            return JsonResponse(messages)
        except Exception as e:
            return JsonResponse({'error': str(e)}, status=500)
```

## Best Practices

1. **Reuse Client Instance**: Create SidecarClient once and reuse
2. **Set Appropriate Timeouts**: Configure based on API latency
3. **Handle Errors**: Implement proper error handling and retry logic
4. **Use Type Hints**: Add type hints for better code clarity
5. **Connection Pooling**: Leverage requests Session for connection reuse

## Advanced: Using requests.Session

```python
import requests
from requests.adapters import HTTPAdapter
from requests.packages.urllib3.util.retry import Retry

class SidecarClient:
    def __init__(self, base_url: Optional[str] = None):
        self.base_url = base_url or os.getenv('SIDECAR_URL', 'http://localhost:5000')
        
        # Configure session with retry logic
        self.session = requests.Session()
        retry = Retry(
            total=3,
            backoff_factor=0.3,
            status_forcelist=[500, 502, 503, 504]
        )
        adapter = HTTPAdapter(max_retries=retry)
        self.session.mount('http://', adapter)
        self.session.mount('https://', adapter)
    
    def call_downstream_api(self, token, service_name, relative_path, **kwargs):
        # Use self.session instead of requests
        response = self.session.get(...)
        return response
```

## Next Steps

- [Call Downstream API](call-downstream-api.md) - Detailed API calling examples
- [Obtain Authorization Header](obtain-authorization-header.md) - Get tokens directly
- [Agent Identities](../agent-identities.md) - Use agent identities
