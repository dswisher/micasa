
COVERAGE_OPTS = /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

.PHONY: test watch report

test:
	dotnet test $(COVERAGE_OPTS)

watch:
	dotnet watch test $(COVERAGE_OPTS)

report:
	dotnet reportgenerator -reports:coverage.opencover.xml -targetdir:coveragereport

open:
	open coveragereport/index.html

