.PHONY: target build build-docs build-docs-api build-docs-website

release-docs:
	@echo "Rebuilding docs"
	rm -rf site api
	@echo "Updating website docs"
	poetry run mike deploy --push --update-aliases ${VERSION} ${ALIAS}
	@echo "Building API docs"
	@$(MAKE) build-docs-api

build-docs-api:
	poetry run pdoc --html --output-dir ./api/ ./aws_lambda_powertools --force
	mv -f ./api/aws_lambda_powertools/* ./api/
	rm -rf ./api/aws_lambda_powertools

docs-local:
	poetry run mkdocs serve

docs-local-docker:
	docker build -t squidfunk/mkdocs-material ./docs/
	docker run --rm -it -p 8000:8000 -v ${PWD}:/docs squidfunk/mkdocs-material

docs-api-local:
	poetry run pdoc --http : aws_lambda_powertools