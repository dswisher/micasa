#!/bin/bash
# Conditionally set up Zscaler certificate if it's real (not a fake marker file)

if [ -f /tmp/ZscalerRootCombined.pem ] && ! grep -q "MICASA_FAKE_CERT" /tmp/ZscalerRootCombined.pem 2>/dev/null; then
    # Real certificate found - install into system CA bundle
    if [ -d /usr/local/share/ca-certificates ]; then
        cp /tmp/ZscalerRootCombined.pem /usr/local/share/ca-certificates/zscaler.crt
        update-ca-certificates
    fi
fi

# Clean up temporary files
rm -f /tmp/ZscalerRootCombined.pem /tmp/setup-zscaler.sh
