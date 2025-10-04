import argparse
import json
from pathlib import Path
from typing import Any, Optional

from MicrosoftIdentityWebSidecarClient import (
    AcquireTokenOptions,
    MicrosoftIdentityWebSidecarClient,
    SidecarCallOptions,
    SidecarError,
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Exercise the Microsoft Identity Web Sidecar client against a running sidecar instance.",
    )
    parser.add_argument(
        "--base-url",
        required=True,
        help="Fully qualified base URL for the sidecar (e.g. https://localhost:5001/sidecar).",
    )
    parser.add_argument(
        "--authorization-header",
        help="Authorization header to send for authenticated endpoints (e.g. 'Bearer <token>').",
    )
    parser.add_argument(
        "--agent-identity",
        help="Optional AgentIdentity query parameter for the sidecar call.",
    )
    parser.add_argument(
        "--agent-username",
        help="Optional AgentUsername query parameter for the sidecar call.",
    )
    parser.add_argument(
        "--agent-user-id",
        help="Optional AgentUserId query parameter for the sidecar call.",
    )

    subparsers = parser.add_subparsers(dest="command", required=True)

    subparsers.add_parser("validate", help="Validate an authorization header using the /Validate endpoint.")

    auth_header_parser = subparsers.add_parser(
        "get-auth-header",
        help="Call /AuthorizationHeader/{apiName}.",
    )
    auth_header_parser.add_argument("api_name", help="Configured API name defined in the sidecar configuration.")
    _augment_with_options_override(auth_header_parser)

    auth_header_unauth_parser = subparsers.add_parser(
        "get-auth-header-unauth", help="Call /AuthorizationHeaderUnauthenticated/{apiName}."
    )
    auth_header_unauth_parser.add_argument(
        "api_name", help="Configured API name defined in the sidecar configuration."
    )
    _augment_with_options_override(auth_header_unauth_parser)

    downstream_parser = subparsers.add_parser(
        "invoke-downstream",
        help="Call /DownstreamApi/{apiName} with an optional JSON body.",
    )
    downstream_parser.add_argument("api_name", help="Configured API name defined in the sidecar configuration.")
    downstream_parser.add_argument(
        "--body-json",
        help="Inline JSON payload to POST to the downstream API.",
    )
    downstream_parser.add_argument(
        "--body-file",
        type=Path,
        help="Path to a JSON file to POST to the downstream API.",
    )
    _augment_with_options_override(downstream_parser)

    downstream_unauth_parser = subparsers.add_parser(
        "invoke-downstream-unauth",
        help="Call /DownstreamApiUnauthenticated/{apiName} with an optional JSON body.",
    )
    downstream_unauth_parser.add_argument("api_name", help="Configured API name defined in the sidecar configuration.")
    downstream_unauth_parser.add_argument(
        "--body-json",
        help="Inline JSON payload to POST to the downstream API.",
    )
    downstream_unauth_parser.add_argument(
        "--body-file",
        type=Path,
        help="Path to a JSON file to POST to the downstream API.",
    )
    _augment_with_options_override(downstream_unauth_parser)

    return parser.parse_args()


def _augment_with_options_override(subparser: argparse.ArgumentParser) -> None:
    subparser.add_argument(
        "--scope",
        dest="scopes",
        action="append",
        help="Repeatable. Adds a scope to optionsOverride.Scopes.",
    )
    subparser.add_argument(
        "--request-app-token",
        dest="request_app_token",
        action="store_const",
        const=True,
        default=None,
        help="Set optionsOverride.RequestAppToken=true.",
    )
    subparser.add_argument(
        "--base-url-override",
        dest="override_base_url",
        help="Sets optionsOverride.BaseUrl.",
    )
    subparser.add_argument(
        "--relative-path",
        dest="relative_path",
        help="Sets optionsOverride.RelativePath.",
    )
    subparser.add_argument(
        "--http-method",
        dest="http_method",
        help="Sets optionsOverride.HttpMethod.",
    )
    subparser.add_argument(
        "--accept-header",
        dest="accept_header",
        help="Sets optionsOverride.AcceptHeader.",
    )
    subparser.add_argument(
        "--content-type",
        dest="content_type",
        help="Sets optionsOverride.ContentType.",
    )
    subparser.add_argument(
        "--tenant",
        dest="tenant",
        help="Sets optionsOverride.AcquireTokenOptions.Tenant.",
    )
    subparser.add_argument(
        "--force-refresh",
        dest="force_refresh",
        action="store_const",
        const=True,
        default=None,
        help="Sets optionsOverride.AcquireTokenOptions.ForceRefresh=true.",
    )
    subparser.add_argument(
        "--claims",
        dest="claims",
        help="Sets optionsOverride.AcquireTokenOptions.Claims.",
    )
    subparser.add_argument(
        "--correlation-id",
        dest="correlation_id",
        help="Sets optionsOverride.AcquireTokenOptions.CorrelationId.",
    )
    subparser.add_argument(
        "--long-running-session-key",
        dest="long_running_session_key",
        help="Sets optionsOverride.AcquireTokenOptions.LongRunningWebApiSessionKey.",
    )
    subparser.add_argument(
        "--fmi-path",
        dest="fmi_path",
        help="Sets optionsOverride.AcquireTokenOptions.FmiPath.",
    )
    subparser.add_argument(
        "--pop-public-key",
        dest="pop_public_key",
        help="Sets optionsOverride.AcquireTokenOptions.PopPublicKey.",
    )
    subparser.add_argument(
        "--managed-identity-client-id",
        dest="managed_identity_client_id",
        help="Sets optionsOverride.AcquireTokenOptions.ManagedIdentity.UserAssignedClientId.",
    )


