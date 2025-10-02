import connexion
from typing import Dict
from typing import Tuple
from typing import Union

from openapi_server.models.authorization_header_result import AuthorizationHeaderResult  # noqa: E501
from openapi_server.models.problem_details import ProblemDetails  # noqa: E501
from openapi_server.models.true_bypass_token_cache import TrueBypassTokenCache  # noqa: E501
from openapi_server import util


def authorization_header(api_name, agent_identity=None, agent_username=None, options_override_scopes=None, options_override_request_app_token=None, options_override_base_url=None, options_override_relative_path=None, options_override_http_method=None, options_override_accept_header=None, options_override_content_type=None, options_override_acquire_token_options_tenant=None, options_override_acquire_token_options_force_refresh=None, options_override_acquire_token_options_claims=None, options_override_acquire_token_options_correlation_id=None, options_override_acquire_token_options_long_running_web_api_session_key=None, options_override_acquire_token_options_fmi_path=None, options_override_acquire_token_options_pop_public_key=None, options_override_acquire_token_options_managed_identity_user_assigned_client_id=None):  # noqa: E501
    """Get an authorization header for a configured downstream API.

    This endpoint will use the identity of the authenticated request to acquire an authorization header.Use dotted query parameters prefixed with &#39;optionsOverride.&#39; to override call settings with respect to the configuration. Examples:   ?optionsOverride.Scopes&#x3D;User.Read&amp;optionsOverride.Scopes&#x3D;Mail.Read   ?optionsOverride.RequestAppToken&#x3D;true&amp;optionsOverride.Scopes&#x3D;https://graph.microsoft.com/.default   ?optionsOverride.AcquireTokenOptions.Tenant&#x3D;GUID Repeat parameters like &#39;optionsOverride.Scopes&#39; to add multiple scopes. # noqa: E501

    :param api_name: 
    :type api_name: str
    :param agent_identity: 
    :type agent_identity: str
    :param agent_username: 
    :type agent_username: str
    :param options_override_scopes: Repeatable. Each occurrence adds one scope. Example: optionsOverride.Scopes&#x3D;User.Read
    :type options_override_scopes: str
    :param options_override_request_app_token: true &#x3D; acquire an app (client credentials) token instead of user token.
    :type options_override_request_app_token: bool
    :param options_override_base_url: Override downstream API base URL.
    :type options_override_base_url: str
    :param options_override_relative_path: Override relative path appended to BaseUrl.
    :type options_override_relative_path: str
    :param options_override_http_method: Override HTTP method (GET, POST, PATCH, etc.).
    :type options_override_http_method: str
    :param options_override_accept_header: Sets Accept header (e.g. application/json).
    :type options_override_accept_header: str
    :param options_override_content_type: Sets Content-Type used for serialized body (if body provided).
    :type options_override_content_type: str
    :param options_override_acquire_token_options_tenant: Override tenant (GUID or &#39;common&#39;).
    :type options_override_acquire_token_options_tenant: str
    :param options_override_acquire_token_options_force_refresh: boolean
    :type options_override_acquire_token_options_force_refresh: dict | bytes
    :param options_override_acquire_token_options_claims: JSON claims challenge or extra claims.
    :type options_override_acquire_token_options_claims: str
    :param options_override_acquire_token_options_correlation_id: GUID correlation id for token acquisition.
    :type options_override_acquire_token_options_correlation_id: str
    :param options_override_acquire_token_options_long_running_web_api_session_key: Session key for long running OBO flows.
    :type options_override_acquire_token_options_long_running_web_api_session_key: str
    :param options_override_acquire_token_options_fmi_path: Federated Managed Identity path (if using FMI).
    :type options_override_acquire_token_options_fmi_path: str
    :param options_override_acquire_token_options_pop_public_key: Public key or JWK for PoP / AT-POP requests.
    :type options_override_acquire_token_options_pop_public_key: str
    :param options_override_acquire_token_options_managed_identity_user_assigned_client_id: Managed Identity client id (user-assigned).
    :type options_override_acquire_token_options_managed_identity_user_assigned_client_id: str

    :rtype: Union[AuthorizationHeaderResult, Tuple[AuthorizationHeaderResult, int], Tuple[AuthorizationHeaderResult, int, Dict[str, str]]
    """
    if connexion.request.is_json:
        options_override_acquire_token_options_force_refresh =  TrueBypassTokenCache.from_dict(connexion.request.get_json())  # noqa: E501
    return 'do some magic!'


