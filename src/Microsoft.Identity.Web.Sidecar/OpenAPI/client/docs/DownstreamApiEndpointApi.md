# openapi_client.DownstreamApiEndpointApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**downstream_api**](DownstreamApiEndpointApi.md#downstream_api) | **POST** /DownstreamApi/{apiName} | Invoke a configured downstream API through the sidecar using the authenticated identity.
[**downstream_api_unauthenticated**](DownstreamApiEndpointApi.md#downstream_api_unauthenticated) | **POST** /DownstreamApiUnauthenticated/{apiName} | Invoke a configured downstream API through the sidecar using the configured client credentials.


# **downstream_api**
> DownstreamApiResult downstream_api(api_name, agent_identity=agent_identity, agent_username=agent_username, options_override_scopes=options_override_scopes, options_override_request_app_token=options_override_request_app_token, options_override_base_url=options_override_base_url, options_override_relative_path=options_override_relative_path, options_override_http_method=options_override_http_method, options_override_accept_header=options_override_accept_header, options_override_content_type=options_override_content_type, options_override_acquire_token_options_tenant=options_override_acquire_token_options_tenant, options_override_acquire_token_options_force_refresh=options_override_acquire_token_options_force_refresh, options_override_acquire_token_options_claims=options_override_acquire_token_options_claims, options_override_acquire_token_options_correlation_id=options_override_acquire_token_options_correlation_id, options_override_acquire_token_options_long_running_web_api_session_key=options_override_acquire_token_options_long_running_web_api_session_key, options_override_acquire_token_options_fmi_path=options_override_acquire_token_options_fmi_path, options_override_acquire_token_options_pop_public_key=options_override_acquire_token_options_pop_public_key, options_override_acquire_token_options_managed_identity_user_assigned_client_id=options_override_acquire_token_options_managed_identity_user_assigned_client_id)

Invoke a configured downstream API through the sidecar using the authenticated identity.

Override downstream call options using dotted query parameters prefixed with 'optionsOverride.'. Examples:
  ?optionsOverride.Scopes=User.Read
  ?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read
  ?optionsOverride.AcquireTokenOptions.Tenant=GUID
  ?optionsOverride.RequestAppToken=true&optionsOverride.Scopes=https://graph.microsoft.com/.default

### Example


```python
import openapi_client
from openapi_client.models.downstream_api_result import DownstreamApiResult
from openapi_client.models.true_bypass_token_cache import TrueBypassTokenCache
from openapi_client.rest import ApiException
from pprint import pprint

# Defining the host is optional and defaults to http://localhost
# See configuration.py for a list of all supported configuration parameters.
configuration = openapi_client.Configuration(
    host = "http://localhost"
)


# Enter a context with an instance of the API client
with openapi_client.ApiClient(configuration) as api_client:
    # Create an instance of the API class
    api_instance = openapi_client.DownstreamApiEndpointApi(api_client)
    api_name = 'api_name_example' # str | 
    agent_identity = 'agent_identity_example' # str |  (optional)
    agent_username = 'agent_username_example' # str |  (optional)
    options_override_scopes = 'options_override_scopes_example' # str | Repeatable. Each occurrence adds one scope. Example: optionsOverride.Scopes=User.Read (optional)
    options_override_request_app_token = True # bool | true = acquire an app (client credentials) token instead of user token. (optional)
    options_override_base_url = 'options_override_base_url_example' # str | Override downstream API base URL. (optional)
    options_override_relative_path = 'options_override_relative_path_example' # str | Override relative path appended to BaseUrl. (optional)
    options_override_http_method = 'options_override_http_method_example' # str | Override HTTP method (GET, POST, PATCH, etc.). (optional)
    options_override_accept_header = 'options_override_accept_header_example' # str | Sets Accept header (e.g. application/json). (optional)
    options_override_content_type = 'options_override_content_type_example' # str | Sets Content-Type used for serialized body (if body provided). (optional)
    options_override_acquire_token_options_tenant = 'options_override_acquire_token_options_tenant_example' # str | Override tenant (GUID or 'common'). (optional)
    options_override_acquire_token_options_force_refresh = openapi_client.TrueBypassTokenCache() # TrueBypassTokenCache | boolean (optional)
    options_override_acquire_token_options_claims = 'options_override_acquire_token_options_claims_example' # str | JSON claims challenge or extra claims. (optional)
    options_override_acquire_token_options_correlation_id = 'options_override_acquire_token_options_correlation_id_example' # str | GUID correlation id for token acquisition. (optional)
    options_override_acquire_token_options_long_running_web_api_session_key = 'options_override_acquire_token_options_long_running_web_api_session_key_example' # str | Session key for long running OBO flows. (optional)
    options_override_acquire_token_options_fmi_path = 'options_override_acquire_token_options_fmi_path_example' # str | Federated Managed Identity path (if using FMI). (optional)
    options_override_acquire_token_options_pop_public_key = 'options_override_acquire_token_options_pop_public_key_example' # str | Public key or JWK for PoP / AT-POP requests. (optional)
    options_override_acquire_token_options_managed_identity_user_assigned_client_id = 'options_override_acquire_token_options_managed_identity_user_assigned_client_id_example' # str | Managed Identity client id (user-assigned). (optional)

    try:
        # Invoke a configured downstream API through the sidecar using the authenticated identity.
        api_response = api_instance.downstream_api(api_name, agent_identity=agent_identity, agent_username=agent_username, options_override_scopes=options_override_scopes, options_override_request_app_token=options_override_request_app_token, options_override_base_url=options_override_base_url, options_override_relative_path=options_override_relative_path, options_override_http_method=options_override_http_method, options_override_accept_header=options_override_accept_header, options_override_content_type=options_override_content_type, options_override_acquire_token_options_tenant=options_override_acquire_token_options_tenant, options_override_acquire_token_options_force_refresh=options_override_acquire_token_options_force_refresh, options_override_acquire_token_options_claims=options_override_acquire_token_options_claims, options_override_acquire_token_options_correlation_id=options_override_acquire_token_options_correlation_id, options_override_acquire_token_options_long_running_web_api_session_key=options_override_acquire_token_options_long_running_web_api_session_key, options_override_acquire_token_options_fmi_path=options_override_acquire_token_options_fmi_path, options_override_acquire_token_options_pop_public_key=options_override_acquire_token_options_pop_public_key, options_override_acquire_token_options_managed_identity_user_assigned_client_id=options_override_acquire_token_options_managed_identity_user_assigned_client_id)
        print("The response of DownstreamApiEndpointApi->downstream_api:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling DownstreamApiEndpointApi->downstream_api: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **api_name** | **str**|  | 
 **agent_identity** | **str**|  | [optional] 
 **agent_username** | **str**|  | [optional] 
 **options_override_scopes** | **str**| Repeatable. Each occurrence adds one scope. Example: optionsOverride.Scopes&#x3D;User.Read | [optional] 
 **options_override_request_app_token** | **bool**| true &#x3D; acquire an app (client credentials) token instead of user token. | [optional] 
 **options_override_base_url** | **str**| Override downstream API base URL. | [optional] 
 **options_override_relative_path** | **str**| Override relative path appended to BaseUrl. | [optional] 
 **options_override_http_method** | **str**| Override HTTP method (GET, POST, PATCH, etc.). | [optional] 
 **options_override_accept_header** | **str**| Sets Accept header (e.g. application/json). | [optional] 
 **options_override_content_type** | **str**| Sets Content-Type used for serialized body (if body provided). | [optional] 
 **options_override_acquire_token_options_tenant** | **str**| Override tenant (GUID or &#39;common&#39;). | [optional] 
 **options_override_acquire_token_options_force_refresh** | [**TrueBypassTokenCache**](.md)| boolean | [optional] 
 **options_override_acquire_token_options_claims** | **str**| JSON claims challenge or extra claims. | [optional] 
 **options_override_acquire_token_options_correlation_id** | **str**| GUID correlation id for token acquisition. | [optional] 
 **options_override_acquire_token_options_long_running_web_api_session_key** | **str**| Session key for long running OBO flows. | [optional] 
 **options_override_acquire_token_options_fmi_path** | **str**| Federated Managed Identity path (if using FMI). | [optional] 
 **options_override_acquire_token_options_pop_public_key** | **str**| Public key or JWK for PoP / AT-POP requests. | [optional] 
 **options_override_acquire_token_options_managed_identity_user_assigned_client_id** | **str**| Managed Identity client id (user-assigned). | [optional] 

### Return type

[**DownstreamApiResult**](DownstreamApiResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, application/problem+json

### HTTP response details

| Status code | Description | Response headers |
|-------------|-------------|------------------|
**200** | OK |  -  |
**400** | Bad Request |  -  |
**401** | Unauthorized |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **downstream_api_unauthenticated**
> DownstreamApiResult downstream_api_unauthenticated(api_name, agent_identity=agent_identity, agent_username=agent_username, options_override_scopes=options_override_scopes, options_override_request_app_token=options_override_request_app_token, options_override_base_url=options_override_base_url, options_override_relative_path=options_override_relative_path, options_override_http_method=options_override_http_method, options_override_accept_header=options_override_accept_header, options_override_content_type=options_override_content_type, options_override_acquire_token_options_tenant=options_override_acquire_token_options_tenant, options_override_acquire_token_options_force_refresh=options_override_acquire_token_options_force_refresh, options_override_acquire_token_options_claims=options_override_acquire_token_options_claims, options_override_acquire_token_options_correlation_id=options_override_acquire_token_options_correlation_id, options_override_acquire_token_options_long_running_web_api_session_key=options_override_acquire_token_options_long_running_web_api_session_key, options_override_acquire_token_options_fmi_path=options_override_acquire_token_options_fmi_path, options_override_acquire_token_options_pop_public_key=options_override_acquire_token_options_pop_public_key, options_override_acquire_token_options_managed_identity_user_assigned_client_id=options_override_acquire_token_options_managed_identity_user_assigned_client_id)

Invoke a configured downstream API through the sidecar using the configured client credentials.

Override downstream call options using dotted query parameters prefixed with 'optionsOverride.'. Examples:
  ?optionsOverride.Scopes=User.Read
  ?optionsOverride.Scopes=User.Read&optionsOverride.Scopes=Mail.Read
  ?optionsOverride.AcquireTokenOptions.Tenant=GUID
  ?optionsOverride.RequestAppToken=true&optionsOverride.Scopes=https://graph.microsoft.com/.default

### Example


```python
import openapi_client
from openapi_client.models.downstream_api_result import DownstreamApiResult
from openapi_client.models.true_bypass_token_cache import TrueBypassTokenCache
from openapi_client.rest import ApiException
from pprint import pprint

# Defining the host is optional and defaults to http://localhost
# See configuration.py for a list of all supported configuration parameters.
configuration = openapi_client.Configuration(
    host = "http://localhost"
)


# Enter a context with an instance of the API client
with openapi_client.ApiClient(configuration) as api_client:
    # Create an instance of the API class
    api_instance = openapi_client.DownstreamApiEndpointApi(api_client)
    api_name = 'api_name_example' # str | 
    agent_identity = 'agent_identity_example' # str |  (optional)
    agent_username = 'agent_username_example' # str |  (optional)
    options_override_scopes = 'options_override_scopes_example' # str | Repeatable. Each occurrence adds one scope. Example: optionsOverride.Scopes=User.Read (optional)
    options_override_request_app_token = True # bool | true = acquire an app (client credentials) token instead of user token. (optional)
    options_override_base_url = 'options_override_base_url_example' # str | Override downstream API base URL. (optional)
    options_override_relative_path = 'options_override_relative_path_example' # str | Override relative path appended to BaseUrl. (optional)
    options_override_http_method = 'options_override_http_method_example' # str | Override HTTP method (GET, POST, PATCH, etc.). (optional)
    options_override_accept_header = 'options_override_accept_header_example' # str | Sets Accept header (e.g. application/json). (optional)
    options_override_content_type = 'options_override_content_type_example' # str | Sets Content-Type used for serialized body (if body provided). (optional)
    options_override_acquire_token_options_tenant = 'options_override_acquire_token_options_tenant_example' # str | Override tenant (GUID or 'common'). (optional)
    options_override_acquire_token_options_force_refresh = openapi_client.TrueBypassTokenCache() # TrueBypassTokenCache | boolean (optional)
    options_override_acquire_token_options_claims = 'options_override_acquire_token_options_claims_example' # str | JSON claims challenge or extra claims. (optional)
    options_override_acquire_token_options_correlation_id = 'options_override_acquire_token_options_correlation_id_example' # str | GUID correlation id for token acquisition. (optional)
    options_override_acquire_token_options_long_running_web_api_session_key = 'options_override_acquire_token_options_long_running_web_api_session_key_example' # str | Session key for long running OBO flows. (optional)
    options_override_acquire_token_options_fmi_path = 'options_override_acquire_token_options_fmi_path_example' # str | Federated Managed Identity path (if using FMI). (optional)
    options_override_acquire_token_options_pop_public_key = 'options_override_acquire_token_options_pop_public_key_example' # str | Public key or JWK for PoP / AT-POP requests. (optional)
    options_override_acquire_token_options_managed_identity_user_assigned_client_id = 'options_override_acquire_token_options_managed_identity_user_assigned_client_id_example' # str | Managed Identity client id (user-assigned). (optional)

    try:
        # Invoke a configured downstream API through the sidecar using the configured client credentials.
        api_response = api_instance.downstream_api_unauthenticated(api_name, agent_identity=agent_identity, agent_username=agent_username, options_override_scopes=options_override_scopes, options_override_request_app_token=options_override_request_app_token, options_override_base_url=options_override_base_url, options_override_relative_path=options_override_relative_path, options_override_http_method=options_override_http_method, options_override_accept_header=options_override_accept_header, options_override_content_type=options_override_content_type, options_override_acquire_token_options_tenant=options_override_acquire_token_options_tenant, options_override_acquire_token_options_force_refresh=options_override_acquire_token_options_force_refresh, options_override_acquire_token_options_claims=options_override_acquire_token_options_claims, options_override_acquire_token_options_correlation_id=options_override_acquire_token_options_correlation_id, options_override_acquire_token_options_long_running_web_api_session_key=options_override_acquire_token_options_long_running_web_api_session_key, options_override_acquire_token_options_fmi_path=options_override_acquire_token_options_fmi_path, options_override_acquire_token_options_pop_public_key=options_override_acquire_token_options_pop_public_key, options_override_acquire_token_options_managed_identity_user_assigned_client_id=options_override_acquire_token_options_managed_identity_user_assigned_client_id)
        print("The response of DownstreamApiEndpointApi->downstream_api_unauthenticated:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling DownstreamApiEndpointApi->downstream_api_unauthenticated: %s\n" % e)
```



### Parameters


Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **api_name** | **str**|  | 
 **agent_identity** | **str**|  | [optional] 
 **agent_username** | **str**|  | [optional] 
 **options_override_scopes** | **str**| Repeatable. Each occurrence adds one scope. Example: optionsOverride.Scopes&#x3D;User.Read | [optional] 
 **options_override_request_app_token** | **bool**| true &#x3D; acquire an app (client credentials) token instead of user token. | [optional] 
 **options_override_base_url** | **str**| Override downstream API base URL. | [optional] 
 **options_override_relative_path** | **str**| Override relative path appended to BaseUrl. | [optional] 
 **options_override_http_method** | **str**| Override HTTP method (GET, POST, PATCH, etc.). | [optional] 
 **options_override_accept_header** | **str**| Sets Accept header (e.g. application/json). | [optional] 
 **options_override_content_type** | **str**| Sets Content-Type used for serialized body (if body provided). | [optional] 
 **options_override_acquire_token_options_tenant** | **str**| Override tenant (GUID or &#39;common&#39;). | [optional] 
 **options_override_acquire_token_options_force_refresh** | [**TrueBypassTokenCache**](.md)| boolean | [optional] 
 **options_override_acquire_token_options_claims** | **str**| JSON claims challenge or extra claims. | [optional] 
 **options_override_acquire_token_options_correlation_id** | **str**| GUID correlation id for token acquisition. | [optional] 
 **options_override_acquire_token_options_long_running_web_api_session_key** | **str**| Session key for long running OBO flows. | [optional] 
 **options_override_acquire_token_options_fmi_path** | **str**| Federated Managed Identity path (if using FMI). | [optional] 
 **options_override_acquire_token_options_pop_public_key** | **str**| Public key or JWK for PoP / AT-POP requests. | [optional] 
 **options_override_acquire_token_options_managed_identity_user_assigned_client_id** | **str**| Managed Identity client id (user-assigned). | [optional] 

### Return type

[**DownstreamApiResult**](DownstreamApiResult.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json, application/problem+json

### HTTP response details

| Status code | Description | Response headers |
|-------------|-------------|------------------|
**200** | OK |  -  |
**400** | Bad Request |  -  |
**401** | Unauthorized |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

