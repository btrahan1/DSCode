# DSCode – Autonomous Coding Assistant Desktop

DSCode is a Windows Forms application that embeds **DeepSeek Chat** inside a WebView2 control and turns it into a local autonomous coding agent. The AI can read/write files, run build commands, execute SQL queries, search the codebase, fetch web pages, and more – all by outputting simple XML tool calls that the desktop app intercepts and executes on your behalf.

![DSCode Screenshot](docs/screenshot.png) *(optional)*

## ✨ Features

- **Full Workspace Integration** – Select any folder as your workspace; the file tree displays with checkboxes to include files as context.
- **AI‑Driven Tool Execution** – DeepSeek (or any compatible chat) receives a system prompt listing 9 tools. When the AI outputs a ````xml <tool name="..."> ```` block, the app parses it, runs the tool locally, and injects the result back into the chat.
- **Direct File Operations** – Read, write, and replace text in files (with uniqueness checking).
- **Command Execution** – Run terminal commands like `dotnet build`, `git status`, etc. (blocking commands are disallowed).
- **SQL Server Support** – Execute queries and export database schemas via a configurable connection string.
- **Git Integration** – One‑click git status and commit+push from the file explorer context menu.
- **Build Mode Toggle** – Enable/disable tool scanning without losing your chat context.
- **File Viewer** – Double‑click any file to view its content in a syntax‑highlighted rich text box.
- **Persistent Workspace** – Last opened folder is saved and automatically reloaded on startup.

## 🚀 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download) (or later)
- [WebView2 Evergreen Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (usually pre‑installed on Windows 11)
- SQL Server (optional) – for `execute_sql` and `get_db_schema` tools

### Build & Run

1. Clone or open the project folder in Visual Studio 2022 (or later).
2. Restore NuGet packages:
   ```bash
   dotnet restore
   ```
3. Build the solution:
   ```bash
   dotnet build
   ```
4. Run the application:
   ```bash
   dotnet run
   ```
   Or press **F5** in Visual Studio.

### First Use

1. Click **📁 Open Folder** and select your development workspace (e.g., a Git repo or a C# project folder).
2. Click **🤖 Initialize Coder**. This injects a system prompt into DeepSeek that describes all available tools and your workspace structure.
3. The WebView2 will navigate to `https://chat.deepseek.com`. You may need to log in (create a free account if needed).
4. Once the prompt is sent, DeepSeek will reply with a confirmation and ask how it can help.
5. Now you can ask the AI to perform tasks – it will automatically output tool calls, and DSCode will execute them and feed back the results.

> **Note:** The app only scans for tool calls when **Build Mode** is **ON** (the blue toggle button). Turn it off if you want to chat normally without triggering local execution.

## 🛠 Available Tools

| Tool Name | Description | Example XML |
|-----------|-------------|--------------|
| `list_directory` | Returns a formatted tree of the workspace folder. | ````xml\n<tool name="list_directory">