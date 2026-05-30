using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using DSCode.Services;

namespace DSCode;

public class FormMain : Form
{
    // Services
    private readonly ToolSystem _toolSystem;
    private readonly GitService _gitService;
    private BrowserBridge _browserBridge = null!;
    private string _workspacePath = string.Empty;

    // UI Elements
    private Label _lblWorkspaceStatus = null!;
    private Button _btnOpenFolder = null!;
    private Button _btnInitialize = null!;
    private Button _btnRerunTool = null!;
    private CheckBox _chkBuildMode = null!;
    private TreeView _tvFiles = null!;
    private ContextMenuStrip _explorerContextMenu = null!;
    private WebView2 _wvDeepSeek = null!;
    private RichTextBox _rtbLogs = null!;
    private Label _lblStatusText = null!;
    private TextBox _txtSqlConnection = null!;

    // File Viewer Fields
    private TableLayoutPanel _pnlFileViewer = null!;
    private Label _lblViewerHeader = null!;
    private RichTextBox _rtbCodeViewer = null!;
    private string _activeFilePath = string.Empty;

    public FormMain()
    {
        _toolSystem = new ToolSystem();
        _gitService = new GitService(_toolSystem);

        // Configure Form settings
        this.Text = "DeepSeek WebView2 Developer Workspace";
        this.Size = new Size(1500, 950);
        this.MinimumSize = new Size(1000, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Theme.Background;
        this.Font = Theme.RegularFont;

        SetupDynamicLayout();
        this.Load += FormMain_Load;
    }

    private void SetupDynamicLayout()
    {
        // 1. Root Layout Table (Header + Workspace split)
        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45)); // Header Height
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Content split
        this.Controls.Add(rootLayout);

