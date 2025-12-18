MARKER_DIR = docker/.markers

.PHONY: all docker-images docker-list clean help ubuntu24 ubuntu22 debian12 amazon23 builders runners

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
	@echo "  ubuntu22       - Build micasa binary for Ubuntu 22.04"
	@echo "  debian12       - Build micasa binary for Debian 12"
	@echo "  amazon23       - Build micasa binary for Amazon Linux 2023"
	@echo "  clean          - Remove Docker build markers and build artifacts"

docker-images: builders runners

runners: 	$(MARKER_DIR)/runner-ubuntu24 \
			$(MARKER_DIR)/runner-ubuntu22 \
			$(MARKER_DIR)/runner-debian12 \
			$(MARKER_DIR)/runner-amazon23

builders: 	$(MARKER_DIR)/builder-ubuntu24 \
			$(MARKER_DIR)/builder-ubuntu22 \
			$(MARKER_DIR)/builder-debian12 \
			$(MARKER_DIR)/builder-amazon23

docker-list:
	@echo "Available micasa builder images:"
	@docker images --filter "reference=micasa-builder-*" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"
	@echo ""
	@echo "Available micasa runner images:"
	@docker images --filter "reference=micasa-runner-*" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"

ubuntu24: $(MARKER_DIR)/builder-ubuntu24 $(MARKER_DIR)/runner-ubuntu24
	@echo "Building micasa for Ubuntu 24.04..."
	@mkdir -p target/docker-ubuntu24
	@docker run \
		--rm \
		--volume "$(CURDIR):/workspace:ro" \
		--volume "$(CURDIR)/target/docker-ubuntu24:/output" \
		micasa-builder-ubuntu24 \
		bash -c "cargo build --manifest-path=/workspace/Cargo.toml --target-dir=/output && cp /output/debug/micasa /output/micasa"
	@echo "Binary built: target/docker-ubuntu24/micasa"

ubuntu22: $(MARKER_DIR)/builder-ubuntu22 $(MARKER_DIR)/runner-ubuntu22
	@echo "Building micasa for Ubuntu 22.04..."
	@mkdir -p target/docker-ubuntu22
	@docker run \
		--rm \
		--volume "$(CURDIR):/workspace:ro" \
		--volume "$(CURDIR)/target/docker-ubuntu22:/output" \
		micasa-builder-ubuntu22 \
		bash -c "cargo build --manifest-path=/workspace/Cargo.toml --target-dir=/output && cp /output/debug/micasa /output/micasa"
	@echo "Binary built: target/docker-ubuntu22/micasa"

debian12: $(MARKER_DIR)/builder-debian12 $(MARKER_DIR)/runner-debian12
	@echo "Building micasa for Debian 12..."
	@mkdir -p target/docker-debian12
	@docker run \
		--rm \
		--volume "$(CURDIR):/workspace:ro" \
		--volume "$(CURDIR)/target/docker-debian12:/output" \
		micasa-builder-debian12 \
		bash -c "cargo build --manifest-path=/workspace/Cargo.toml --target-dir=/output && cp /output/debug/micasa /output/micasa"
	@echo "Binary built: target/docker-debian12/micasa"

amazon23: $(MARKER_DIR)/builder-amazon23 $(MARKER_DIR)/runner-amazon23
	@echo "Building micasa for Amazon Linux 2023..."
	@mkdir -p target/docker-amazon23
	@docker run \
		--rm \
		--volume "$(CURDIR):/workspace:ro" \
		--volume "$(CURDIR)/target/docker-amazon23:/output" \
		micasa-builder-amazon23 \
		bash -c "cargo build --manifest-path=/workspace/Cargo.toml --target-dir=/output && cp /output/debug/micasa /output/micasa"
	@echo "Binary built: target/docker-amazon23/micasa"

clean:
	rm -rf $(MARKER_DIR)
	rm -rf docker/builder/ubuntu24/tmp docker/runner/ubuntu24/tmp
	rm -rf docker/builder/ubuntu22/tmp docker/runner/ubuntu22/tmp
	rm -rf docker/builder/debian12/tmp docker/runner/debian12/tmp
	rm -rf docker/builder/amazon23/tmp docker/runner/amazon23/tmp
	rm -rf target/docker-ubuntu24
	rm -rf target/docker-ubuntu22
	rm -rf target/docker-debian12
	rm -rf target/docker-amazon23

# - - - - - - - INTERNAL FILE TARGETS - - - - - - - -

$(MARKER_DIR):
	mkdir $(MARKER_DIR)

$(MARKER_DIR)/builder-ubuntu24: $(MARKER_DIR) docker/builder/ubuntu24/Dockerfile
	$(call prepare-zscaler-cert,docker/builder/ubuntu24)
	docker build -t micasa-builder-ubuntu24 docker/builder/ubuntu24
	@touch $(MARKER_DIR)/builder-ubuntu24

$(MARKER_DIR)/runner-ubuntu24: $(MARKER_DIR) docker/runner/ubuntu24/Dockerfile
	$(call prepare-zscaler-cert,docker/runner/ubuntu24)
	docker build -t micasa-runner-ubuntu24 docker/runner/ubuntu24
	@touch $(MARKER_DIR)/runner-ubuntu24

$(MARKER_DIR)/builder-ubuntu22: $(MARKER_DIR) docker/builder/ubuntu22/Dockerfile
	$(call prepare-zscaler-cert,docker/builder/ubuntu22)
	docker build -t micasa-builder-ubuntu22 docker/builder/ubuntu22
	@touch $(MARKER_DIR)/builder-ubuntu22

$(MARKER_DIR)/runner-ubuntu22: $(MARKER_DIR) docker/runner/ubuntu22/Dockerfile
	$(call prepare-zscaler-cert,docker/runner/ubuntu22)
	docker build -t micasa-runner-ubuntu22 docker/runner/ubuntu22
	@touch $(MARKER_DIR)/runner-ubuntu22

$(MARKER_DIR)/builder-debian12: $(MARKER_DIR) docker/builder/debian12/Dockerfile
	$(call prepare-zscaler-cert,docker/builder/debian12)
	docker build -t micasa-builder-debian12 docker/builder/debian12
	@touch $(MARKER_DIR)/builder-debian12

$(MARKER_DIR)/runner-debian12: $(MARKER_DIR) docker/runner/debian12/Dockerfile
	$(call prepare-zscaler-cert,docker/runner/debian12)
	docker build -t micasa-runner-debian12 docker/runner/debian12
	@touch $(MARKER_DIR)/runner-debian12

$(MARKER_DIR)/builder-amazon23: $(MARKER_DIR) docker/builder/amazon23/Dockerfile
	$(call prepare-zscaler-cert,docker/builder/amazon23)
	docker build -t micasa-builder-amazon23 docker/builder/amazon23
	@touch $(MARKER_DIR)/builder-amazon23

$(MARKER_DIR)/runner-amazon23: $(MARKER_DIR) docker/runner/amazon23/Dockerfile
	$(call prepare-zscaler-cert,docker/runner/amazon23)
	docker build -t micasa-runner-amazon23 docker/runner/amazon23
	@touch $(MARKER_DIR)/runner-amazon23

