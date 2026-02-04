FROM ubuntu:24.04

# Install the things we need to allow the app to run
RUN apt-get update && apt-get install -y \
    sudo \
    wget \
    ca-certificates

# Install .NET 10 SDK and show the intro message
RUN wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-sdk-10.0 && \
    dotnet help

# Create micasa user with sudo privileges
RUN useradd -m -s /bin/bash micasa && \
    echo "micasa ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers

# Switch to micasa user
USER micasa
WORKDIR /home/micasa

CMD ["/bin/bash"]
