FROM amazonlinux:2023

# Amazon Linux 2023 already includes Python3 and bash
# Install sudo and shadow-utils (provides useradd) for testing
RUN dnf install -y sudo shadow-utils

# Create micasa user with sudo privileges
RUN useradd -m -s /bin/bash micasa && \
    echo "micasa ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers

# Switch to micasa user
USER micasa
WORKDIR /home/micasa

CMD ["/bin/bash"]
