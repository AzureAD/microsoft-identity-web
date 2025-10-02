# openapi_client.ValidateRequestEndpointsApi

All URIs are relative to *http://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**validate_authorization_header**](ValidateRequestEndpointsApi.md#validate_authorization_header) | **GET** /Validate | 


# **validate_authorization_header**
> ValidateAuthorizationHeaderResult validate_authorization_header()

### Example


```python
import openapi_client
from openapi_client.models.validate_authorization_header_result import ValidateAuthorizationHeaderResult
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
    api_instance = openapi_client.ValidateRequestEndpointsApi(api_client)

    try:
        api_response = api_instance.validate_authorization_header()
        print("The response of ValidateRequestEndpointsApi->validate_authorization_header:\n")
        pprint(api_response)
    except Exception as e:
        print("Exception when calling ValidateRequestEndpointsApi->validate_authorization_header: %s\n" % e)
```



### Parameters

This endpoint does not need any parameter.

### Return type

[**ValidateAuthorizationHeaderResult**](ValidateAuthorizationHeaderResult.md)

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

