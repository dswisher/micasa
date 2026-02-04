
.PHONY: all run clean

all: .docker-image

.docker-image: Dockerfile tmp/config-proxy-certs.sh tmp/proxy-certs.pem
	docker build --tag $(NAME) .
	touch $@

run: .docker-image
	docker run \
		--rm \
		--interactive \
		--tty \
		--name $(NAME) \
		--volume ../../src:/home/micasa/src \
		$(NAME) || true

clean:
	rm -f .docker-image
	rm -rf tmp

tmp/proxy-certs.pem: ../prep-proxy-certs.sh
	../prep-proxy-certs.sh

tmp/config-proxy-certs.sh: tmp ../config-proxy-certs.sh
	cp ../config-proxy-certs.sh tmp/config-proxy-certs.sh

tmp:
	mkdir tmp

