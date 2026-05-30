using System;
using System.Threading.Tasks;

namespace DSCode.Services;

public class GitService
{
    private readonly ToolSystem _toolSystem;

    public GitService(ToolSystem toolSystem)
    {
        _toolSystem = toolSystem;
    }

    public async Task<string> RunGitStatusAsync(string workspacePath, Action<string> logCallback)
    {
        if (string.IsNullOrEmpty(workspacePath))
            return "Error: Workspace path is not set.";

        logCallback("[Git Status] Executing command...");
        string result = await _toolSystem.RunCommandAsync("git status", workspacePath, outStr => logCallback(outStr.TrimEnd()));
        logCallback($"[Git Status Complete] Output size: {result.Length} chars.");
        return result;
    }

    public async Task<string> RunGitCommitPushAsync(string workspacePath, string commitMessage, Action<string> logCallback)
    {
        if (string.IsNullOrEmpty(workspacePath))
            return "Error: Workspace path is not set.";

        logCallback($"[Git Commit & Push] Initiating stage, commit, and push...");
        string chainedCommand = $"git add .; git commit -m \"{commitMessage.Replace("\"", "\\\"")}\"; git push";
        string result = await _toolSystem.RunCommandAsync(chainedCommand, workspacePath, outStr => logCallback(outStr.TrimEnd()));
        logCallback($"[Git Commit & Push Complete] Output size: {result.Length} chars.");
        return result;
    }
}
