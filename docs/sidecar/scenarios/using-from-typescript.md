# Scenario: Using the Sidecar from TypeScript

Reference implementation inspired by PR #3520.

## Fetch Authorization Header

```ts
async function getAuthHeader(sidecarBase: string, apiName: string, inboundUserToken: string) {
  const resp = await fetch(`${sidecarBase}/AuthorizationHeader/${encodeURIComponent(apiName)}`, {
    headers: { Authorization: `Bearer ${inboundUserToken}` }
  });
  if (!resp.ok) {
    throw new Error(`Sidecar error ${resp.status}: ${await resp.text()}`);
  }
  const json = await resp.json() as { authorizationHeader: string };
  return json.authorizationHeader;
}
```

## Agent + User Delegation

```ts
async function getAgentUserHeader(sidecarBase: string, apiName: string, agent: string, userOid: string, inboundToken: string) {
  const url = new URL(`${sidecarBase}/AuthorizationHeader/${apiName}`);
  url.searchParams.set("AgentIdentity", agent);
  url.searchParams.set("AgentUserId", userOid);
  const r = await fetch(url.toString(), { headers: { Authorization: `Bearer ${inboundToken}` } });
  if (!r.ok) throw new Error(await r.text());
  return (await r.json()).authorizationHeader as string;
}
```

## Proxy Call Example

```ts
async function proxyGraphMessages(sidecarBase: string, inboundToken: string) {
  const url = new URL(`${sidecarBase}/DownstreamApi/Graph`);
  url.searchParams.set("optionsOverride.HttpMethod", "GET");
  url.searchParams.set("optionsOverride.RelativePath", "/v1.0/me/messages");
  const r = await fetch(url.toString(), {
    method: "POST",
    headers: { Authorization: `Bearer ${inboundToken}` }
  });
  const json = await r.json();
  if (json.statusCode !== 200) {
    throw new Error(`Downstream status ${json.statusCode}: ${json.content}`);
  }
  return JSON.parse(json.content);
}
```

## Tips

| Concern | Approach |
|---------|----------|
| Correlation | Append `optionsOverride.AcquireTokenOptions.CorrelationId` |
| Extra scopes | Repeat `optionsOverride.Scopes` |
| Error handling | Distinguish HTTP error vs downstream `statusCode` |
