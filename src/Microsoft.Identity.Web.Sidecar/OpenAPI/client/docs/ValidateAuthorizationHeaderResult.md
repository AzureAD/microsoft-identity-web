# ValidateAuthorizationHeaderResult


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**protocol** | **str** |  | 
**token** | **str** |  | 
**claims** | **object** |  | 

## Example

```python
from openapi_client.models.validate_authorization_header_result import ValidateAuthorizationHeaderResult

# TODO update the JSON string below
json = "{}"
# create an instance of ValidateAuthorizationHeaderResult from a JSON string
validate_authorization_header_result_instance = ValidateAuthorizationHeaderResult.from_json(json)
# print the JSON string representation of the object
print(ValidateAuthorizationHeaderResult.to_json())

# convert the object into a dict
validate_authorization_header_result_dict = validate_authorization_header_result_instance.to_dict()
# create an instance of ValidateAuthorizationHeaderResult from a dict
validate_authorization_header_result_from_dict = ValidateAuthorizationHeaderResult.from_dict(validate_authorization_header_result_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


