#!/bin/bash

# Check for ZScaler root certs
if [ -f /opt/newscorp/zscaler/ZscalerRootCombined.pem ]; then
    cp /opt/newscorp/zscaler/ZscalerRootCombined.pem tmp/proxy-certs.pem
else
    echo "FAKE_CERT" > tmp/proxy-certs.pem
fi