        // 2. Header Panel
        var pnlHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 8,
            RowCount = 1,
            BackColor = Theme.Sidebar,
            Padding = new Padding(8, 6, 8, 6),
            Margin = new Padding(0)
        };
        pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130)); // Open Folder Button
        pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // Initialize Button
        pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170)); // Rerun Button
        pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); // Build Mode Button
        pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));  // SQL Label
        pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350)); // SQL TextBox
        pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // Workspace text
        pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200)); // Status Text
        rootLayout.Controls.Add(pnlHeader, 0, 0);

        _btnOpenFolder = new Button
        {
            Text = "📁 Open Folder",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextMain,
            Font = Theme.HeaderFont,
            Cursor = Cursors.Hand
        };
        _btnOpenFolder.FlatAppearance.BorderColor = Theme.Border;
        _btnOpenFolder.FlatAppearance.BorderSize = 1;
        _btnOpenFolder.Click += btnOpenFolder_Click;
        pnlHeader.Controls.Add(_btnOpenFolder, 0, 0);

        _btnInitialize = new Button
        {
            Text = "🤖 Initialize Coder",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextMain,
            Font = Theme.HeaderFont,
            Cursor = Cursors.Hand
        };
        _btnInitialize.FlatAppearance.BorderColor = Theme.Border;
        _btnInitialize.FlatAppearance.BorderSize = 1;
        _btnInitialize.Click += btnInitialize_Click;
        pnlHeader.Controls.Add(_btnInitialize, 1, 0);

        _btnRerunTool = new Button
        {
            Text = "🔄 Rerun Last Tool",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextMain,
            Font = Theme.HeaderFont,
            Cursor = Cursors.Hand
        };
        _btnRerunTool.FlatAppearance.BorderColor = Theme.Border;
        _btnRerunTool.FlatAppearance.BorderSize = 1;
        _btnRerunTool.Click += btnRerunTool_Click;
        pnlHeader.Controls.Add(_btnRerunTool, 2, 0);

        _chkBuildMode = new CheckBox
        {
            Text = "🔨 Build Mode: OFF",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Appearance = Appearance.Button,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextSecondary,
            Font = Theme.HeaderFont,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Checked = false
        };
        _chkBuildMode.FlatAppearance.BorderColor = Theme.Border;
        _chkBuildMode.FlatAppearance.BorderSize = 1;
        _chkBuildMode.CheckedChanged += ChkBuildMode_CheckedChanged;
        pnlHeader.Controls.Add(_chkBuildMode, 3, 0);

        var lblSql = new Label
        {
            Text = "🔌 SQL DB:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Theme.TextSecondary,
            Font = Theme.HeaderFont
        };
        pnlHeader.Controls.Add(lblSql, 4, 0);

        _txtSqlConnection = new TextBox
        {
            Text = @"Server=MSI\SQLEXPRESS01;Database=master;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;",
            Dock = DockStyle.Fill,
            Font = Theme.RegularFont,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextMain,
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlHeader.Controls.Add(_txtSqlConnection, 5, 0);

        _lblWorkspaceStatus = new Label
        {
            Text = "No Workspace Folder Loaded (Anchor a folder to begin development)",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.TextSecondary,
            Font = Theme.HeaderFont,
            Padding = new Padding(10, 0, 0, 0)
        };
        pnlHeader.Controls.Add(_lblWorkspaceStatus, 6, 0);

        _lblStatusText = new Label
        {
            Text = "Ready",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Theme.Success,
            Font = Theme.HeaderFont
        };
        pnlHeader.Controls.Add(_lblStatusText, 7, 0);

        // 3. Workspace Splitter (Left: Sidebar, Right: Main Views)
        var splitWorkspace = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            BackColor = Theme.Border
        };
        rootLayout.Controls.Add(splitWorkspace, 0, 1);

        // 4. Sidebar Content Container (Left Panel)
        var pnlSidebar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Sidebar,
            Padding = new Padding(6),
            Margin = new Padding(0)
        };
        pnlSidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Label Header
        pnlSidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Tree View
        splitWorkspace.Panel1.Controls.Add(pnlSidebar);

        var pnlSidebarHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        pnlSidebarHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Header Label
        pnlSidebarHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120)); // Copy Button

        var lblFilesHeader = new Label
        {
            Text = "📂 WORKSPACE EXPLORER",
            Dock = DockStyle.Fill,
            ForeColor = Theme.TextMain,
            Font = Theme.HeaderFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlSidebarHeader.Controls.Add(lblFilesHeader, 0, 0);

        var btnCopyContext = new Button
        {
            Text = "📋 Copy Context",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextSecondary,
            Font = new Font(Theme.RegularFont.FontFamily, 7.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCopyContext.FlatAppearance.BorderColor = Theme.Border;
        btnCopyContext.FlatAppearance.BorderSize = 1;
        btnCopyContext.Click += btnCopyContext_Click;
        pnlSidebarHeader.Controls.Add(btnCopyContext, 1, 0);

        pnlSidebar.Controls.Add(pnlSidebarHeader, 0, 0);

        _tvFiles = new TreeView
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextMain,
            LineColor = Theme.Border,
            BorderStyle = BorderStyle.None,
            Font = Theme.RegularFont,
            Indent = 15,
            CheckBoxes = true
        };
        _tvFiles.NodeMouseDoubleClick += tvFiles_NodeMouseDoubleClick;
        _tvFiles.AfterCheck += tvFiles_AfterCheck;
        pnlSidebar.Controls.Add(_tvFiles, 0, 1);

        // Context Menu Setup
        _explorerContextMenu = new ContextMenuStrip();
        
        var menuOpenExplorer = new ToolStripMenuItem("📂 Open in File Explorer", null, btnOpenExplorer_Click);
        var menuOpenVs = new ToolStripMenuItem("💻 Open in Visual Studio", null, btnOpenVs_Click);
        
        var menuGitOptions = new ToolStripMenuItem("📌 Git Options");
        var gitStatusItem = new ToolStripMenuItem("Status", null, gitStatus_Click);
        var gitCommitPushItem = new ToolStripMenuItem("Commit & Push...", null, gitCommitPush_Click);
        menuGitOptions.DropDownItems.Add(gitStatusItem);
        menuGitOptions.DropDownItems.Add(gitCommitPushItem);
        
        _explorerContextMenu.Items.Add(menuOpenExplorer);
        _explorerContextMenu.Items.Add(menuOpenVs);
        _explorerContextMenu.Items.Add(new ToolStripSeparator());
        _explorerContextMenu.Items.Add(menuGitOptions);
        
        _tvFiles.ContextMenuStrip = _explorerContextMenu;
        _tvFiles.NodeMouseClick += (s, ev) =>
        {
            if (ev.Button == MouseButtons.Right)
            {
                _tvFiles.SelectedNode = ev.Node;
            }
        };

        // 5. Main Splitter (Top: DeepSeek WebView2 + File Viewer, Bottom: Console Logs)
        var splitMain = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 6,
            BackColor = Theme.Border
        };
        splitWorkspace.Panel2.Controls.Add(splitMain);

        // Nested Vertical Split for WebView2 (Left) and File Viewer (Right)
        var splitContent = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            BackColor = Theme.Border
        };
        splitMain.Panel1.Controls.Add(splitContent);

        // WebView2 Container (Left side of splitContent)
        _wvDeepSeek = new WebView2
        {
            Dock = DockStyle.Fill
        };
        splitContent.Panel1.Controls.Add(_wvDeepSeek);

        // File Viewer Panel (Right side of splitContent)
        _pnlFileViewer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Theme.Background,
            Padding = new Padding(4),
            Margin = new Padding(0)
        };
        _pnlFileViewer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Title
        _pnlFileViewer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));  // Close Button
        _pnlFileViewer.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));       // Header row
        _pnlFileViewer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));       // Code viewer row
        splitContent.Panel2.Controls.Add(_pnlFileViewer);

        _lblViewerHeader = new Label
        {
            Text = "📄 CODE VIEWER",
            Dock = DockStyle.Fill,
            ForeColor = Theme.TextSecondary,
            Font = Theme.HeaderFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _pnlFileViewer.Controls.Add(_lblViewerHeader, 0, 0);

        var btnCloseViewer = new Button
        {
            Text = "Close ✕",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextSecondary,
            Font = Theme.RegularFont,
            Cursor = Cursors.Hand
        };
        btnCloseViewer.FlatAppearance.BorderColor = Theme.Border;
        btnCloseViewer.FlatAppearance.BorderSize = 1;
        btnCloseViewer.Click += (s, ev) => {
            splitContent.SplitterDistance = splitContent.Width; // hide right panel
        };
        _pnlFileViewer.Controls.Add(btnCloseViewer, 1, 0);

        _rtbCodeViewer = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextMain,
            Font = Theme.CodeFont,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            WordWrap = false
        };
        _pnlFileViewer.Controls.Add(_rtbCodeViewer, 0, 1);
        _pnlFileViewer.SetColumnSpan(_rtbCodeViewer, 2);

        // 6. Logs & Output Console Panel (Bottom Panel)
        var pnlLogs = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Background,
            Padding = new Padding(6),
            Margin = new Padding(0)
        };
        pnlLogs.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // Logs Title
        pnlLogs.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Text Box
        splitMain.Panel2.Controls.Add(pnlLogs);

        var lblLogsHeader = new Label
        {
            Text = "🖥 LOCAL TOOL EXECUTION LOGS",
            Dock = DockStyle.Fill,
            ForeColor = Theme.TextSecondary,
            Font = Theme.HeaderFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlLogs.Controls.Add(lblLogsHeader, 0, 0);

        _rtbLogs = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextMain,
            Font = Theme.CodeFont,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            WordWrap = true
        };
        pnlLogs.Controls.Add(_rtbLogs, 0, 1);

        // Default Split Distances
        splitWorkspace.SplitterDistance = 280;
        splitMain.SplitterDistance = 650;
    }

    private async void FormMain_Load(object? sender, EventArgs e)
    {
        Log("Initializing application...");
        _browserBridge = new BrowserBridge(
            _wvDeepSeek,
            _toolSystem,
            () => _workspacePath,
            () => _txtSqlConnection.Text,
            () => _chkBuildMode.Checked,
            msg => Log(msg),
            (statusText, statusColor) => SetStatus(statusText, statusColor),
            () => RefreshFileTreeOnMainThread()
        );
        await _browserBridge.InitializeAsync();
        
        var settings = SettingsManager.Load();
        if (!string.IsNullOrEmpty(settings.LastOpenedPath) && Directory.Exists(settings.LastOpenedPath))
        {
            OpenWorkspace(settings.LastOpenedPath);
        }
        else
        {
            Log("Ready. Open a folder to target development.");
        }
    }



    private void btnOpenFolder_Click(object? sender, EventArgs e)
    {
        using var fbd = new FolderBrowserDialog();
        fbd.Description = "Select Workspace Development Folder";
        fbd.UseDescriptionForTitle = true;

        if (fbd.ShowDialog() == DialogResult.OK)
        {
            OpenWorkspace(fbd.SelectedPath);
        }
    }

    private void OpenWorkspace(string path)
    {
        _workspacePath = path;
        _lblWorkspaceStatus.Text = $"Active Folder: {path}";
        _lblWorkspaceStatus.ForeColor = Theme.TextMain;
        Log($"Loaded workspace: {path}");
        
        // Persist path
        SettingsManager.Save(new AppSettings { LastOpenedPath = path });

        RefreshFileTree();
    }

    private void RefreshFileTreeOnMainThread()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(RefreshFileTree));
        }
        else
        {
            RefreshFileTree();
        }
    }

    private void RefreshFileTree()
    {
        _tvFiles.Nodes.Clear();
        if (string.IsNullOrEmpty(_workspacePath) || !Directory.Exists(_workspacePath)) return;

        try
        {
            var rootDirectoryInfo = new DirectoryInfo(_workspacePath);
            var rootNode = CreateDirectoryNode(rootDirectoryInfo);
            _tvFiles.Nodes.Add(rootNode);
            rootNode.Expand();
        }
        catch (Exception ex)
        {
            Log($"[Error building file tree]: {ex.Message}");
        }
    }

    private TreeNode CreateDirectoryNode(DirectoryInfo directoryInfo)
    {
        var directoryNode = new TreeNode(directoryInfo.Name)
        {
            Tag = directoryInfo.FullName,
            ForeColor = Theme.TextMain
        };

        try
        {
            foreach (var directory in directoryInfo.GetDirectories())
            {
                if (directory.Name == "bin" || directory.Name == "obj" || directory.Name == ".git" || directory.Name == "node_modules")
                    continue;
                directoryNode.Nodes.Add(CreateDirectoryNode(directory));
            }
            foreach (var file in directoryInfo.GetFiles())
            {
                var fileNode = new TreeNode(file.Name)
                {
                    Tag = file.FullName,
                    ForeColor = Theme.TextSecondary
                };
                directoryNode.Nodes.Add(fileNode);
            }
        }
        catch { }

        return directoryNode;
    }

    private void tvFiles_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node == null || e.Node.Tag == null) return;
        string path = e.Node.Tag.ToString() ?? "";

        if (File.Exists(path))
        {
            try
            {
                string text = File.ReadAllText(path);
                _rtbCodeViewer.Text = text;
                _activeFilePath = path;
                _lblViewerHeader.Text = $"📄 {Path.GetFileName(path)}";

                // Locate the parent split container to expand the file viewer if hidden
                var parentSplit = _pnlFileViewer.Parent as SplitContainer;
                if (parentSplit != null && parentSplit.SplitterDistance >= parentSplit.Width - 100)
                {
                    parentSplit.SplitterDistance = (int)(parentSplit.Width * 0.6); // give 60% to WebView, 40% to viewer
                }
                Log($"Opened file in viewer: {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                Log($"[Error opening file]: {ex.Message}");
            }
        }
    }

    private bool _isUpdatingCheck = false;
    private void tvFiles_AfterCheck(object? sender, TreeViewEventArgs e)
    {
        if (e.Node == null || _isUpdatingCheck) return;
        _isUpdatingCheck = true;
        try
        {
            UpdateChildChecks(e.Node, e.Node.Checked);
        }
        finally
        {
            _isUpdatingCheck = false;
        }
    }

    private void UpdateChildChecks(TreeNode node, bool isChecked)
    {
        foreach (TreeNode child in node.Nodes)
        {
            child.Checked = isChecked;
            UpdateChildChecks(child, isChecked);
        }
    }

    private void btnCopyContext_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_workspacePath))
        {
            MessageBox.Show("Please open a workspace folder first.", "Workspace Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var checkedFiles = new List<string>();
        foreach (TreeNode node in _tvFiles.Nodes)
        {
            CollectCheckedFiles(node, checkedFiles);
        }

        if (checkedFiles.Count == 0)
        {
            MessageBox.Show("Please select one or more files in the workspace explorer first.", "No Files Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var sb = new StringBuilder();
            foreach (var file in checkedFiles)
            {
                if (File.Exists(file))
                {
                    string content = File.ReadAllText(file);
                    string relPath = Path.GetRelativePath(_workspacePath, file);
                    sb.AppendLine($"// FILE: {relPath}");
                    sb.AppendLine("```");
                    sb.AppendLine(content);
                    sb.AppendLine("```");
                    sb.AppendLine();
                }
            }

            Clipboard.SetText(sb.ToString());
            MessageBox.Show($"Successfully copied context for {checkedFiles.Count} files to clipboard!", "Context Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error copying files to clipboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CollectCheckedFiles(TreeNode node, List<string> files)
    {
        string path = node.Tag?.ToString() ?? "";
        if (node.Checked && File.Exists(path))
        {
            files.Add(path);
        }
        foreach (TreeNode child in node.Nodes)
        {
            CollectCheckedFiles(child, files);
        }
    }

    private async void ChkBuildMode_CheckedChanged(object? sender, EventArgs e)
    {
        if (_chkBuildMode.Checked)
        {
            _chkBuildMode.Text = "⚡ Build Mode: ON";
            _chkBuildMode.BackColor = Theme.Success;
            _chkBuildMode.ForeColor = Color.White;
        }
        else
        {
            _chkBuildMode.Text = "🔨 Build Mode: OFF";
            _chkBuildMode.BackColor = Theme.Surface;
            _chkBuildMode.ForeColor = Theme.TextSecondary;
        }

        await _browserBridge.UpdateBuildModeInBrowserAsync(_chkBuildMode.Checked);
    }

    private async void btnInitialize_Click(object? sender, EventArgs e)
    {
        await _browserBridge.InitializeCoderAsync(_activeFilePath);
    }

    private async void btnRerunTool_Click(object? sender, EventArgs e)
    {
        await _browserBridge.RerunLastToolCallAsync();
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(Log), message);
            return;
        }

        if (string.IsNullOrEmpty(message)) return;
        _rtbLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _rtbLogs.SelectionStart = _rtbLogs.Text.Length;
        _rtbLogs.ScrollToCaret();
    }

    private void btnOpenExplorer_Click(object? sender, EventArgs e)
    {
        if (_tvFiles.SelectedNode == null || _tvFiles.SelectedNode.Tag == null) return;
        string path = _tvFiles.SelectedNode.Tag.ToString() ?? "";

        try
        {
            if (File.Exists(path))
            {
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else if (Directory.Exists(path))
            {
                Process.Start("explorer.exe", $"\"{path}\"");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open Explorer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnOpenVs_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_workspacePath) || !Directory.Exists(_workspacePath)) return;

        try
        {
            string[] slnFiles = Directory.GetFiles(_workspacePath, "*.sln", SearchOption.AllDirectories);
            string targetPath = "";
            if (slnFiles.Length > 0)
            {
                targetPath = slnFiles[0];
            }
            else
            {
                string[] csprojFiles = Directory.GetFiles(_workspacePath, "*.csproj", SearchOption.AllDirectories);
                if (csprojFiles.Length > 0)
                {
                    targetPath = csprojFiles[0];
                }
            }

            if (!string.IsNullOrEmpty(targetPath))
            {
                Log($"Launching Visual Studio for target: {Path.GetFileName(targetPath)}");
                Process.Start(new ProcessStartInfo(targetPath) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("No .sln or .csproj file found in this workspace.", "Project File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to launch Visual Studio: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void gitStatus_Click(object? sender, EventArgs e)
    {
        await _gitService.RunGitStatusAsync(_workspacePath, msg => Log(msg));
    }

    private async void gitCommitPush_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_workspacePath)) return;

        string commitMessage = ShowInputDialog(
            "Enter commit message:", 
            "Git Commit & Push", 
            $"Savepoint: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
        );

        if (string.IsNullOrWhiteSpace(commitMessage))
        {
            Log("[Git Commit & Push] Cancelled by user.");
            return;
        }

        await _gitService.RunGitCommitPushAsync(_workspacePath, commitMessage, msg => Log(msg));
    }

    private static string ShowInputDialog(string text, string caption, string defaultVal = "")
    {
        Form prompt = new Form()
        {
            Width = 400,
            Height = 160,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = caption,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = Theme.Background,
            ForeColor = Theme.TextMain,
            Font = Theme.RegularFont
        };
        Label textLabel = new Label() { Left = 20, Top = 15, Text = text, AutoSize = true };
        TextBox textBox = new TextBox() { Left = 20, Top = 40, Width = 340, Text = defaultVal, BorderStyle = BorderStyle.FixedSingle, BackColor = Theme.Surface, ForeColor = Theme.TextMain };
        Button confirmation = new Button() { Text = "Ok", Left = 270, Width = 90, Top = 80, DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat, BackColor = Theme.Surface };
        Button cancel = new Button() { Text = "Cancel", Left = 170, Width = 90, Top = 80, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat, BackColor = Theme.Surface };
        
        confirmation.FlatAppearance.BorderColor = Theme.Border;
        cancel.FlatAppearance.BorderColor = Theme.Border;
        confirmation.ForeColor = Theme.TextMain;
        cancel.ForeColor = Theme.TextMain;

        prompt.Controls.Add(textBox);
        prompt.Controls.Add(confirmation);
        prompt.Controls.Add(cancel);
        prompt.Controls.Add(textLabel);
        prompt.AcceptButton = confirmation;
        prompt.CancelButton = cancel;

        return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
    }

    private void SetStatus(string text, Color color)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string, Color>(SetStatus), text, color);
            return;
        }
        _lblStatusText.Text = text;
        _lblStatusText.ForeColor = color;
    }
}
