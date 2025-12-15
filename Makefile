
.PHONY: lint

lint:
	@echo "------------ RUFF ------------"
	@uv run ruff check
	@echo "---------- PYRIGHT -----------"
	@uv run pyright
	@echo "--------- PYDOCLINT ----------"
	@uv run pydoclint --quiet .; status=$$?; if [ $$status -eq 0 ]; then echo "No violations"; fi; exit $$status

