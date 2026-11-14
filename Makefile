.PHONY: build-docs build-docs-website docs-local docs-venv changelog

DOCS_VENV ?= .venv-docs

docs-venv:
	python3 -m venv $(DOCS_VENV)
	$(DOCS_VENV)/bin/pip install --require-hashes --only-binary :all: -r docs/requirements.txt

build-docs:
	@$(MAKE) build-docs-website

build-docs-website: docs-venv
	mkdir -p dist
	$(DOCS_VENV)/bin/mkdocs build
	cp -R site/* dist/

docs-local: docs-venv
	$(DOCS_VENV)/bin/mkdocs serve

changelog:
	git fetch --tags origin
	CURRENT_VERSION=$(shell git describe --abbrev=0 --tag) ;\
	echo "[+] Pre-generating CHANGELOG for tag: $$CURRENT_VERSION" ;\
	docker run -v "${PWD}":/workdir quay.io/git-chglog/git-chglog:0.15.4 > CHANGELOG.md
