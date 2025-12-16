# Troubleshooting Guide

This guide helps diagnose and resolve common issues with the Microsoft Entra Identity Sidecar.

## Quick Diagnostics

### Check Sidecar Health

```bash
# Check if sidecar is running
kubectl get pods -l app=myapp

# Check sidecar logs
kubectl logs <pod-name> -c sidecar

# Test health endpoint
kubectl exec <pod-name> -c sidecar -- curl http://localhost:5000/health
```

### Check Configuration

```bash
# View sidecar environment variables
kubectl exec <pod-name> -c sidecar -- env | grep AzureAd

# Check ConfigMap
kubectl get configmap sidecar-config -o yaml

# Check Secrets
kubectl get secret sidecar-secrets -o yaml
```

## Common Issues

### 1. Container Won't Start

#### Symptom
Pod shows `CrashLoopBackOff` or `Error` status.

#### Possible Causes

**Missing Required Configuration**

```bash
# Check logs for configuration errors
kubectl logs <pod-name> -c sidecar

# Look for messages like:
# "AzureAd:TenantId is required"
# "AzureAd:ClientId is required"
```

**Solution**:
```yaml
# Ensure all required configuration is set
env:
- name: AzureAd__TenantId
  value: "<your-tenant-id>"
- name: AzureAd__ClientId
  value: "<your-client-id>"
```

**Invalid Credential Configuration**

```bash
# Check for credential errors in logs
kubectl logs <pod-name> -c sidecar | grep -i "credential"
```

**Solution**: Verify credential configuration and access to Key Vault or secrets.

**Port Conflict**

```bash
# Check if port 5000 is already in use
kubectl exec <pod-name> -c sidecar -- netstat -tuln | grep 5000
```

**Solution**: Change sidecar port if needed:
```yaml
env:
- name: ASPNETCORE_URLS
  value: "http://+:5001"
```

### 2. 401 Unauthorized Errors

#### Symptom
Requests to sidecar return 401 Unauthorized.

#### Possible Causes

**Missing Authorization Header**

```bash
# Test with curl
curl -v http://localhost:5000/AuthorizationHeader/Graph
# Should show 401 because no Authorization header
```

**Solution**: Include Authorization header:
```bash
curl -H "Authorization: Bearer <token>" \
  http://localhost:5000/AuthorizationHeader/Graph
```

**Invalid or Expired Token**

```bash
# Check token claims
kubectl exec <pod-name> -c sidecar -- curl -H "Authorization: Bearer <token>" \
  http://localhost:5000/Validate
```

**Solution**: Obtain a new token from Microsoft Entra ID.

**Audience Mismatch**

```bash
# Check logs for audience validation errors
kubectl logs <pod-name> -c sidecar | grep -i "audience"
```

**Solution**: Verify audience configuration matches token:
```yaml
env:
- name: AzureAd__Audience
  value: "api://<your-api-id>"
```

**Scope Validation Failure**

```bash
# Check logs for scope errors
kubectl logs <pod-name> -c sidecar | grep -i "scope"
```

**Solution**: Ensure token contains required scopes:
```yaml
env:
- name: AzureAd__Scopes
  value: "access_as_user"  # Or remove to disable scope validation
```

### 3. 400 Bad Request - Agent Identity Validation

#### Symptom
Requests with agent identity parameters return 400 Bad Request.

#### Error: AgentUsername Without AgentIdentity

**Request**:
```bash
curl -H "Authorization: Bearer <token>" \
  "http://localhost:5000/AuthorizationHeader/Graph?AgentUsername=user@contoso.com"
```

**Error Response**:
```json
{
  "status": 400,
  "detail": "AgentUsername requires AgentIdentity to be specified"
}
```

**Solution**: Include AgentIdentity parameter:
```bash
curl -H "Authorization: Bearer <token>" \
  "http://localhost:5000/AuthorizationHeader/Graph?AgentIdentity=<client-id>&AgentUsername=user@contoso.com"
```

