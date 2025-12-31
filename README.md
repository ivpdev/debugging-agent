# Running Evals

## Prerequisites

Set `OPENROUTER_API_KEY` in a `.env` file (in the project root or `DebugAgentPrototype/` directory).

## Running Evals

From the `Evals/` directory:

```bash
# Run all evals
dotnet run

# Run a specific eval by name
dotnet run <eval-name>
```

Example:
```bash
cd Evals
dotnet run set-breakpoint
```

Results are written to `Evals/evals/evals/<eval-name>/result.md` and `chat-history.json`.

