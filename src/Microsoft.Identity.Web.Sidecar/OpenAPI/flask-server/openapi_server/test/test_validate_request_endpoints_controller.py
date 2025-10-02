import unittest

from flask import json

from openapi_server.models.problem_details import ProblemDetails  # noqa: E501
from openapi_server.models.validate_authorization_header_result import ValidateAuthorizationHeaderResult  # noqa: E501
from openapi_server.test import BaseTestCase


class TestValidateRequestEndpointsController(BaseTestCase):
    """ValidateRequestEndpointsController integration test stubs"""

    def test_validate_authorization_header(self):
        """Test case for validate_authorization_header

        
        """
        headers = { 
            'Accept': 'application/json',
        }
        response = self.client.open(
            '/Validate',
            method='GET',
            headers=headers)
        self.assert200(response,
                       'Response body is : ' + response.data.decode('utf-8'))


if __name__ == '__main__':
    unittest.main()
