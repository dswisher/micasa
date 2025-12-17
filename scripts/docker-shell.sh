#!/usr/bin/env bash
#
# Launch a Docker container with the micasa binary mounted.
#
# Usage: ./scripts/docker-shell.sh <os-name>
#
# Example: ./scripts/docker-shell.sh ubuntu24
#
# Note: Build the binary first using: make <os-name>

set -e

if [ $# -eq 0 ]; then
    echo "Error: No OS name provided"
    echo ""
    echo "Usage: $0 <os-name>"
    echo ""
    echo "Examples:"
    echo "    $0 ubuntu24"
    echo ""
    echo "To see a list of available images, do:"
    echo "    make docker-list"
    exit 1
fi

OS_NAME="$1"
RUNNER_IMAGE="micasa-runner-$OS_NAME"

# Get the project root directory (parent of scripts directory)
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Path to the binary
BINARY_PATH="$PROJECT_ROOT/target/docker-$OS_NAME/micasa"

if [ ! -f "$BINARY_PATH" ]; then
    echo "Error: Binary not found at $BINARY_PATH"
    echo ""
    echo "Please build the binary first:"
    echo "    make $OS_NAME"
    exit 1
fi

echo "Starting runner container: $RUNNER_IMAGE"
echo "Mounting: $BINARY_PATH -> /usr/local/bin/micasa (read-only)"
echo ""

docker run \
    --interactive \
    --tty \
    --rm \
    --volume "$BINARY_PATH:/usr/local/bin/micasa:ro" \
    "$RUNNER_IMAGE" \
    /bin/bash
