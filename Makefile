.PHONY: coverage
coverage:
	rm -rf TestResults/Temp
	dotnet test --collect:'XPlat Code Coverage' --results-directory TestResults/Temp
	reportgenerator \
		-reports:"TestResults/Temp/*/coverage.cobertura.xml"  \
		-targetdir:".coverage"                                \
		-filefilters:-*.g.cs                                  \
		-historydir:".coverage/history"                       \
		'-reporttypes:Html_Dark;MarkdownSummaryGithub'

.PHONY: mdtohtml
mdtohtml:
	go install github.com/gomarkdown/mdtohtml@latest
	mdtohtml -page README.md AzureKeyVaultEmulator/index.html
