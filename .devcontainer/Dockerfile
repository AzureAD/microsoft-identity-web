# See here for image contents: https://github.com/devcontainers/images/blob/main/src/dotnet/.devcontainer/Dockerfile
# Example of using a preview version: mcr.microsoft.com/devcontainers/dotnet:10.0-preview

FROM mcr.microsoft.com/vscode/devcontainers/dotnet:latest

# [Option] Install Node.js
ARG INSTALL_NODE="true"
ARG NODE_VERSION="lts/*"
RUN if [ "${INSTALL_NODE}" = "true" ]; then su vscode -c "umask 0002 && . /usr/local/share/nvm/nvm.sh && nvm install ${NODE_VERSION} 2>&1"; fi