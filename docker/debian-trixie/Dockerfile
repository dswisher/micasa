# aka debian 13
FROM debian:trixie

# Install Python, sudo, and other tools needed for testing
RUN apt-get update && apt-get install -y \
    sudo \
    wget \
    ca-certificates

# Conditionally set up proxy certificates.
COPY tmp/config-proxy-certs.sh tmp/proxy-certs.pem /tmp/
RUN chmod +x /tmp/config-proxy-certs.sh && /tmp/config-proxy-certs.sh

# Add Microsoft package repository and install .NET 10 SDK
RUN wget https://packages.microsoft.com/config/debian/13/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-sdk-10.0

# Create micasa user with sudo privileges
RUN useradd -m -s /bin/bash micasa && \
    echo "micasa ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers

# Switch to micasa user
USER micasa
WORKDIR /home/micasa

# Trigger the first-run welcome message during build
RUN dotnet help > /dev/null

# Add ~/.local/bin to PATH for micasa user
ENV PATH=$PATH:/home/micasa/.local/bin

# Run bash
CMD ["/bin/bash"]