#### Error: AgentUsername and AgentUserId Both Specified

**Request**:
```bash
curl -H "Authorization: Bearer <token>" \
  "http://localhost:5000/AuthorizationHeader/Graph?AgentIdentity=<id>&AgentUsername=user@contoso.com&AgentUserId=<oid>"
```

**Error Response**:
```json
{
  "status": 400,
  "detail": "AgentUsername and AgentUserId are mutually exclusive"
}
```

**Solution**: Use only one:
```bash
# Use AgentUsername
curl -H "Authorization: Bearer <token>" \
  "http://localhost:5000/AuthorizationHeader/Graph?AgentIdentity=<id>&AgentUsername=user@contoso.com"

# OR use AgentUserId
curl -H "Authorization: Bearer <token>" \
  "http://localhost:5000/AuthorizationHeader/Graph?AgentIdentity=<id>&AgentUserId=<oid>"
```

#### Error: Invalid AgentUserId Format

**Request**:
```bash
curl -H "Authorization: Bearer <token>" \
  "http://localhost:5000/AuthorizationHeader/Graph?AgentIdentity=<id>&AgentUserId=invalid-guid"
```

**Error Response**:
```json
{
  "status": 400,
  "detail": "AgentUserId must be a valid GUID"
}
```

**Solution**: Provide a valid GUID:
```bash
curl -H "Authorization: Bearer <token>" \
  "http://localhost:5000/AuthorizationHeader/Graph?AgentIdentity=<id>&AgentUserId=12345678-1234-1234-1234-123456789012"
```

### 4. 404 Not Found - Service Not Configured

#### Symptom
```json
{
  "status": 404,
  "detail": "Downstream API 'UnknownService' not configured"
}
```

#### Possible Causes

**Service Name Typo**

```bash
# Wrong service name in URL
curl -H "Authorization: Bearer <token>" \
  http://localhost:5000/AuthorizationHeader/Grafh
# Should be "Graph"
```

**Solution**: Use correct service name from configuration.

**Missing DownstreamApis Configuration**

**Solution**: Add service configuration:
```yaml
env:
- name: DownstreamApis__Graph__BaseUrl
  value: "https://graph.microsoft.com/v1.0"
- name: DownstreamApis__Graph__Scopes
  value: "User.Read"
```

### 5. Token Acquisition Failures

#### Symptom
500 Internal Server Error when acquiring tokens.

#### AADSTS Error Codes

**AADSTS50076: Multi-Factor Authentication Required**

```
AADSTS50076: Due to a configuration change made by your administrator, 
or because you moved to a new location, you must use multi-factor authentication.
```

**Solution**: User must complete MFA. This is expected behavior for conditional access policies.

**AADSTS65001: User Consent Required**

```
AADSTS65001: The user or administrator has not consented to use the application.
```

**Solution**: 
1. Request admin consent for the application
2. Ensure delegated permissions are properly configured

**AADSTS700016: Application Not Found**

```
AADSTS700016: Application with identifier '<client-id>' was not found.
```

**Solution**: Verify ClientId is correct and application exists in tenant.

**AADSTS7000215: Invalid Client Secret**

```
AADSTS7000215: Invalid client secret is provided.
```

**Solution**: 
1. Verify client secret is correct
2. Check if secret has expired
3. Generate new secret and update configuration

**AADSTS700027: Certificate or Private Key Not Configured**

```
AADSTS700027: The certificate with identifier '<thumbprint>' was not found.
```

**Solution**:
1. Verify certificate is registered in app registration
2. Check certificate configuration in sidecar
3. Ensure certificate is accessible from container

#### Token Cache Issues

**Solution**: Clear token cache and restart:
```bash
kubectl rollout restart deployment <deployment-name>
```

For distributed cache (Redis):
```bash
# Clear Redis cache
redis-cli FLUSHDB
```

### 6. Network Connectivity Issues

#### Cannot Reach Microsoft Entra ID

