# DownstreamApiResult


## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**status_code** | **int** |  | 
**headers** | **Dict[str, List[str]]** |  | 
**content** | **str** |  | 

## Example

```python
from openapi_client.models.downstream_api_result import DownstreamApiResult

# TODO update the JSON string below
json = "{}"
# create an instance of DownstreamApiResult from a JSON string
downstream_api_result_instance = DownstreamApiResult.from_json(json)
# print the JSON string representation of the object
print(DownstreamApiResult.to_json())

# convert the object into a dict
downstream_api_result_dict = downstream_api_result_instance.to_dict()
# create an instance of DownstreamApiResult from a dict
downstream_api_result_from_dict = DownstreamApiResult.from_dict(downstream_api_result_dict)
```
[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


