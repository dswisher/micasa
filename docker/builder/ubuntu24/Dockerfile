FROM ubuntu:24.04

# Install build tools and ca-certificates
RUN apt-get update && apt-get install -y \
    ca-certificates \
    curl \
    build-essential

# Conditionally set up Zscaler certificate if present
COPY tmp/setup-zscaler.sh tmp/ZscalerRootCombined.pem /tmp/
RUN chmod +x /tmp/setup-zscaler.sh && /tmp/setup-zscaler.sh

# Install Rust (system CA bundle includes Zscaler cert if present)
RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y && \
    /root/.cargo/bin/rustc --version
ENV PATH="/root/.cargo/bin:${PATH}"

WORKDIR /workspace
