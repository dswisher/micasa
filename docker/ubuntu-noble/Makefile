NAME=ubuntu-noble

all: .docker-image

.docker-image: Dockerfile $(FILES)
	docker build --tag $(NAME) .
	touch $@

run: .docker-image
	docker run \
		--rm \
		--interactive \
		--tty \
		--name $(NAME) \
		$(NAME)
