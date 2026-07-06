#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
ENV_FILE="${KORAXLIB_ENV_FILE:-${ROOT_DIR}/.env}"

if [[ -f "${ENV_FILE}" ]]; then
    set -a
    # shellcheck disable=SC1090
    source "${ENV_FILE}"
    set +a
fi

STS2_DIR="${STS2_DIR:-/home/aspharos/.local/share/Steam/steamapps/common/Slay the Spire 2}"
STS2_APP_ID="${STS2_APP_ID:-2868840}"
STS2_RENDERER="${STS2_RENDERER:-opengl}"
KORAXLIB_ENABLE_SMOKE_CONTENT="${KORAXLIB_ENABLE_SMOKE_CONTENT:-1}"

case "${STS2_RENDERER}" in
    opengl)
        LAUNCH_SCRIPT="launch_opengl.sh"
        ;;
    vulkan)
        LAUNCH_SCRIPT="launch_vulkan.sh"
        ;;
    *)
        printf 'Unsupported STS2_RENDERER=%s. Use opengl or vulkan.\n' "${STS2_RENDERER}" >&2
        exit 2
        ;;
esac

if ! command -v steam-run >/dev/null 2>&1; then
    printf 'steam-run was not found. On NixOS, install or enter a shell that provides steam-run.\n' >&2
    exit 127
fi

if [[ ! -x "${STS2_DIR}/${LAUNCH_SCRIPT}" ]]; then
    printf 'STS2 launch script not found or not executable: %s\n' "${STS2_DIR}/${LAUNCH_SCRIPT}" >&2
    printf 'Set STS2_DIR in %s if your Steam library path is different.\n' "${ENV_FILE}" >&2
    exit 1
fi

printf 'Launching STS2 KoraxLib smoke test: renderer=%s smoke=%s\n' \
    "${STS2_RENDERER}" "${KORAXLIB_ENABLE_SMOKE_CONTENT}"

cd "${STS2_DIR}"
env \
    "SteamAppId=${STS2_APP_ID}" \
    "SteamGameId=${STS2_APP_ID}" \
    "KORAXLIB_ENABLE_SMOKE_CONTENT=${KORAXLIB_ENABLE_SMOKE_CONTENT}" \
    steam-run bash "./${LAUNCH_SCRIPT}" "$@"
