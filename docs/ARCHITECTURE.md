# DSCode Architecture

This document explains the internal design of DSCode, a Windows Forms application that bridges DeepSeek Chat with local development tools.

## High‑Level Overview

DSCode embeds a WebView2 control pointing to `https://chat.deepseek.com`. It injects JavaScript into the page that:
- Scans the AI's responses for XML tool call blocks.
- Sends those tool calls to the C# host via `window.chrome.webview.postMessage`.
- Receives the tool execution results and injects them back into the chat input, then clicks send.

The C# side consists of a main form (`FormMain`), a bridge service (`BrowserBridge`), a tool execution engine (`ToolSystem`), and supporting services for Git, settings, and theming.

## Component Diagram (Text)
<svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 12 12" fill="none" class="_9bc997d _33882ae"><path d="M-5.24537e-07 0C-2.34843e-07 6.62742 5.37258 12 12 12L0 12L-5.24537e-07 0Z" fill="currentColor"></path></svg><svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 12 12" fill="none" class="_9bc997d _28d7e84"><path d="M-5.24537e-07 0C-2.34843e-07 6.62742 5.37258 12 12 12L0 12L-5.24537e-07 0Z" fill="currentColor"></path></svg></div><p class="ds-markdown-paragraph">┌─────────────────────────────────────────────────────────────┐<br>│                         FormMain (WinForms)                 │<br>│  ┌──────────┬───────────────────────────┬────────────────┐ │<br>│  │ Toolbar  │   SplitContainer           │  Sidebar       │ │<br>│  │ (buttons,│  ┌──────────┬───────────┐  │ (File Tree)    │ │<br>│  │  SQL txt)│  │ WebView2  │ File      │  │ + checkboxes   │ │<br>│  │          │  │ (DeepSeek)│ Viewer    │  └────────────────┘ │<br>│  └──────────┘  └──────────┴───────────┘                      │<br>│                ┌──────────────────────────────┐              │<br>│                │ Log Panel (RichTextBox)       │              │<br>│                └──────────────────────────────┘              │<br>└─────────────────────────────────────────────────────────────┘<br>│                            ▲<br>│ (events, tool calls)       │ (tool results)<br>▼                            │<br>┌─────────────────────────────────────────────────────────────┐<br>│                    BrowserBridge                             │<br>│ - Injects automation script into WebView2                    │<br>│ - Handles WebMessageReceived (tool calls)                    │<br>│ - Executes tools via ToolSystem                              │<br>│ - Returns results via ExecuteScriptAsync (injectToolResult)  │<br>└─────────────────────────────────────────────────────────────┘<br>│<br>▼<br>┌─────────────────────────────────────────────────────────────┐<br>│                      ToolSystem                              │<br>│ - Implements all 9 tools (file ops, commands, SQL, etc.)    │<br>│ - Uses absolute paths (combines workspace + relative path)  │<br>│ - RunCommandAsync supports cancellation & streaming output  │<br>└─────────────────────────────────────────────────────────────┘<br>│<br>├──► GitService (git status, commit+push)<br>├──► SettingsManager (persist last workspace)<br>└──► Theme (static colors/fonts)</p><div class="md-code-block md-code-block-light"><div class="md-code-block-banner-wrap"><div class="md-code-block-banner md-code-block-banner-lite"><div class="_121d384"><div class="d2a24f03">text</div><div class="d2a24f03 _246a029"><div class="efa13877"><button role="button" aria-disabled="true" class="ds-atom-button ds-atom-button--disabled ds-text-button ds-text-button--with-icon ds-text-button--disabled" style="margin-right: 4px;" disabled=""><div class="ds-icon ds-atom-button__icon" style="font-size: 16px; width: 16px; height: 16px; margin-right: 3px;"><svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M6.14929 4.02032C7.11197 4.02032 7.87983 4.02016 8.49597 4.07598C9.12128 4.13269 9.65792 4.25188 10.1415 4.53106C10.7202 4.8653 11.2008 5.3459 11.535 5.92462C11.8142 6.40818 11.9334 6.94481 11.9901 7.57012C12.0459 8.18625 12.0458 8.95419 12.0458 9.9168C12.0458 10.8795 12.0459 11.6473 11.9901 12.2635C11.9334 12.8888 11.8142 13.4254 11.535 13.909C11.2008 14.4877 10.7202 14.9683 10.1415 15.3025C9.65792 15.5817 9.12128 15.7009 8.49597 15.7576C7.87984 15.8134 7.11196 15.8133 6.14929 15.8133C5.18667 15.8133 4.41874 15.8134 3.80261 15.7576C3.1773 15.7009 2.64067 15.5817 2.1571 15.3025C1.5784 14.9683 1.09778 14.4877 0.76355 13.909C0.484366 13.4254 0.365184 12.8888 0.308472 12.2635C0.252649 11.6473 0.252808 10.8795 0.252808 9.9168C0.252808 8.95418 0.252664 8.18625 0.308472 7.57012C0.365184 6.94481 0.484366 6.40818 0.76355 5.92462C1.09777 5.34589 1.57839 4.86529 2.1571 4.53106C2.64067 4.25188 3.1773 4.13269 3.80261 4.07598C4.41874 4.02017 5.18666 4.02032 6.14929 4.02032ZM6.14929 5.37774C5.16181 5.37774 4.46634 5.37761 3.92566 5.42657C3.39434 5.47472 3.07859 5.56574 2.83582 5.70587C2.4632 5.92106 2.15354 6.2307 1.93835 6.60333C1.79823 6.8461 1.70721 7.16185 1.65906 7.69317C1.6101 8.23385 1.61023 8.92933 1.61023 9.9168C1.61023 10.9043 1.61009 11.5998 1.65906 12.1404C1.70721 12.6717 1.79823 12.9875 1.93835 13.2303C2.15356 13.6029 2.46321 13.9126 2.83582 14.1277C3.07859 14.2679 3.39434 14.3589 3.92566 14.407C4.46634 14.456 5.16182 14.4559 6.14929 14.4559C7.13682 14.4559 7.83224 14.456 8.37292 14.407C8.90425 14.3589 9.21999 14.2679 9.46277 14.1277C9.83535 13.9126 10.145 13.6029 10.3602 13.2303C10.5004 12.9875 10.5914 12.6717 10.6395 12.1404C10.6885 11.5998 10.6884 10.9043 10.6884 9.9168C10.6884 8.92934 10.6885 8.23384 10.6395 7.69317C10.5914 7.16185 10.5004 6.8461 10.3602 6.60333C10.1451 6.23071 9.83536 5.92107 9.46277 5.70587C9.21999 5.56574 8.90424 5.47472 8.37292 5.42657C7.83224 5.3776 7.13682 5.37774 6.14929 5.37774ZM9.80164 0.367975C10.7638 0.367975 11.5314 0.36788 12.1473 0.423639C12.7726 0.480307 13.3093 0.598759 13.7928 0.877741C14.3717 1.21192 14.8521 1.69355 15.1864 2.27227C15.4655 2.75574 15.5857 3.29164 15.6425 3.9168C15.6983 4.53301 15.6971 5.3016 15.6971 6.26446V7.82989C15.6971 8.29264 15.6989 8.58993 15.6649 8.84844C15.4668 10.3525 14.401 11.5738 12.9833 11.9988V10.5467C13.6973 10.1903 14.2105 9.49662 14.3192 8.67169C14.3387 8.52347 14.3407 8.3358 14.3407 7.82989V6.26446C14.3407 5.27706 14.3398 4.58149 14.2909 4.04083C14.2428 3.50968 14.1526 3.19372 14.0126 2.95098C13.7974 2.57849 13.4876 2.26869 13.1151 2.05352C12.8724 1.91347 12.5564 1.82237 12.0253 1.77423C11.4847 1.72528 10.7888 1.7254 9.80164 1.7254H7.71472C6.7562 1.72558 5.92665 2.27697 5.52332 3.07891H4.07019C4.54221 1.51132 5.9932 0.368186 7.71472 0.367975H9.80164Z" fill="currentColor"></path></svg></div>Copy<div class="ds-focus-ring"></div></button><button role="button" aria-disabled="true" class="ds-atom-button ds-atom-button--disabled ds-text-button ds-text-button--with-icon ds-text-button--disabled" disabled=""><div class="ds-icon ds-atom-button__icon" style="font-size: 16px; width: 16px; height: 16px; margin-right: 3px;"><svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M15.3695 11.411L15.1234 12.8866C14.8869 14.3042 13.6603 15.3436 12.223 15.3436H3.77673C2.33958 15.3434 1.1128 14.3042 0.876343 12.8866L0.630249 11.411L2.05408 11.1747L2.29919 12.6493C2.41973 13.3713 3.04475 13.9001 3.77673 13.9003H12.223C12.9551 13.9002 13.58 13.3713 13.7006 12.6493L13.9457 11.1747L15.3695 11.411ZM8.72205 8.994C8.77717 8.93934 8.83792 8.88106 8.90271 8.81627L12.4828 5.23424L13.5043 6.25572L9.92224 9.8358C9.6395 10.1185 9.38763 10.3732 9.15857 10.5575C8.91892 10.7503 8.63953 10.9224 8.2865 10.9784C8.09711 11.0083 7.90363 11.0083 7.71423 10.9784C7.36106 10.9224 7.0809 10.7503 6.84119 10.5575C6.61215 10.3732 6.36022 10.1185 6.07751 9.8358L2.49646 6.25572L3.51697 5.23424L7.09705 8.81627C7.16219 8.88142 7.22331 8.94006 7.27869 8.99498V1.3065H8.72205V8.994Z" fill="currentColor"></path></svg></div>Download<div class="ds-focus-ring"></div></button></div></div></div></div></div>
## Key Classes

