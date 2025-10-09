# Scenario: Using the Sidecar from Python

Inspired by Python integration commit.

## Acquire Authorization Header

```python
import requests

def get_auth_header(sidecar_base: str, api_name: str, inbound_user_token: str) -> str:
    url = f"{sidecar_base}/AuthorizationHeader/{api_name}"
    resp = requests.get(url, headers={"Authorization": f"Bearer {inbound_user_token}"}, timeout=10)
    resp.raise_for_status()
    return resp.json()["authorizationHeader"]
```

## Agent + User Delegation

```python
def get_agent_user_header(sidecar_base: str, api_name: str, agent: str, user_oid: str, inbound_token: str) -> str:
    url = f"{sidecar_base}/AuthorizationHeader/{api_name}?AgentIdentity={agent}&AgentUserId={user_oid}"
    r = requests.get(url, headers={"Authorization": f"Bearer {inbound_token}"}, timeout=10)
    r.raise_for_status()
    return r.json()["authorizationHeader"]
```

## Proxy a Downstream API Call

```python
def proxy_graph_messages(sidecar_base: str, inbound_token: str):
    url = (f"{sidecar_base}/DownstreamApi/Graph"
           "?optionsOverride.HttpMethod=GET"
           "&optionsOverride.RelativePath=/v1.0/me/messages")
    r = requests.post(url, headers={"Authorization": f"Bearer {inbound_token}"}, timeout=15)
    r.raise_for_status()
    data = r.json()
    if data["statusCode"] != 200:
        raise RuntimeError(f"Downstream error {data['statusCode']}: {data['content']}")
    return data
```

## Tips

| Concern | Approach |
|---------|----------|
| Retries | Use backoff for transient 5xx or network errors |
| Correlation | Add query: `optionsOverride.AcquireTokenOptions.CorrelationId=<guid>` |
| JSON body upstream | For POST proxy with outbound body, send JSON in POST while customizing outbound method via override |
