Quick start:
1.	pip install requests
2.	Place file as shown.
3.	Use:

```python
from Microsoft.Identity.Web.Sidecar.python.sidecar_client

import SidecarClient, DownstreamApiOptions
client = SidecarClient()  # or create_insecure_localhost_client()
result = client.validate()
header = client.get_authorization_header("ApiName", options=DownstreamApiOptions(relativePath="/ping"))
```

Adjust:
•	Add logging where needed.
•	Extend error mapping if server adds more problem fields.
•	Replace create_insecure_localhost_client() in production with a properly trusted cert.
Let me know if you prefer httpx / async variant. 
