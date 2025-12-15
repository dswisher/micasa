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
    echo "Usage: $0 <image-suffix>"
    echo ""
    echo "The image suffix is the last part of the test image name:"
    echo "    micasa-test-ubuntu24 -> ubuntu24"
    echo ""
    echo "Examples:"
    echo "    $0 amazonlinux2023"
    echo "    $0 ubuntu24"
    echo "    $0 debian12"
    echo "    $0 fedora"
    echo ""
    echo "To see a list of available images, do:"
    echo "    make docker-list"
    exit 1
fi

IMAGE_NAME="micasa-test-$1"
# Get the project root directory (parent of scripts directory)
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Check if the manifest file exists
MANIFEST_PATH="$HOME/.config/micasa/micasa.txt"
MANIFEST_VOLUME=""

if [ -f "$MANIFEST_PATH" ]; then
    MANIFEST_VOLUME="--volume $MANIFEST_PATH:/home/micasa/.config/micasa/micasa.txt:ro"
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

