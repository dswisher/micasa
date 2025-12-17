FROM ubuntu:24.04

# Keep image clean - only install minimal tools for interactive testing
# NOTE: Intentionally NOT installing curl or other tools by default
# to test that micasa can handle installing its prerequisites
RUN apt-get update && apt-get install -y \
    sudo \
    vim

# Conditionally set up Zscaler certificate if present
COPY tmp/setup-zscaler.sh tmp/ZscalerRootCombined.pem /tmp/
RUN chmod +x /tmp/setup-zscaler.sh && /tmp/setup-zscaler.sh

# Create micasa user with sudo privileges
RUN useradd -m -s /bin/bash micasa && \
    echo "micasa ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers

# Switch to micasa user
USER micasa
WORKDIR /home/micasa

# Create .config directory so Docker doesn't create it as root when mounting
RUN mkdir -p /home/micasa/.config

CMD ["/bin/bash"]