**Symptom**: Timeout errors when acquiring tokens.

**Diagnostics**:
```bash
# Test connectivity from sidecar container
kubectl exec <pod-name> -c sidecar -- curl -v https://login.microsoftonline.com

# Check DNS resolution
kubectl exec <pod-name> -c sidecar -- nslookup login.microsoftonline.com
```

**Solution**:
- Check network policies
- Verify firewall rules allow HTTPS to login.microsoftonline.com
- Ensure DNS is working correctly

#### Cannot Reach Downstream APIs

**Diagnostics**:
```bash
# Test connectivity to downstream API
kubectl exec <pod-name> -c sidecar -- curl -v https://graph.microsoft.com

# Check configuration
kubectl exec <pod-name> -c sidecar -- env | grep DownstreamApis__Graph__BaseUrl
```

**Solution**:
- Verify downstream API URL is correct
- Check network egress rules
- Ensure API is accessible from cluster

### 7. Application Cannot Reach Sidecar

#### Symptom
Application shows connection errors when calling sidecar.

**Diagnostics**:
```bash
# Test from application container
kubectl exec <pod-name> -c app -- curl -v http://localhost:5000/health

# Check if sidecar is listening
kubectl exec <pod-name> -c sidecar -- netstat -tuln | grep 5000
```

**Solution**:
- Verify SIDECAR_URL environment variable
- Check sidecar is running: `kubectl get pods`
- Ensure port 5000 is not blocked

### 8. Performance Issues

#### Slow Token Acquisition

**Diagnostics**:
```bash
# Enable detailed logging
# Add to sidecar configuration:
# - name: Logging__LogLevel__Microsoft.Identity.Web
#   value: "Debug"

# Check logs for timing information
kubectl logs <pod-name> -c sidecar | grep "Token acquisition"
```

**Solutions**:
1. **Check Token Cache**: Ensure caching is enabled and working
2. **Increase Resources**: Allocate more CPU/memory to sidecar
3. **Network Latency**: Check latency to Microsoft Entra ID
4. **Connection Pooling**: Verify HTTP connection reuse

#### High Memory Usage

**Diagnostics**:
```bash
# Check resource usage
kubectl top pod <pod-name> --containers

# Check for memory leaks in logs
kubectl logs <pod-name> -c sidecar | grep -i "memory"
```

**Solutions**:
1. Increase memory limits
2. Check for token cache size issues
3. Review application usage patterns
4. Consider distributed cache for multiple replicas

### 9. Certificate Issues

#### Certificate Not Found

**Symptom**: 
```
Certificate with thumbprint '<thumbprint>' not found in certificate store.
```

**Solution**:
- Verify certificate is mounted correctly
- Check certificate store path
- Ensure certificate permissions are correct

#### Certificate Expired

**Symptom**:
```
The certificate has expired.
```

**Solution**:
1. Generate new certificate
2. Register in Microsoft Entra ID
3. Update sidecar configuration
4. Redeploy containers

#### Key Vault Access Denied

**Symptom**:
```
Access denied to Key Vault '<vault-name>'.
```

**Solution**:
- Verify managed identity has access policy to Key Vault
- Check Key Vault firewall rules
- Ensure certificate exists in Key Vault

### 10. Signed HTTP Request (SHR) Issues

#### Invalid PoP Token

**Symptom**: Downstream API rejects PoP token.

**Diagnostics**:
```bash
# Check if PoP token is being requested
kubectl logs <pod-name> -c sidecar | grep -i "pop"

# Verify PopPublicKey is configured correctly
kubectl exec <pod-name> -c sidecar -- env | grep PopPublicKey
```

**Solution**:
- Verify public key is correctly base64 encoded
- Ensure downstream API supports PoP tokens
- Check PoP token format

#### Missing Private Key

**Symptom**: Cannot sign HTTP request.

**Solution**: Ensure private key is available to application for signing requests.

## Error Reference Table

