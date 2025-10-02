import unittest

from flask import json

from openapi_server.models.authorization_header_result import AuthorizationHeaderResult  # noqa: E501
from openapi_server.models.problem_details import ProblemDetails  # noqa: E501
from openapi_server.models.true_bypass_token_cache import TrueBypassTokenCache  # noqa: E501
from openapi_server.test import BaseTestCase


class TestAuthorizationHeaderEndpointController(BaseTestCase):
    """AuthorizationHeaderEndpointController integration test stubs"""

    def test_authorization_header(self):
        """Test case for authorization_header

        Get an authorization header for a configured downstream API.
        """
        query_string = [('AgentIdentity', 'agent_identity_example'),
                        ('AgentUsername', 'agent_username_example'),
                        ('optionsOverride.Scopes', 'options_override_scopes_example'),
                        ('optionsOverride.RequestAppToken', True),
                        ('optionsOverride.BaseUrl', 'options_override_base_url_example'),
                        ('optionsOverride.RelativePath', 'options_override_relative_path_example'),
                        ('optionsOverride.HttpMethod', 'options_override_http_method_example'),
                        ('optionsOverride.AcceptHeader', 'options_override_accept_header_example'),
                        ('optionsOverride.ContentType', 'options_override_content_type_example'),
                        ('optionsOverride.AcquireTokenOptions.Tenant', 'options_override_acquire_token_options_tenant_example'),
                        ('optionsOverride.AcquireTokenOptions.ForceRefresh', openapi_server.TrueBypassTokenCache()),
                        ('optionsOverride.AcquireTokenOptions.Claims', 'options_override_acquire_token_options_claims_example'),
                        ('optionsOverride.AcquireTokenOptions.CorrelationId', 'options_override_acquire_token_options_correlation_id_example'),
                        ('optionsOverride.AcquireTokenOptions.LongRunningWebApiSessionKey', 'options_override_acquire_token_options_long_running_web_api_session_key_example'),
                        ('optionsOverride.AcquireTokenOptions.FmiPath', 'options_override_acquire_token_options_fmi_path_example'),
                        ('optionsOverride.AcquireTokenOptions.PopPublicKey', 'options_override_acquire_token_options_pop_public_key_example'),
                        ('optionsOverride.AcquireTokenOptions.ManagedIdentity.UserAssignedClientId', 'options_override_acquire_token_options_managed_identity_user_assigned_client_id_example')]
        headers = { 
            'Accept': 'application/json',
        }
        response = self.client.open(
            '/AuthorizationHeader/{api_name}'.format(api_name='api_name_example'),
            method='GET',
            headers=headers,
            query_string=query_string)
        self.assert200(response,
                       'Response body is : ' + response.data.decode('utf-8'))

    def test_authorization_header_unauthenticated(self):
        """Test case for authorization_header_unauthenticated

        Get an authorization header for a configured downstream API using this configured client credentials.
        """
        query_string = [('AgentIdentity', 'agent_identity_example'),
                        ('AgentUsername', 'agent_username_example'),
                        ('optionsOverride.Scopes', 'options_override_scopes_example'),
                        ('optionsOverride.RequestAppToken', True),
                        ('optionsOverride.BaseUrl', 'options_override_base_url_example'),
                        ('optionsOverride.RelativePath', 'options_override_relative_path_example'),
                        ('optionsOverride.HttpMethod', 'options_override_http_method_example'),
                        ('optionsOverride.AcceptHeader', 'options_override_accept_header_example'),
                        ('optionsOverride.ContentType', 'options_override_content_type_example'),
                        ('optionsOverride.AcquireTokenOptions.Tenant', 'options_override_acquire_token_options_tenant_example'),
                        ('optionsOverride.AcquireTokenOptions.ForceRefresh', openapi_server.TrueBypassTokenCache()),
                        ('optionsOverride.AcquireTokenOptions.Claims', 'options_override_acquire_token_options_claims_example'),
                        ('optionsOverride.AcquireTokenOptions.CorrelationId', 'options_override_acquire_token_options_correlation_id_example'),
                        ('optionsOverride.AcquireTokenOptions.LongRunningWebApiSessionKey', 'options_override_acquire_token_options_long_running_web_api_session_key_example'),
                        ('optionsOverride.AcquireTokenOptions.FmiPath', 'options_override_acquire_token_options_fmi_path_example'),
                        ('optionsOverride.AcquireTokenOptions.PopPublicKey', 'options_override_acquire_token_options_pop_public_key_example'),
                        ('optionsOverride.AcquireTokenOptions.ManagedIdentity.UserAssignedClientId', 'options_override_acquire_token_options_managed_identity_user_assigned_client_id_example')]
        headers = { 
            'Accept': 'application/json',
        }
        response = self.client.open(
            '/AuthorizationHeaderUnauthenticated/{api_name}'.format(api_name='api_name_example'),
            method='GET',
            headers=headers,
            query_string=query_string)
        self.assert200(response,
                       'Response body is : ' + response.data.decode('utf-8'))


if __name__ == '__main__':
    unittest.main()
