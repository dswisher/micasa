.PHONY: all docker-images docker-list clean help ubuntu24

# Function to prepare Zscaler cert for docker build
# Usage: $(call prepare-zscaler-cert,docker-dir)
define prepare-zscaler-cert
	@mkdir -p $(1)/tmp
	@cp docker/setup-zscaler.sh $(1)/tmp/
	@if [ -f /opt/newscorp/zscaler/ZscalerRootCombined.pem ]; then \
		cp /opt/newscorp/zscaler/ZscalerRootCombined.pem $(1)/tmp/ZscalerRootCombined.pem; \
	else \
		echo "MICASA_FAKE_CERT" > $(1)/tmp/ZscalerRootCombined.pem; \
	fi
endef

# - - - - - - - USER TARGETS - - - - - - - -

all: help

help:
	@echo "Available targets:"
	@echo "  docker-images  - Build all Docker images (builders and runners)"
	@echo "  docker-list    - List available Docker images"
	@echo "  ubuntu24       - Build micasa binary for Ubuntu 24.04"
	@echo "  clean          - Remove Docker build markers and build artifacts"

docker-images: docker/.builder-ubuntu24 docker/.runner-ubuntu24

docker-list:
	@echo "Available micasa builder images:"
	@docker images --filter "reference=micasa-builder-*" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"
	@echo ""
	@echo "Available micasa runner images:"
	@docker images --filter "reference=micasa-runner-*" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"

ubuntu24: docker/.builder-ubuntu24
	@echo "Building micasa for Ubuntu 24.04..."
	@mkdir -p target/docker-ubuntu24
	@docker run \
		--rm \
		--volume "$(CURDIR):/workspace:ro" \
		--volume "$(CURDIR)/target/docker-ubuntu24:/output" \
		micasa-builder-ubuntu24 \
		bash -c "cargo build --manifest-path=/workspace/Cargo.toml --target-dir=/output && cp /output/debug/micasa /output/micasa"
	@echo "Binary built: target/docker-ubuntu24/micasa"

clean:
	rm -f docker/.builder-ubuntu24 docker/.runner-ubuntu24
	rm -rf docker/builder/ubuntu24/tmp docker/runner/ubuntu24/tmp
	rm -rf target/docker-ubuntu24

# - - - - - - - INTERNAL FILE TARGETS - - - - - - - -

docker/.builder-ubuntu24: docker/builder/ubuntu24/Dockerfile
	$(call prepare-zscaler-cert,docker/builder/ubuntu24)
	docker build -t micasa-builder-ubuntu24 docker/builder/ubuntu24
	@touch docker/.builder-ubuntu24

docker/.runner-ubuntu24: docker/runner/ubuntu24/Dockerfile
	$(call prepare-zscaler-cert,docker/runner/ubuntu24)
	docker build -t micasa-runner-ubuntu24 docker/runner/ubuntu24
	@touch docker/.runner-ubuntu24
