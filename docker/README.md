# Docker Test Images

This directory contains Dockerfiles for building test images to validate micasa across different Linux distributions.

## Building Images

Build all images:
```bash
make docker-build
```

Build a specific image:
```bash
make docker/.ubuntu24
```

List built images:
```bash
make docker-list
```

Clean build markers (forces rebuild):
```bash
make clean
```

## Using Test Images

After building, use the test script:
```bash
./scripts/docker-shell.sh ubuntu24
./scripts/docker-shell.sh amazonlinux2023
./scripts/docker-shell.sh debian12
```

## How It Works

- Each subdirectory contains a Dockerfile for a specific distribution
- Make uses marker files (`.ubuntu24`, etc.) to track whether images need rebuilding
- If the Dockerfile changes, Make will detect it and rebuild automatically
- Run `make clean` to force a complete rebuild of all images

