# Debug Agent Prototype

> **IMPORTANT** The code is not meant to be production-ready. The intention of this prototype is to show the solution design. Technical aspects like handling or edge cases, proper error handling, logging etc. were omitted for sake of readability.

## Prerequisites

.NET must be installed.
lldb is installed an available in the command line.

## Running Application

1. Go to the application directory `DebugAgentPrototype/`
   ```bash
   cd DebugAgentPrototype
   ```
   
2. Set `OPENROUTER_API_KEY` in a `.env` file: make a copy of .env.sample and replace the placeholder. 
   ```bash
   cp .env.sample .env 
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

## Running Evals

1. Go to the application directory `Evals/`
   ```bash
   cd DebugAgentPrototype
   ```
   
2. Set `OPENROUTER_API_KEY` in a `.env` file: make a copy of .env.sample and replace the placeholder. 
   ```bash
   cp .env.sample .env 
   ```

3. Run evals:

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

Results of individual evals execution are written to `Evals/evals/evals/<eval-name>/result.md`.

