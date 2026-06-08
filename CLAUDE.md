# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**ClawdToast** is a Windows application that integrates with Claude Code as a stop hook. When Claude Code finishes responding to a user, ClawdToast displays a Windows toast notification showing how long the response took (in Portuguese).

The application reads the Claude Code transcript file, extracts the turn duration from the JSON log entries, and displays a formatted duration message in a toast notification.

## Architecture

### Key Components

- **Program.cs**: Main entry point. Reads hook input from stdin, parses the transcript file, extracts turn duration, and displays the toast.
- **HookInput.cs**: Models the JSON input from the Claude Code harness stop hook.
- **TranscriptEntry.cs**: Models transcript entries from the Claude Code transcript file. Looks for entries with subtype `turn_duration` to extract duration metrics.
- **ClawdToastSettings.cs**: Manages application settings from `clawd-toast.settings.json`. Currently unused but initialized for future extensibility.
- **ClawdToastAppRegistry.cs**: Registers the application in Windows registry with an AppUserModelId for proper toast notification handling. Manages the app icon.
- **ClawdToastTrace.cs**: Conditional debug logging to `clawd-toast-debug.log` (DEBUG/TRACE builds only).
- **FileExtensions.cs**: Utility for reading files backward from end to start—used to efficiently find the most recent transcript entry without loading the entire file.

### Data Flow

1. Claude Code stop hook invokes ClawdToast with JSON input containing the transcript path
2. ClawdToast reads transcript file backward (most recent entries first)
3. Finds the first `turn_duration` entry with duration data
4. Formats duration into human-readable Portuguese (e.g., "2 horas, 30 minutos e 45 segundos")
5. Creates and displays a Windows toast notification with the duration

### Key Design Decisions

- **Backward file reading**: The `FileExtensions.ReadLinesBackward` method reads the transcript file backward to find the most recent entry without loading the entire file into memory. This is essential since transcripts can grow large.
- **Retry logic**: Built-in retries (5 attempts, 200ms delay) handle timing issues where the transcript may not be fully written when the hook executes.
- **AOT compilation**: Project uses `PublishAot` to enable ahead-of-time compilation for faster startup and smaller executable size.
- **Source-generated JSON**: Uses .NET 8+ source-generated JSON serialization (`JsonSerializerContext`) for performance.

## Building and Running

### Build the project

```powershell
dotnet build -c Release
```

### Run the compiled executable

The compiled executable is located at:
```
ClawdToast\bin\Release\net10.0-windows10.0.22621.0\win-x64\ClawdToast.exe
```

### Settings File

On first run, ClawdToast creates `clawd-toast.settings.json` next to the executable with default settings:
```json
{
  "min_duration_minutes": 2.0
}
```

## Integration with Claude Code

This application is designed to be used as a stop hook in Claude Code. Configure it in your Claude Code settings or hooks to execute when Claude Code finishes a turn. The harness will pass the transcript path via JSON input on stdin.

### Configuration Example

In Claude Code hooks/settings, the stop hook should invoke:
```
ClawdToast.exe
```

The harness automatically pipes the necessary JSON containing the transcript path.

## Debugging

Debug output is written to `clawd-toast-debug.log` in the same directory as the executable (DEBUG configuration only). Check this file if the application is not showing notifications as expected.

Common issues:
- Toast not displaying: Verify the application is registered in Windows App Notifications settings
- Duration not found: Check `clawd-toast-debug.log` for transcript parsing errors. The retry logic should handle most timing issues.
- Settings not loading: Verify `clawd-toast.settings.json` is valid JSON in the same directory as the executable

## Platform Requirements

- Windows 10 (build 22621) or later
- .NET 10.0 runtime (or self-contained AOT binary)
- x64 architecture
