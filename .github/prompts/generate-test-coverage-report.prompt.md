---
description: "Run dotnet test with coverage collection and summarize which methods lack test coverage"
agent: "agent"
tools: [execute, read, search]
---
Run test coverage analysis for the GestionClubs solution and provide a summary of uncovered methods.

## Steps

1. Run tests with coverage collection:
   ```bash
   cd GestionClubs
   dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
   ```

2. If `reportgenerator` is not installed, install it:
   ```bash
   dotnet tool install -g dotnet-reportgenerator-globaltool
   ```

3. Generate a text summary report:
   ```bash
   reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./TestResults/Report" -reporttypes:"TextSummary;Cobertura"
   ```

4. Read the generated summary from `./TestResults/Report/Summary.txt`

5. Search the coverage XML for methods with `line-rate="0"` or low coverage

## Output Format

Provide a table:

| Service/Class | Method | Coverage % | Status |
|---------------|--------|-----------|--------|
| ClubServices | Method | 0% | 🔴 Uncovered |
| ... | ... | ... | ... |

Then list **top 5 priorities** — uncovered methods in Application services that handle business logic (skip trivial getters/setters).

End with a suggested `@test-writer` prompt for the highest-priority uncovered method.