| Error Code | Message | Cause | Solution |
|------------|---------|-------|----------|
| 400 | AgentUsername requires AgentIdentity | AgentUsername without AgentIdentity | Add AgentIdentity parameter |
| 400 | AgentUsername and AgentUserId are mutually exclusive | Both parameters specified | Use only one parameter |
| 400 | AgentUserId must be a valid GUID | Invalid GUID format | Provide valid GUID |
| 400 | Service name is required | Missing service name in path | Include service name in URL |
| 400 | No token found | Missing Authorization header | Include valid token |
| 401 | Unauthorized | Invalid or expired token | Obtain new token |
| 403 | Forbidden | Missing required scopes | Request token with correct scopes |
| 404 | Downstream API not configured | Service not in configuration | Add DownstreamApis configuration |
| 500 | Failed to acquire token | Various MSAL errors | Check logs for specific AADSTS error |
| 503 | Service Unavailable | Health check failure | Check sidecar status and configuration |

## Debugging Tools

### Enable Detailed Logging

```yaml
env:
- name: Logging__LogLevel__Default
  value: "Debug"
- name: Logging__LogLevel__Microsoft.Identity.Web
  value: "Trace"
- name: Logging__LogLevel__Microsoft.AspNetCore
  value: "Debug"
```

**Warning**: Debug/Trace logging may log sensitive information. Use only in development or temporarily in production.

### Test Token Validation

```bash
# Validate token
curl -H "Authorization: Bearer <token>" \
  http://localhost:5000/Validate | jq .

# Check claims
curl -H "Authorization: Bearer <token>" \
  http://localhost:5000/Validate | jq '.claims'
```

### Test Token Acquisition

```bash
# Get authorization header
curl -H "Authorization: Bearer <token>" \
  http://localhost:5000/AuthorizationHeader/Graph | jq .

# Extract and decode token
curl -H "Authorization: Bearer <token>" \
  http://localhost:5000/AuthorizationHeader/Graph | \
  jq -r '.authorizationHeader' | \
  cut -d' ' -f2 | \
  jwt decode -
```

### Monitor with Application Insights

If configured:

```bash
# Query Application Insights
az monitor app-insights query \
  --app <app-insights-name> \
  --analytics-query "traces | where message contains 'token acquisition' | take 100"
```

## Getting Help

### Collect Diagnostic Information

When opening an issue, include:

1. **Sidecar Version**:
   ```bash
   kubectl describe pod <pod-name> | grep -A 5 "sidecar:"
   ```

2. **Configuration** (redact sensitive data):
   ```bash
   kubectl get configmap sidecar-config -o yaml
   ```

3. **Logs** (last 100 lines):
   ```bash
   kubectl logs <pod-name> -c sidecar --tail=100
   ```

4. **Error Messages**: Full error response from sidecar

5. **Request Details**: HTTP method, endpoint, parameters used

### Support Resources

- **GitHub Issues**: [microsoft-identity-web/issues](https://github.com/AzureAD/microsoft-identity-web/issues)
- **Microsoft Q&A**: [Microsoft Identity Platform](https://docs.microsoft.com/answers/topics/azure-active-directory.html)
- **Stack Overflow**: Tag `[microsoft-identity-web]`

## Best Practices for Troubleshooting

1. **Start with Health Check**: Always verify sidecar is healthy first
2. **Check Logs**: Sidecar logs contain valuable diagnostic information
3. **Verify Configuration**: Ensure all required settings are present and correct
4. **Test Incrementally**: Start with simple requests, add complexity gradually
5. **Use Correlation IDs**: Include correlation IDs for tracing across services
6. **Monitor Continuously**: Set up alerts for authentication failures
7. **Document Issues**: Keep notes on issues and resolutions for future reference

## Next Steps

- [Configuration Reference](configuration.md) - Review configuration options
- [Agent Identities](agent-identities.md) - Understand agent identity rules
- [Security Best Practices](security.md) - Ensure secure configuration
- [FAQ](faq.md) - Common questions and answers
