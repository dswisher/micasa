#!/usr/bin/env bash
#
# Launch a Docker container with the micasa source code mounted.
# Usage: ./scripts/docker-shell.sh <image-name>
#
# Example: ./scripts/docker-shell.sh amazonlinux:2023

set -e

if [ $# -eq 0 ]; then
    echo "Error: No image name provided"
    echo ""
    echo "Usage: $0 <image-name>"
    echo ""
    echo "Examples:"
    echo "  $0 amazonlinux:2023"
    echo "  $0 ubuntu:24.04"
    echo "  $0 debian:bookworm"
    echo "  $0 fedora:latest"
    exit 1
fi

IMAGE_NAME="$1"
# Get the project root directory (parent of scripts directory)
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Check if the manifest file exists
MANIFEST_PATH="$HOME/.config/micasa/micasa.txt"
MANIFEST_VOLUME=""

if [ -f "$MANIFEST_PATH" ]; then
    MANIFEST_VOLUME="--volume $MANIFEST_PATH:/root/.config/micasa/micasa.txt:ro"
    MANIFEST_STATUS="yes (read-only)"
else
    MANIFEST_STATUS="no (file not found at $MANIFEST_PATH)"
fi

echo "Starting container from image: $IMAGE_NAME"
echo "Mounting: $PROJECT_ROOT -> /workspace"
echo "Mounting manifest: $MANIFEST_STATUS"
echo ""

docker run \
    --interactive \
    --tty \
    --rm \
    --volume "$PROJECT_ROOT:/workspace" \
    $MANIFEST_VOLUME \
    --workdir /workspace \
    "$IMAGE_NAME" \
    /bin/bash
