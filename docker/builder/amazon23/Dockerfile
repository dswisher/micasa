FROM amazonlinux:2023

# Install ca-certificates first (needed for dnf to work with HTTPS)
RUN dnf install -y ca-certificates

# Conditionally set up Zscaler certificate if present
COPY tmp/setup-zscaler.sh tmp/ZscalerRootCombined.pem /tmp/
RUN chmod +x /tmp/setup-zscaler.sh && /tmp/setup-zscaler.sh

# Now install build tools (curl-minimal is already installed)
RUN dnf install -y \
    gcc \
    gcc-c++ \
    make

# Install Rust (system CA bundle includes Zscaler cert if present)
RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y && \
    /root/.cargo/bin/rustc --version
ENV PATH="/root/.cargo/bin:${PATH}"

WORKDIR /workspace
