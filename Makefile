
.PHONY: all lint docker-images docker-list clean

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

all:
	@echo "Specify a target, like 'lint', 'docker-images', 'docker-list' or 'clean'."


lint:
	@echo "------------ RUFF ------------"
	@uv run ruff check
	@echo "---------- PYRIGHT -----------"
	@uv run pyright
	@echo "--------- PYDOCLINT ----------"
	@uv run pydoclint --quiet .; status=$$?; if [ $$status -eq 0 ]; then echo "No violations"; fi; exit $$status

docker-images: docker/.ubuntu24 docker/.ubuntu22 docker/.amazonlinux2023 docker/.debian12


docker-list:
	@echo "Available micasa test images:"
	@docker images --filter "reference=micasa-test-*" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"


clean:
	rm -f docker/.ubuntu24 docker/.ubuntu22 docker/.amazonlinux2023 docker/.debian12
	rm -rf docker/ubuntu24/tmp docker/ubuntu22/tmp docker/amazonlinux2023/tmp docker/debian12/tmp


# - - - - - - - INTERNAL FILE TARGETS - - - - - - - -

docker/.ubuntu24: docker/ubuntu24/Dockerfile
	$(call prepare-zscaler-cert,docker/ubuntu24)
	docker build -t micasa-test-ubuntu24 docker/ubuntu24
	@touch docker/.ubuntu24

docker/.ubuntu22: docker/ubuntu22/Dockerfile
	$(call prepare-zscaler-cert,docker/ubuntu22)
	docker build -t micasa-test-ubuntu22 docker/ubuntu22
	@touch docker/.ubuntu22

docker/.amazonlinux2023: docker/amazonlinux2023/Dockerfile
	$(call prepare-zscaler-cert,docker/amazonlinux2023)
	docker build -t micasa-test-amazonlinux2023 docker/amazonlinux2023
	@touch docker/.amazonlinux2023

docker/.debian12: docker/debian12/Dockerfile
	$(call prepare-zscaler-cert,docker/debian12)
	docker build -t micasa-test-debian12 docker/debian12
	@touch docker/.debian12

