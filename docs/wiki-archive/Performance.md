There have been performance testing done with Microsoft Identity Web. Below is a description of test environments and findings.

### Test setup
A web API ([PerformanceTestService](https://github.com/AzureAD/microsoft-identity-web/tree/master/tests/PerformanceTests/PerformanceTestService)) was created with controllers that have actions that perform only a specific operation. The testing was specifically focused on common operations like getting an access token for user by using a `TokenAcquisition` class ([WeatherForecastController](https://github.com/AzureAD/microsoft-identity-web/blob/master/tests/PerformanceTests/PerformanceTestService/Controllers/WeatherForecastController.cs)). 

A [test runner client](https://github.com/AzureAD/microsoft-identity-web/tree/master/tests/PerformanceTests/Microsoft.Identity.Web.Perf.Client) was created to send authenticated requests to the web API. Test user accounts were created in Azure Active Directory. The test runner acquires a token for users by using MSAL.NET Public Client Application and also caches it for future calls. Then an HTTP client sends continuous requests to the specified web API endpoint for a specified duration.

Two separate environments were used to host the IntegrationTestService. An Azure Virtual Machine (8 core @ 2.35GHz CPU, 32GB memory, SSD, Windows Server 2019, ASP.NET Core 3.1.402) and an App Services instance (S1 plan, 1 core A-series equivalent CPU, 1.75GB memory, ASP.NET Core 3.1.302).

There were a few performance measuring tools used. [Dotnet-counters](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters) - to gather machine and ASP.NET Core related metrics. Application Insights was used in conjunction with App Service hosted web API. [Event counters](https://github.com/AzureAD/microsoft-identity-web/tree/master/tests/IntegrationTests/IntegrationTestService/EventSource) were also added to the cache operations (reads, writes) and were surfaced in App Insights.

### Findings
Multiple test runner instances and configurations were used. For example, the client calling the web API for 1, 100, 1k, and 2k users and running for at least 1 hour to 2 day durations. Note: The change in metrics in the middle of the charts is because of App Service restart.

Average CPU usage on App Services was ~7% for a few concurrent test clients and ~20% for larger number of concurrent clients. On a VM, CPU usage was 0-3%.
![Avg CPU](https://user-images.githubusercontent.com/34331512/94507420-8ca87480-01c4-11eb-8143-30ab3a4f7d51.png)

Average process private bytes on App Services were 400MB (with multiple clients sending requests at a time). On the VM, average memory working set was 145-170MB (with only one client sending requests at a time). 
![Avg Memory](https://user-images.githubusercontent.com/34331512/94507421-8ca87480-01c4-11eb-92aa-e20d8e044ea9.png)

Token cache behaves as expected. First operation is a write and the rest are reads. For a new request, each write operation is complemented with a read; so the total reads are higher than total requests by a sum of reads.
![Avg cache ops](https://user-images.githubusercontent.com/34331512/94507416-8b774780-01c4-11eb-9ec7-ebcd7154cb6f.png)

Below is response time and duration distribution for 4.89M calls.
![Avg Response Time](https://user-images.githubusercontent.com/34331512/94507419-8c0fde00-01c4-11eb-8078-e1aff878a903.png)
![Request time distribution](https://user-images.githubusercontent.com/34331512/94507422-8ca87480-01c4-11eb-83e8-db6851f44999.png)
| Metric | Duration |
|-|-|
| AVG | 13.3ms |
| P50 | 2.28ms |
| P95 | 119ms |
| P99 | 161ms |

### Future work
We continue to invest in the performance testing. Future work includes increasing the number of metrics (ex. cache size growth) and test cases, running the performance tests on a regular schedule, creating more reports (ex. via App Insights).