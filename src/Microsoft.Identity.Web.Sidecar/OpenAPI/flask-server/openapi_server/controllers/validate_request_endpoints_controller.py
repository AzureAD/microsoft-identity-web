import connexion
from typing import Dict
from typing import Tuple
from typing import Union

from openapi_server.models.problem_details import ProblemDetails  # noqa: E501
from openapi_server.models.validate_authorization_header_result import ValidateAuthorizationHeaderResult  # noqa: E501
from openapi_server import util


def validate_authorization_header():  # noqa: E501
    """validate_authorization_header

     # noqa: E501


    :rtype: Union[ValidateAuthorizationHeaderResult, Tuple[ValidateAuthorizationHeaderResult, int], Tuple[ValidateAuthorizationHeaderResult, int, Dict[str, str]]
    """
    return 'do some magic!'
