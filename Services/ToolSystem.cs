using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DSCode.Services;

public class ToolSystem
{
    private readonly string[] _ignoredDirs = { ".git", "bin", "obj", ".vs", "node_modules", "lib" };
    private readonly string[] _ignoredExtensions = { ".map", ".png", ".ico", ".jpg", ".jpeg", ".gif", ".pdf", ".zip", ".tar", ".gz" };

    public string ListDirectory(string rootPath)
    {
        if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
            return "Error: Directory does not exist.";

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("WORKSPACE DIRECTORY STRUCTURE:");
            var files = Directory.GetFileSystemEntries(rootPath, "*", SearchOption.AllDirectories)
                .Where(f => !IsIgnored(f, rootPath))
                .Select(f => Path.GetRelativePath(rootPath, f))
                .OrderBy(f => f)
                .ToList();

            if (files.Count == 0)
            {
                sb.AppendLine("(Empty Directory)");
            }
            else
            {
                foreach (var file in files)
                {
                    sb.AppendLine($"- {file}");
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error listing directory: {ex.Message}";
        }
    }

    public string ReadFile(string filePath, int startLine = 0, int endLine = 0)
    {
        if (!File.Exists(filePath))
            return $"Error: File '{filePath}' does not exist.";

        try
        {
            var lines = File.ReadAllLines(filePath);
            var sb = new StringBuilder();

            int start = startLine > 0 ? startLine : 1;
            int end = endLine > 0 ? endLine : lines.Length;

            start = Math.Clamp(start, 1, lines.Length);
            end = Math.Clamp(end, start, lines.Length);

            sb.AppendLine($"[FILE CONTENT OF {Path.GetFileName(filePath)} (Lines {start}-{end} of {lines.Length})]");
            for (int i = start; i <= end; i++)
            {
                sb.AppendLine($"{i}: {lines[i - 1]}");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }

    public string WriteFile(string filePath, string content)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(filePath, content);
            return $"Success: Wrote '{Path.GetFileName(filePath)}' successfully.";
        }
        catch (Exception ex)
        {
            return $"Error writing file: {ex.Message}";
        }
    }

    public string ReplaceText(string filePath, string targetText, string replacementText)
    {
        if (!File.Exists(filePath))
            return $"Error: File '{filePath}' does not exist.";

        try
        {
            string fileContent = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(targetText))
            {
                return "Error: Target text is empty.";
            }

            int idx = fileContent.IndexOf(targetText, StringComparison.Ordinal);
            if (idx == -1)
            {
                return "Error: Edit failed (target text not found). Please use a full file replacement (write_file) instead.";
            }

            int lastIdx = fileContent.LastIndexOf(targetText, StringComparison.Ordinal);
            if (idx != lastIdx)
            {
                return "Error: Edit failed (target text matches multiple locations). Please use a full file replacement (write_file) instead.";
            }

            string newContent = fileContent.Remove(idx, targetText.Length).Insert(idx, replacementText);
            File.WriteAllText(filePath, newContent);
            return $"Success: Replaced target text block in '{Path.GetFileName(filePath)}'.";
        }
        catch (Exception ex)
        {
            return $"Error replacing text: {ex.Message}. Please use a full file replacement (write_file) instead.";
        }
    }

    public async Task<string> RunCommandAsync(string command, string workingDir, Action<string> onOutputReceived, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(workingDir) || !Directory.Exists(workingDir))
            return "Error: Working directory does not exist.";

        return await Task.Run(() =>
        {
            var sb = new StringBuilder();
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -Command \"{command.Replace("\"", "\\\"")}\"",
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process();
                process.StartInfo = processInfo;

                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        onOutputReceived(args.Data + Environment.NewLine);
                        sb.AppendLine(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        onOutputReceived("[ERROR] " + args.Data + Environment.NewLine);
                        sb.AppendLine("[ERROR] " + args.Data);
                    }
                };

                using var registration = cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true);
                        }
                    }
                    catch { }
                });

                if (cancellationToken.IsCancellationRequested)
                {
                    return "Command execution canceled before starting.";
                }

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                if (cancellationToken.IsCancellationRequested)
                {
                    return sb.ToString() + Environment.NewLine + "[Command Canceled by User]";
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                string errMsg = $"Process exception: {ex.Message}";
                onOutputReceived(errMsg + Environment.NewLine);
                return errMsg;
            }
        });
    }

    public string ExecuteSql(string connectionString, string query)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "Error: SQL Connection string is empty.";

        try
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = query;

            using var reader = command.ExecuteReader();
            if (reader.FieldCount > 0)
            {
                var dt = new System.Data.DataTable();
                dt.Load(reader);

                if (dt.Rows.Count == 0)
                {
                    return "(0 rows returned)";
                }

                var sb = new StringBuilder();
                var columns = dt.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName).ToList();
                sb.AppendLine("| " + string.Join(" | ", columns) + " |");
                sb.AppendLine("| " + string.Join(" | ", columns.Select(_ => "---")) + " |");

                foreach (System.Data.DataRow row in dt.Rows)
                {
                    var values = dt.Columns.Cast<System.Data.DataColumn>()
                        .Select(c => row[c]?.ToString() ?? "NULL")
                        .ToList();
                    sb.AppendLine("| " + string.Join(" | ", values) + " |");
                }

                return sb.ToString();
            }
            else
            {
                int affected = reader.RecordsAffected;
                return $"Success: Query executed successfully. Records affected: {affected}.";
            }
        }
        catch (Exception ex)
        {
            return $"Error executing SQL command: {ex.Message}";
        }
    }

    private bool IsIgnored(string fullPath, string rootPath)
    {
        var relative = Path.GetRelativePath(rootPath, fullPath).ToLower();
        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        
        if (parts.Any(p => _ignoredDirs.Contains(p)))
            return true;

        var ext = Path.GetExtension(fullPath).ToLower();
        if (_ignoredExtensions.Contains(ext))
            return true;

        return false;
    }
}
