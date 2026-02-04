FROM amazonlinux:2023

# Install sudo and shadow-utils (provides useradd) for testing, as well
# as dependencies for .NET
RUN dnf install -y \
    ca-certificates \
    findutils \
    gzip \
    libicu \
    shadow-utils \
    sudo\
    tar

# Conditionally set up proxy certificates.
COPY tmp/config-proxy-certs.sh tmp/proxy-certs.pem /tmp/
RUN chmod +x /tmp/config-proxy-certs.sh && /tmp/config-proxy-certs.sh

# Install .NET 10 using the official install script from dotnet.microsoft.com
#   See https://learn.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#scripted-install
RUN curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh && \
    bash /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet && \
    rm /tmp/dotnet-install.sh

# Add .NET to PATH for all users.
ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH=$PATH:/usr/share/dotnet

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