def build_call_options(args: argparse.Namespace) -> Optional[SidecarCallOptions]:
    if not any(
        getattr(args, attr, None)
        for attr in (
            "scopes",
            "request_app_token",
            "override_base_url",
            "relative_path",
            "http_method",
            "accept_header",
            "content_type",
            "tenant",
            "force_refresh",
            "claims",
            "correlation_id",
            "long_running_session_key",
            "fmi_path",
            "pop_public_key",
            "managed_identity_client_id",
        )
    ):
        return None

    acquire_options = AcquireTokenOptions(
        tenant=getattr(args, "tenant", None),
        force_refresh=getattr(args, "force_refresh", None),
        claims=getattr(args, "claims", None),
        correlation_id=getattr(args, "correlation_id", None),
        long_running_web_api_session_key=getattr(args, "long_running_session_key", None),
        fmi_path=getattr(args, "fmi_path", None),
        pop_public_key=getattr(args, "pop_public_key", None),
        managed_identity_user_assigned_client_id=getattr(args, "managed_identity_client_id", None),
    )

    if not any(
        getattr(acquire_options, field)
        is not None
        for field in (
            "tenant",
            "force_refresh",
            "claims",
            "correlation_id",
            "long_running_web_api_session_key",
            "fmi_path",
            "pop_public_key",
            "managed_identity_user_assigned_client_id",
        )
    ):
        acquire_options = None

    return SidecarCallOptions(
    scopes=getattr(args, "scopes", None),
    request_app_token=getattr(args, "request_app_token", None),
    base_url=getattr(args, "override_base_url", None),
    relative_path=getattr(args, "relative_path", None),
    http_method=getattr(args, "http_method", None),
    accept_header=getattr(args, "accept_header", None),
    content_type=getattr(args, "content_type", None),
        acquire_token_options=acquire_options,
    )


def _resolve_json_body(args: argparse.Namespace) -> Optional[Any]:
    if getattr(args, "body_json", None):
        return json.loads(args.body_json)
    if getattr(args, "body_file", None):
        data = args.body_file.read_text(encoding="utf-8")
        return json.loads(data)
    return None


def ensure_authorization_header(args: argparse.Namespace) -> str:
    if not args.authorization_header:
        raise SystemExit("This command requires --authorization-header.")
    return args.authorization_header


def main() -> None:
    args = parse_args()
    options = build_call_options(args)

    try:
        with MicrosoftIdentityWebSidecarClient(args.base_url) as client:
            if args.command == "validate":
                authorization_header = ensure_authorization_header(args)
                result = client.validate_authorization_header(authorization_header)
                _print_json({
                    "protocol": result.protocol,
                    "token": result.token,
                    "claims": result.claims,
                })
            elif args.command == "get-auth-header":
                authorization_header = ensure_authorization_header(args)
                result = client.get_authorization_header(
                    args.api_name,
                    authorization_header,
                    agent_identity=args.agent_identity,
                    agent_username=args.agent_username,
                    agent_user_id=args.agent_user_id,
                    options=options,
                )
                _print_json({"authorizationHeader": result.authorization_header})
            elif args.command == "get-auth-header-unauth":
                result = client.get_authorization_header_unauthenticated(
                    args.api_name,
                    agent_identity=args.agent_identity,
                    agent_username=args.agent_username,
                    agent_user_id=args.agent_user_id,
                    options=options,
                )
                _print_json({"authorizationHeader": result.authorization_header})
            elif args.command == "invoke-downstream":
                authorization_header = ensure_authorization_header(args)
                body = _resolve_json_body(args)
                result = client.invoke_downstream_api(
                    args.api_name,
                    authorization_header,
                    agent_identity=args.agent_identity,
                    agent_username=args.agent_username,
                    agent_user_id=args.agent_user_id,
                    options=options,
                    json_body=body,
                )
                _print_json({
                    "statusCode": result.status_code,
                    "headers": result.headers,
                    "content": result.content,
                })
            elif args.command == "invoke-downstream-unauth":
                body = _resolve_json_body(args)
                result = client.invoke_downstream_api_unauthenticated(
                    args.api_name,
                    agent_identity=args.agent_identity,
                    agent_username=args.agent_username,
                    agent_user_id=args.agent_user_id,
                    options=options,
                    json_body=body,
                )
                _print_json({
                    "statusCode": result.status_code,
                    "headers": result.headers,
                    "content": result.content,
                })
            else:
                raise SystemExit(f"Unsupported command: {args.command}")
    except SidecarError as sidecar_error:
        _handle_sidecar_error(sidecar_error)


def _print_json(payload: Any) -> None:
    print(json.dumps(payload, indent=2, ensure_ascii=False))


def _handle_sidecar_error(error: SidecarError) -> None:
    details: dict[str, Any] = {
        "statusCode": error.status_code,
    }
    if error.problem_details:
        details["problemDetails"] = {
            "type": error.problem_details.type,
            "title": error.problem_details.title,
            "status": error.problem_details.status,
            "detail": error.problem_details.detail,
            "instance": error.problem_details.instance,
        }
    else:
        details["message"] = str(error)
    print(json.dumps(details, indent=2, ensure_ascii=False))
    raise SystemExit(1)


if __name__ == "__main__":
    main()