### FormMain
- Derives from `System.Windows.Forms.Form`.
- Sets up the UI layout programmatically (no designer file).
- Handles user interactions: open folder, initialize coder, rerun last tool, build mode toggle, file tree double‑click, context menu (open in Explorer/VS, Git actions).
- Maintains `_workspacePath` and `_activeFilePath`.
- Provides `Log()` and `SetStatus()` methods used by `BrowserBridge`.
- Refreshes the file tree after file write operations (via `_refreshTreeCallback`).

### BrowserBridge
- Orchestrates communication between the WebView2 (DeepSeek) and the local tool system.
- **`InitializeAsync()`**:
  - Ensures WebView2 core is ready.
  - Injects a large JavaScript automation script (see below).
  - Subscribes to `WebMessageReceived`.
  - Navigates to `https://chat.deepseek.com`.
- **`InitializeCoderAsync(string activeFilePath)`**:
  - Builds a system prompt containing the workspace directory tree, optional active file content, and the tool specification.
  - Injects that prompt into the DeepSeek chat input and sends it.
- **`WvDeepSeek_WebMessageReceived`**:
  - Listens for `tool_call` messages from the injected script.
  - Calls `ExecuteToolAsync` to run the requested tool.
  - Sends the result back to the browser via `window.injectToolResult`.
