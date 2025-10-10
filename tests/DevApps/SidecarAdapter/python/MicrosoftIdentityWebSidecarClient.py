from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Dict, Iterable, Mapping, MutableMapping, Optional, Sequence
from urllib.parse import urljoin

import requests


JsonDict = Dict[str, Any]


@dataclass(frozen=True)
class ProblemDetails:
    """Represents the RFC 7807 problem details payload returned by the sidecar."""

    type: Optional[str]
    title: Optional[str]
    status: Optional[int]
    detail: Optional[str]
    instance: Optional[str]

    @staticmethod
    def from_dict(data: Mapping[str, Any]) -> "ProblemDetails":
        return ProblemDetails(
            type=data.get("type"),
            title=data.get("title"),
            status=data.get("status"),
            detail=data.get("detail"),
            instance=data.get("instance"),
        )


@dataclass(frozen=True)
class AuthorizationHeaderResult:
    authorization_header: str

    @staticmethod
    def from_dict(data: Mapping[str, Any]) -> "AuthorizationHeaderResult":
        return AuthorizationHeaderResult(authorization_header=data["authorizationHeader"])


@dataclass(frozen=True)
class DownstreamApiResult:
    status_code: int
    headers: Mapping[str, Any]
    content: Any

    @staticmethod
    def from_dict(data: Mapping[str, Any]) -> "DownstreamApiResult":
        return DownstreamApiResult(
            status_code=data["statusCode"],
            headers=data.get("headers", {}),
            content=data.get("content"),
        )


@dataclass(frozen=True)
class ValidateAuthorizationHeaderResult:
    protocol: str
    token: str
    claims: Mapping[str, Any]

    @staticmethod
    def from_dict(data: Mapping[str, Any]) -> "ValidateAuthorizationHeaderResult":
        return ValidateAuthorizationHeaderResult(
            protocol=data["protocol"],
            token=data["token"],
            claims=data.get("claims", {}),
        )


@dataclass(frozen=True)
class AcquireTokenOptions:
    tenant: Optional[str] = None
    force_refresh: Optional[bool] = None
    claims: Optional[str] = None
    correlation_id: Optional[str] = None
    long_running_web_api_session_key: Optional[str] = None
    fmi_path: Optional[str] = None
    pop_public_key: Optional[str] = None
    managed_identity_user_assigned_client_id: Optional[str] = None


@dataclass(frozen=True)
class SidecarCallOptions:
    scopes: Optional[Sequence[str]] = None
    request_app_token: Optional[bool] = None
    base_url: Optional[str] = None
    relative_path: Optional[str] = None
    http_method: Optional[str] = None
    accept_header: Optional[str] = None
    content_type: Optional[str] = None
    acquire_token_options: Optional[AcquireTokenOptions] = None


class SidecarError(Exception):
    """Raised when the sidecar returns an error response."""

    def __init__(self, status_code: int, message: str, problem_details: Optional[ProblemDetails] = None) -> None:
        super().__init__(message)
        self.status_code = status_code
        self.problem_details = problem_details


