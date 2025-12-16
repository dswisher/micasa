#!/bin/bash
# Conditionally set up Zscaler certificate if it's real (not a fake marker file)

if [ -f /tmp/ZscalerRootCombined.pem ] && ! grep -q "MICASA_FAKE_CERT" /tmp/ZscalerRootCombined.pem 2>/dev/null; then
    # Real certificate found - install it
    mkdir -p /opt/newscorp/zscaler
    cp /tmp/ZscalerRootCombined.pem /opt/newscorp/zscaler/

    # Set CURL_CA_BUNDLE in multiple locations to cover all shell scenarios

    # For login shells (sh, bash, zsh) - via /etc/profile.d/
    mkdir -p /etc/profile.d
    echo 'export CURL_CA_BUNDLE=/opt/newscorp/zscaler/ZscalerRootCombined.pem' >> /etc/profile.d/zscaler.sh
    chmod 644 /etc/profile.d/zscaler.sh

    # For bash non-login interactive shells (docker exec bash)
    if [ -f /etc/bash.bashrc ]; then
        echo 'export CURL_CA_BUNDLE=/opt/newscorp/zscaler/ZscalerRootCombined.pem' >> /etc/bash.bashrc
    fi

    # For non-interactive processes
    echo 'CURL_CA_BUNDLE=/opt/newscorp/zscaler/ZscalerRootCombined.pem' >> /etc/environment
fi

# Clean up temporary files
rm -f /tmp/ZscalerRootCombined.pem /tmp/setup-zscaler.sh
