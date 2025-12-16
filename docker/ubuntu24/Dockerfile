FROM ubuntu:24.04

# Install Python, sudo, and other tools needed for testing
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    sudo

# Set Python 3 as the default python
RUN update-alternatives --install /usr/bin/python python /usr/bin/python3 1

# Conditionally set up Zscaler certificate if present
COPY tmp/setup-zscaler.sh tmp/ZscalerRootCombined.pem /tmp/
RUN chmod +x /tmp/setup-zscaler.sh && /tmp/setup-zscaler.sh

# Create micasa user with sudo privileges
RUN useradd -m -s /bin/bash micasa && \
    echo "micasa ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers

# Switch to micasa user
USER micasa
WORKDIR /home/micasa

# Create .config directory so Docker doesn't create it as root when mounting the manifest
RUN mkdir -p /home/micasa/.config

CMD ["/bin/bash"]
