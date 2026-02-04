#!/bin/bash

# This is run inside the docker container to set up SSL root certs to work with a proxy.
# Since docker build does not have a "conditional copy", we always copy a cert file, but
# it may be a "fake" file that we need to ignore.

if [ -f /tmp/proxy-certs.pem ] && ! grep -q "FAKE_CERT" /tmp/proxy-certs.pem 2>/dev/null; then

    # Real certificate found - install it
    echo "Installing proxy certificates..."

    # Detect the OS type
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        OS_ID="$ID"
    else
        echo "Warning: Cannot detect OS type, skipping certificate installation"
        exit 1
    fi

    # Install certificates based on OS type
    case "$OS_ID" in
        ubuntu|debian)
            # Debian/Ubuntu: use update-ca-certificates
            echo "Detected Debian/Ubuntu - using update-ca-certificates"
            cp /tmp/proxy-certs.pem /usr/local/share/ca-certificates/proxy-certs.crt
            update-ca-certificates
            ;;
        amzn|rhel|centos|fedora)
            # Amazon Linux/RHEL/CentOS/Fedora: use update-ca-trust
            echo "Detected $OS_ID - using update-ca-trust"
            cp /tmp/proxy-certs.pem /etc/pki/ca-trust/source/anchors/proxy-certs.pem
            update-ca-trust extract
            ;;
        *)
            echo "Warning: Unsupported OS type '$OS_ID', skipping certificate installation"
            exit 1
            ;;
    esac

    echo "Proxy certificates installed successfully"
fi

# Clean up temporary files
rm -f /tmp/proxy-certs.pem /tmp/config-proxy-certs.sh