class MicrosoftIdentityWebSidecarClient:
    """Client for the Microsoft.Identity.Web.Sidecar endpoints."""

    def __init__(
        self,
        base_url: str,
        *,
        session: Optional[requests.Session] = None,
        default_headers: Optional[Mapping[str, str]] = None,
        timeout: Optional[float] = 30.0,
    ) -> None:
        self._base_url = base_url.rstrip("/") + "/"
        self._session = session or requests.Session()
        self._owns_session = session is None
        self._default_headers: Dict[str, str] = dict(default_headers or {})
        self._timeout = timeout

    def close(self) -> None:
        if self._owns_session:
            self._session.close()

    def __enter__(self) -> "MicrosoftIdentityWebSidecarClient":
        return self

    def __exit__(self, exc_type, exc_value, traceback) -> None:  # type: ignore[override]
        self.close()

    def validate_authorization_header(self, authorization_header: str) -> ValidateAuthorizationHeaderResult:
        response_data = self._send_json(
            method="GET",
            path="Validate",
            headers={"Authorization": authorization_header},
        )
        return ValidateAuthorizationHeaderResult.from_dict(response_data)

    def get_authorization_header(
        self,
        api_name: str,
        authorization_header: str,
        *,
        agent_identity: Optional[str] = None,
        agent_username: Optional[str] = None,
        agent_user_id: Optional[str] = None,
        options: Optional[SidecarCallOptions] = None,
    ) -> AuthorizationHeaderResult:
        params = self._build_query_parameters(agent_identity, agent_username, agent_user_id, options)
        response_data = self._send_json(
            method="GET",
            path=f"AuthorizationHeader/{api_name}",
            headers={"Authorization": authorization_header},
            params=params,
        )
        return AuthorizationHeaderResult.from_dict(response_data)

    def get_authorization_header_unauthenticated(
        self,
        api_name: str,
        *,
        agent_identity: Optional[str] = None,
        agent_username: Optional[str] = None,
        agent_user_id: Optional[str] = None,
        options: Optional[SidecarCallOptions] = None,
    ) -> AuthorizationHeaderResult:
        params = self._build_query_parameters(agent_identity, agent_username, agent_user_id, options)
        response_data = self._send_json(
            method="GET",
            path=f"AuthorizationHeaderUnauthenticated/{api_name}",
            params=params,
        )
        return AuthorizationHeaderResult.from_dict(response_data)

    def invoke_downstream_api(
        self,
        api_name: str,
        authorization_header: str,
        *,
        agent_identity: Optional[str] = None,
        agent_username: Optional[str] = None,
        agent_user_id: Optional[str] = None,
        options: Optional[SidecarCallOptions] = None,
        json_body: Any = None,
    ) -> DownstreamApiResult:
        params = self._build_query_parameters(agent_identity, agent_username, agent_user_id, options)
        response_data = self._send_json(
            method="POST",
            path=f"DownstreamApi/{api_name}",
            headers={"Authorization": authorization_header},
            params=params,
            json=json_body,
        )
        return DownstreamApiResult.from_dict(response_data)

    def invoke_downstream_api_unauthenticated(
        self,
        api_name: str,
        *,
        agent_identity: Optional[str] = None,
        agent_username: Optional[str] = None,
        agent_user_id: Optional[str] = None,
        options: Optional[SidecarCallOptions] = None,
        json_body: Any = None,
    ) -> DownstreamApiResult:
        params = self._build_query_parameters(agent_identity, agent_username, agent_user_id, options)
        response_data = self._send_json(
            method="POST",
            path=f"DownstreamApiUnauthenticated/{api_name}",
            params=params,
            json=json_body,
        )
        return DownstreamApiResult.from_dict(response_data)

    def with_default_authorization(self, authorization_header: str) -> "MicrosoftIdentityWebSidecarClient":
        """Return a new client instance that always sends the given Authorization header."""

        headers = dict(self._default_headers)
        headers["Authorization"] = authorization_header
        return MicrosoftIdentityWebSidecarClient(
            self._base_url,
            session=self._session,
            default_headers=headers,
            timeout=self._timeout,
        )

    def _build_query_parameters(
        self,
        agent_identity: Optional[str],
        agent_username: Optional[str],
        agent_user_id: Optional[str],
        options: Optional[SidecarCallOptions],
    ) -> Dict[str, Any]:
        params: Dict[str, Any] = {}
        if agent_identity:
            params["AgentIdentity"] = agent_identity
        if agent_username:
            params["AgentUsername"] = agent_username
        if agent_user_id:
            params["AgentUserId"] = agent_user_id

        if options:
            if options.scopes:
                params["optionsOverride.Scopes"] = list(options.scopes)
            if options.request_app_token is not None:
                params["optionsOverride.RequestAppToken"] = _to_bool_str(options.request_app_token)
            if options.base_url:
                params["optionsOverride.BaseUrl"] = options.base_url
            if options.relative_path:
                params["optionsOverride.RelativePath"] = options.relative_path
            if options.http_method:
                params["optionsOverride.HttpMethod"] = options.http_method
            if options.accept_header:
                params["optionsOverride.AcceptHeader"] = options.accept_header
            if options.content_type:
                params["optionsOverride.ContentType"] = options.content_type

            if options.acquire_token_options:
                acquire_options = options.acquire_token_options
                if acquire_options.tenant:
                    params["optionsOverride.AcquireTokenOptions.Tenant"] = acquire_options.tenant
                if acquire_options.force_refresh is not None:
                    params[
                        "optionsOverride.AcquireTokenOptions.ForceRefresh"
                    ] = _to_bool_str(acquire_options.force_refresh)
                if acquire_options.claims:
                    params["optionsOverride.AcquireTokenOptions.Claims"] = acquire_options.claims
                if acquire_options.correlation_id:
                    params[
                        "optionsOverride.AcquireTokenOptions.CorrelationId"
                    ] = acquire_options.correlation_id
                if acquire_options.long_running_web_api_session_key:
                    params[
                        "optionsOverride.AcquireTokenOptions.LongRunningWebApiSessionKey"
                    ] = acquire_options.long_running_web_api_session_key
                if acquire_options.fmi_path:
                    params["optionsOverride.AcquireTokenOptions.FmiPath"] = acquire_options.fmi_path
                if acquire_options.pop_public_key:
                    params[
                        "optionsOverride.AcquireTokenOptions.PopPublicKey"
                    ] = acquire_options.pop_public_key
                if acquire_options.managed_identity_user_assigned_client_id:
                    params[
                        "optionsOverride.AcquireTokenOptions.ManagedIdentity.UserAssignedClientId"
                    ] = acquire_options.managed_identity_user_assigned_client_id
        return params

    def _send_json(
        self,
        *,
        method: str,
        path: str,
        headers: Optional[Mapping[str, str]] = None,
        params: Optional[Mapping[str, Any]] = None,
        json: Any = None,
    ) -> JsonDict:
        response = self._send(
            method=method,
            path=path,
            headers=headers,
            params=params,
            json=json,
        )
        try:
            return response.json()
        except ValueError as exc:
            raise SidecarError(response.status_code, "Expected JSON response from sidecar") from exc

    def _send(
        self,
        *,
        method: str,
        path: str,
        headers: Optional[Mapping[str, str]] = None,
        params: Optional[Mapping[str, Any]] = None,
        json: Any = None,
    ) -> requests.Response:
        url = urljoin(self._base_url, path)
        request_headers: MutableMapping[str, str] = dict(self._default_headers)
        if headers:
            request_headers.update(headers)

        prepared_params = _prepare_params(params)

        response = self._session.request(
            method=method,
            url=url,
            headers=request_headers,
            params=prepared_params,
            json=json,
            timeout=self._timeout,
        )
        if response.status_code >= 400:
            self._raise_sidecar_error(response)
        return response

    def _raise_sidecar_error(self, response: requests.Response) -> None:
        problem_details: Optional[ProblemDetails] = None
        message = f"Sidecar request failed with status code {response.status_code}"
        try:
            data = response.json()
        except ValueError:
            pass
        else:
            if isinstance(data, Mapping):
                problem_details = ProblemDetails.from_dict(data)
                detail = problem_details.detail or problem_details.title
                if detail:
                    message = detail
        raise SidecarError(response.status_code, message, problem_details)


def _prepare_params(params: Optional[Mapping[str, Any]]) -> Optional[Mapping[str, Any]]:
    if not params:
        return params
    prepared: Dict[str, Any] = {}
    for key, value in params.items():
        if isinstance(value, Iterable) and not isinstance(value, (str, bytes)):
            prepared[key] = list(value)
        else:
            prepared[key] = value
    return prepared


def _to_bool_str(value: bool) -> str:
    return "true" if value else "false"
