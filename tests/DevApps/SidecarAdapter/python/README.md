# Sidecar Adapter Python

This folder contains helper scripts for interacting with the Microsoft Identity Web Sidecar from Python.

## Requirements

- [Install UV](https://astral.sh/uv)

## Contents

- `MicrosoftIdentityWebSidecarClient.py` – Typed client covering the Sidecar's `/Validate`, `/AuthorizationHeader`, and `/DownstreamApi` endpoints.
- `main.py` – Command-line harness that exercises the client and prints JSON responses.
- `get_token.py` – Helper for obtaining a user token via MSAL.
```

## Usage

Display the available commands:

```sh
uv run --with requests main.py --help
```

The examples depend on setting these variables

```sh
$side_car_url = "<url sidecar is running at>"
# Example values, use appropriate values for the token you want to request.
$token = uv run --with msal get_token.py --client-id "f79f9db9-c582-4b7b-9d4c-0e8fd40623f0" --authority "https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca" --scope "api://556d438d-2f4b-4add-9713-ede4e5f5d7da/access_as_user"
```

Example: validate an authorization header returned by `get_token.py`:

```sh
uv run --with requests main.py --base-url $side_car_url --authorization-header "Bearer $token" validate
```

Invoke a downstream API by name, supplying an override scope and a JSON payload stored in `body.json`:

```sh
uv run --with requests main.py --base-url $side_car_url --authorization-header "Bearer $token" --scope <scopes> invoke-downstream <api-name> --body-file <path-to-file>
```

Invoke a downstream API by name, use the credentials configured by the application:

```sh
uv run --with requests main.py --base-url=$side_car_url --agent-username=<username> --agent-identity=<agentUpn> invoke-downstream-unauth me
```

For client-credential flows, omit `--authorization-header` and use the unauthenticated commands such as `get-auth-header-unauth` or `invoke-downstream-unauth`.
