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
$token = uv run --with msal get_token.py --client-id "9808c2f0-4555-4dc2-beea-b4dc3212d39e" --authority "https://login.microsoftonline.com/10c419d4-4a50-45b2-aa4e-919fb84df24f" --scope "api://a021aff4-57ad-453a-bae8-e4192e5860f3/access_as_user"
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
