from __future__ import annotations
from dataclasses import dataclass, field, asdict
from typing import Optional, Dict, Any, List
import requests
import json
import uuid

# -------- Models --------

@dataclass
class ProblemDetails:
    type: Optional[str] = None
    title: Optional[str] = None
    status: Optional[int] = None
    detail: Optional[str] = None
    instance: Optional[str] = None
    # Allow extra fields
    extra: Dict[str, Any] = field(default_factory=dict)

    @staticmethod
    def from_dict(d: Dict[str, Any]) -> "ProblemDetails":
        known = {k: d.get(k) for k in ["type", "title", "status", "detail", "instance"]}
        extra = {k: v for k, v in d.items() if k not in known}
        return ProblemDetails(**known, extra=extra)

@dataclass
class ManagedIdentityOptions:
    userAssignedClientId: Optional[str] = None

@dataclass
class AcquireTokenOptions:
    authenticationOptionsName: Optional[str] = None
    correlationId: Optional[str] = None  # uuid string
    extraQueryParameters: Optional[Dict[str, str]] = None
    extraParameters: Optional[Dict[str, Any]] = None
    extraHeadersParameters: Optional[Dict[str, str]] = None
    claims: Optional[str] = None
    fmiPath: Optional[str] = None
    forceRefresh: Optional[bool] = None
    popPublicKey: Optional[str] = None
    popClaim: Optional[str] = None
    managedIdentity: Optional[ManagedIdentityOptions] = None
    longRunningWebApiSessionKey: Optional[str] = None
    tenant: Optional[str] = None
    userFlow: Optional[str] = None

@dataclass
class DownstreamApiOptions:
    scopes: Optional[List[str]] = None
    acceptHeader: Optional[str] = None
    contentType: Optional[str] = None
    extraHeaderParameters: Optional[Dict[str, str]] = None
    extraQueryParameters: Optional[Dict[str, str]] = None
    baseUrl: Optional[str] = None
    relativePath: Optional[str] = None
    httpMethod: Optional[str] = None  # default server side: "Get"
    acquireTokenOptions: Optional[AcquireTokenOptions] = None
    protocolScheme: Optional[str] = None  # default server side: "Bearer"
    requestAppToken: Optional[bool] = None
    # serializer / deserializer / customizeHttpRequestMessage intentionally omitted (server-only)

@dataclass
class AuthorizationHeaderResult:
    authorizationHeader: str

    @staticmethod
    def from_dict(d: Dict[str, Any]) -> "AuthorizationHeaderResult":
        return AuthorizationHeaderResult(authorizationHeader=d["authorizationHeader"])

@dataclass
class ValidateAuthorizationHeaderResult:
    protocol: str
    token: str
    claims: Any

    @staticmethod
    def from_dict(d: Dict[str, Any]) -> "ValidateAuthorizationHeaderResult":
        return ValidateAuthorizationHeaderResult(
            protocol=d["protocol"],
            token=d["token"],
            claims=d["claims"]
        )

# -------- Errors --------

class SidecarError(Exception):
    def __init__(self, message: str, problem: Optional[ProblemDetails] = None, status_code: Optional[int] = None):
        super().__init__(message)
        self.problem = problem
        self.status_code = status_code

# -------- Client --------

class SidecarClient:
    def __init__(
        self,
        base_url: str = "https://localhost:7255",
        *,
        timeout: float = 30.0,
        default_headers: Optional[Dict[str, str]] = None,
        verify_tls: bool = True,
        session: Optional[requests.Session] = None
    ):
        self.base_url = base_url.rstrip("/")
        self.timeout = timeout
        self.session = session or requests.Session()
        self.verify_tls = verify_tls
        self.default_headers = {
            "Accept": "application/json",
            "Content-Type": "application/json",
            **(default_headers or {})
        }

    def _request(self, method: str, path: str, *, params=None, json_body=None) -> Dict[str, Any]:
        url = f"{self.base_url}{path}"
        try:
            resp = self.session.request(
                method=method,
                url=url,
                headers=self.default_headers,
                params=params,
                json=json_body,
                timeout=self.timeout,
                verify=self.verify_tls
            )
        except requests.RequestException as ex:
            raise SidecarError(f"Network error calling {url}: {ex}") from ex

        content_text = resp.text or ""
        parsed: Any = None
        if content_text:
            try:
                parsed = resp.json()
            except ValueError:
                parsed = content_text  # leave raw

        if resp.status_code >= 400:
            problem = None
            if isinstance(parsed, dict):
                problem = ProblemDetails.from_dict(parsed)
            raise SidecarError(
                f"Sidecar HTTP {resp.status_code}",
                problem=problem,
                status_code=resp.status_code
            )
        if not isinstance(parsed, dict):
            raise SidecarError(f"Unexpected non-JSON response for {url}")
        return parsed

    # GET /Validate
    def validate(self) -> ValidateAuthorizationHeaderResult:
        data = self._request("GET", "/Validate")
        return ValidateAuthorizationHeaderResult.from_dict(data)

    # POST /AuthorizationHeader/{apiName}
    def get_authorization_header(
        self,
        api_name: str,
        *,
        agent_identity: Optional[str] = None,
        agent_username: Optional[str] = None,
        tenant: Optional[str] = None,
        options: Optional[DownstreamApiOptions] = None
    ) -> AuthorizationHeaderResult:
        if not api_name:
            raise ValueError("api_name is required")
        params = {}
        if agent_identity:
            params["agentIdentity"] = agent_identity
        if agent_username:
            params["agentUsername"] = agent_username
        if tenant:
            params["tenant"] = tenant

        body_dict = self._serialize_downstream_options(options)
        data = self._request("POST", f"/AuthorizationHeader/{requests.utils.quote(api_name, safe='')}",
                             params=params,
                             json_body=body_dict)
        return AuthorizationHeaderResult.from_dict(data)

    def _serialize_downstream_options(self, opts: Optional[DownstreamApiOptions]) -> Dict[str, Any]:
        if not opts:
            return {}
        def recurse(obj):
            if obj is None:
                return None
            if isinstance(obj, (str, int, float, bool)):
                return obj
            if isinstance(obj, list):
                return [recurse(x) for x in obj]
            if isinstance(obj, dict):
                return {k: recurse(v) for k, v in obj.items() if v is not None}
            if hasattr(obj, "__dataclass_fields__"):
                d = asdict(obj)
                return {k: recurse(v) for k, v in d.items() if v is not None}
            return obj
        return recurse(opts) or {}

# -------- Convenience factory for local dev (disables TLS verify) --------

def create_insecure_localhost_client() -> SidecarClient:
    # For local development ONLY (self-signed cert). Do not use in production.
    return SidecarClient(verify_tls=False)

# -------- Example usage (remove or guard under if __name__ == '__main__') --------
if __name__ == "__main__":
    client = create_insecure_localhost_client()
    try:
        v = client.validate()
        print("Validate:", v)
        hdr = client.get_authorization_header(
            "MyDownstreamApi",
            options=DownstreamApiOptions(
                scopes=["api://your-api-guid/.default"],
                relativePath="/v1/data",
                httpMethod="Get"
            )
        )
        print("Authorization header:", hdr.authorizationHeader)
    except SidecarError as e:
        if e.problem:
            print("Error:", e.problem.title, e.problem.detail, e.status_code)
        else:
            print("Error:", e)
