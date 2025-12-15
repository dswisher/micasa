
.PHONY: lint docker-images docker-list clean

lint:
	@echo "------------ RUFF ------------"
	@uv run ruff check
	@echo "---------- PYRIGHT -----------"
	@uv run pyright
	@echo "--------- PYDOCLINT ----------"
	@uv run pydoclint --quiet .; status=$$?; if [ $$status -eq 0 ]; then echo "No violations"; fi; exit $$status

# Docker test images
docker-images: docker/.ubuntu24 docker/.ubuntu22 docker/.amazonlinux2023 docker/.debian12 docker/.fedora

docker/.ubuntu24: docker/ubuntu24/Dockerfile
	docker build -t micasa-test-ubuntu24 docker/ubuntu24
	@touch docker/.ubuntu24

docker/.ubuntu22: docker/ubuntu22/Dockerfile
	docker build -t micasa-test-ubuntu22 docker/ubuntu22
	@touch docker/.ubuntu22

docker/.amazonlinux2023: docker/amazonlinux2023/Dockerfile
	docker build -t micasa-test-amazonlinux2023 docker/amazonlinux2023
	@touch docker/.amazonlinux2023

docker/.debian12: docker/debian12/Dockerfile
	docker build -t micasa-test-debian12 docker/debian12
	@touch docker/.debian12

docker/.fedora: docker/fedora-latest/Dockerfile
	docker build -t micasa-test-fedora docker/fedora-latest
	@touch docker/.fedora

docker-list:
	@echo "Available micasa test images:"
	@docker images --filter "reference=micasa-test-*" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"

clean:
	rm -f docker/.ubuntu24 docker/.ubuntu22 docker/.amazonlinux2023 docker/.debian12 docker/.fedora