def authorization_header_unauthenticated(api_name, agent_identity=None, agent_username=None, options_override_scopes=None, options_override_request_app_token=None, options_override_base_url=None, options_override_relative_path=None, options_override_http_method=None, options_override_accept_header=None, options_override_content_type=None, options_override_acquire_token_options_tenant=None, options_override_acquire_token_options_force_refresh=None, options_override_acquire_token_options_claims=None, options_override_acquire_token_options_correlation_id=None, options_override_acquire_token_options_long_running_web_api_session_key=None, options_override_acquire_token_options_fmi_path=None, options_override_acquire_token_options_pop_public_key=None, options_override_acquire_token_options_managed_identity_user_assigned_client_id=None):  # noqa: E501
    """Get an authorization header for a configured downstream API using this configured client credentials.

    This endpoint will use the configured client credentials to acquire an authorization header.Use dotted query parameters prefixed with &#39;optionsOverride.&#39; to override call settings with respect to the configuration. Examples:   ?optionsOverride.Scopes&#x3D;User.Read&amp;optionsOverride.Scopes&#x3D;Mail.Read   ?optionsOverride.RequestAppToken&#x3D;true&amp;optionsOverride.Scopes&#x3D;https://graph.microsoft.com/.default   ?optionsOverride.AcquireTokenOptions.Tenant&#x3D;GUID Repeat parameters like &#39;optionsOverride.Scopes&#39; to add multiple scopes. # noqa: E501

    :param api_name: 
    :type api_name: str
    :param agent_identity: 
    :type agent_identity: str
    :param agent_username: 
    :type agent_username: str
    :param options_override_scopes: Repeatable. Each occurrence adds one scope. Example: optionsOverride.Scopes&#x3D;User.Read
    :type options_override_scopes: str
    :param options_override_request_app_token: true &#x3D; acquire an app (client credentials) token instead of user token.
    :type options_override_request_app_token: bool
    :param options_override_base_url: Override downstream API base URL.
    :type options_override_base_url: str
    :param options_override_relative_path: Override relative path appended to BaseUrl.
    :type options_override_relative_path: str
    :param options_override_http_method: Override HTTP method (GET, POST, PATCH, etc.).
    :type options_override_http_method: str
    :param options_override_accept_header: Sets Accept header (e.g. application/json).
    :type options_override_accept_header: str
    :param options_override_content_type: Sets Content-Type used for serialized body (if body provided).
    :type options_override_content_type: str
    :param options_override_acquire_token_options_tenant: Override tenant (GUID or &#39;common&#39;).
    :type options_override_acquire_token_options_tenant: str
    :param options_override_acquire_token_options_force_refresh: boolean
    :type options_override_acquire_token_options_force_refresh: dict | bytes
    :param options_override_acquire_token_options_claims: JSON claims challenge or extra claims.
    :type options_override_acquire_token_options_claims: str
    :param options_override_acquire_token_options_correlation_id: GUID correlation id for token acquisition.
    :type options_override_acquire_token_options_correlation_id: str
    :param options_override_acquire_token_options_long_running_web_api_session_key: Session key for long running OBO flows.
    :type options_override_acquire_token_options_long_running_web_api_session_key: str
    :param options_override_acquire_token_options_fmi_path: Federated Managed Identity path (if using FMI).
    :type options_override_acquire_token_options_fmi_path: str
    :param options_override_acquire_token_options_pop_public_key: Public key or JWK for PoP / AT-POP requests.
    :type options_override_acquire_token_options_pop_public_key: str
    :param options_override_acquire_token_options_managed_identity_user_assigned_client_id: Managed Identity client id (user-assigned).
    :type options_override_acquire_token_options_managed_identity_user_assigned_client_id: str

    :rtype: Union[AuthorizationHeaderResult, Tuple[AuthorizationHeaderResult, int], Tuple[AuthorizationHeaderResult, int, Dict[str, str]]
    """
    if connexion.request.is_json:
        options_override_acquire_token_options_force_refresh =  TrueBypassTokenCache.from_dict(connexion.request.get_json())  # noqa: E501
    return 'do some magic!'