- **`ExecuteToolAsync`** – routes tool names to `ToolSystem` methods.
- **`RerunLastToolCallAsync`** – executes JavaScript `window.rerunLastToolCall()` to force re‑scanning of the last message.

### ToolSystem
- Pure logic class (no UI dependencies).
- **`ListDirectory(string workspacePath)`** – recursive directory tree, formatted as text.
- **`ReadFile(string absolutePath, int startLine, int endLine)`** – returns file content with line numbers.
- **`WriteFile(string absolutePath, string content)`** – overwrites file, creates directories if needed.
- **`ReplaceText(string absolutePath, string target, string replacement)`** – checks uniqueness of target, then replaces.
- **`RunCommandAsync(string command, string workingDir, Action<string> onOutput, CancellationToken token)`** – starts a process, reads output asynchronously, returns combined stdout/stderr.
- **`ExecuteSql(string connectionString, string sql)`** – uses `System.Data.SqlClient` to run queries, returns results as formatted text.
- **`SearchCode(string workspacePath, string searchQuery)`** – uses `Regex` to grep files (ignores bin/obj/.git/node_modules).
- **`WebFetchAsync(string url)`** – downloads HTML and uses `HtmlAgilityPack` to extract plain text.
- **`GetDbSchema(string connectionString)`** – queries `INFORMATION_SCHEMA` for tables, columns, types, and primary keys.

### GitService
- Wraps `git` commands executed via `ToolSystem.RunCommandAsync`.
- **`RunGitStatusAsync(string workspacePath, Action<string> log)`** – runs `git status --short`.
- **`RunGitCommitPushAsync(string workspacePath, string commitMessage, Action<string> log)`** – runs `git add .`, `git commit -m "..."`, `git push`.

### SettingsManager
- Simple JSON serialization of `AppSettings` (currently only `LastOpenedPath`).
- Saved in `%APPDATA%\DSCode\settings.json`.

### Theme
- Static class defining `Color` and `Font` properties for a consistent dark theme.
- Used throughout `FormMain` for controls.

## JavaScript Automation Script (Injected)

The script is a large IIFE injected via `AddScriptToExecuteOnDocumentCreatedAsync`. It runs inside the DeepSeek page context. Main functions:

- **`scanForToolCalls()`** – runs every 1.5 seconds (setInterval). It:
  - Checks `window.buildModeDisabled` (controlled by Build Mode toggle).
  - Finds the last message with class `.ds-markdown`.
  - Verifies user is scrolled to bottom (to avoid interrupting reading).
  - Parses the message HTML for a tool block using regex (see `parseToolCall`).
  - If found and not already executed, sends a `tool_call` message to C#.
- **`parseToolCall(el)`** – extracts tool name, path, content, command, target, replacement, start_line, end_line from the XML. Handles HTML entities and malformed tags.
- **`window.injectToolResult(result)`** – called from C# after tool execution. It finds the chat input, inserts `### TOOL OUTPUT:\n...\nProceed to next step.`, locates the send button (geometric heuristic), and clicks it.
- **`window.rerunLastToolCall()`** – clears the execution flag and signature of the last tool message, then triggers a new scan.

The script also includes helper functions `getScrollContainer`, `getMessageSignature`, and `findSendButton`.

## Tool Execution Flow (Sequence)

1. User asks DeepSeek to perform a task (e.g., "list all C# files").
2. DeepSeek responds with a ````xml <tool name="list_directory">