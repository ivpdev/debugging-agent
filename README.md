# Debug Agent Prototype

## Prerequisites

### Installing lldb

**On macOS:**
lldb comes with Xcode Command Line Tools. Install or verify installation:
```bash
xcode-select --install
```

Verify installation:
```bash
lldb --version
```

**On Linux:**
```bash
# Ubuntu/Debian
sudo apt-get install lldb

# Fedora/RHEL
sudo dnf install lldb
```

**On Windows:**
lldb is included with Visual Studio, or you can install the LLVM toolchain separately.

## Running Application

1. Set `OPENROUTER_API_KEY` in a `.env` file. The application will look for `.env` in the following locations (in order):
   - `DebugAgentPrototype/` directory (project root)
   - Workspace root (solution root)
   - Current working directory (where you run the command from)
   - Executable directory (`bin/Debug/net9.0/`)

   Create a `.env` file with:
   ```
   OPENROUTER_API_KEY=your_api_key_here
   ```

2. Run the application from the `DebugAgentPrototype/` directory:
   ```bash
   cd DebugAgentPrototype
   dotnet run
   ```

## Running Evals

1. Set `OPENROUTER_API_KEY` in a `.env` file. The evals program will look for `.env` in the following locations (in order):
   - `Evals/` directory (project root)
   - `Evals/bin/DebugAgentPrototype/.env` (2 levels up from executable, then into DebugAgentPrototype)
   - Workspace root (solution root)
   - Current working directory (where you run the command from)
   - `Current working directory/DebugAgentPrototype/.env`
   - Executable directory (`bin/Debug/net9.0/`)

   Create a `.env` file with:
   ```
   OPENROUTER_API_KEY=your_api_key_here
   ```

2. Run evals from the `Evals/` directory:

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

Results are written to `Evals/evals/evals/<eval-name>/result.md` and `conversation-for-evaluation.json`.

