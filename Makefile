.PHONY: build-docs build-docs-website docs-local docs-local-docker

build-docs:
	@$(MAKE) build-docs-website

build-docs-website:
	mkdir -p dist
	docker build -t squidfunk/mkdocs-material ./docs/
	docker run --rm -t -v ${PWD}:/docs squidfunk/mkdocs-material build
	cp -R site/* dist/

docs-local:
	poetry run mkdocs serve

docs-local-docker:
	docker build -t squidfunk/mkdocs-material ./docs/
	docker run --rm -it -p 8000:8000 -v ${PWD}:/docs squidfunk/mkdocs-material

changelog:
	git fetch --tags origin
	CURRENT_VERSION=$(shell git describe --abbrev=0 --tag) ;\
	echo "[+] Pre-generating CHANGELOG for tag: $$CURRENT_VERSION" ;\
	docker run -v "${PWD}":/workdir quay.io/git-chglog/git-chglog:0.15.4 > CHANGELOG.md