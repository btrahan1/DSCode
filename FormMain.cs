using System;
using System.Drawing;
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
    private string _workspacePath = string.Empty;
    private CancellationTokenSource? _commandCts;

    // UI Elements
    private Label _lblWorkspaceStatus = null!;
    private Button _btnOpenFolder = null!;
    private Button _btnInitialize = null!;
    private Button _btnRerunTool = null!;
    private CheckBox _chkBuildMode = null!;
    private TreeView _tvFiles = null!;
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

        var lblFilesHeader = new Label
        {
            Text = "📂 WORKSPACE EXPLORER",
            Dock = DockStyle.Fill,
            ForeColor = Theme.TextMain,
            Font = Theme.HeaderFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlSidebar.Controls.Add(lblFilesHeader, 0, 0);

        _tvFiles = new TreeView
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextMain,
            LineColor = Theme.Border,
            BorderStyle = BorderStyle.None,
            Font = Theme.RegularFont,
            Indent = 15
        };
        _tvFiles.NodeMouseDoubleClick += tvFiles_NodeMouseDoubleClick;
        pnlSidebar.Controls.Add(_tvFiles, 0, 1);

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
        await InitializeWebViewAsync();
        
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

    private async Task InitializeWebViewAsync()
    {
        try
        {
            Log("Creating WebView2 environment...");
            await _wvDeepSeek.EnsureCoreWebView2Async(null);

            // Injected automation script that runs inside DeepSeek web interface
            string script = @"
(function() {
    window.buildModeDisabled = true;
    let currentTask = null;
    const executedSignatures = new Set();

    function logToHost(msg) {
        window.chrome.webview.postMessage({ type: 'log', message: msg });
    }

    function getScrollContainer(el) {
        let parent = el.parentElement;
        while (parent) {
            const style = window.getComputedStyle(parent);
            if ((parent.scrollHeight > parent.clientHeight) && 
                (style.overflowY === 'auto' || style.overflowY === 'scroll' || parent.tagName === 'BODY')) {
                return parent;
            }
            parent = parent.parentElement;
        }
        return null;
    }

    function getMessageSignature(msgEl, container) {
        const xml = msgEl.textContent || '';
        const sh = container ? container.scrollHeight : 0;
        let prevText = '';
        const parent = msgEl.closest('.ds-message'); 
        if (parent && parent.previousElementSibling) {
            prevText = parent.previousElementSibling.textContent.slice(-100);
        }
        return xml + '_' + sh + '_' + prevText;
    }

    function parseToolCall(el) {
        let html = el.innerHTML;
        if (!html) return null;

        // 1. Strip styling and formatting tags (span, pre, code)
        let cleanHtml = html.replace(/<\/?(?:span|pre|code)[^>]*>/gi, '');

        // 2. Decode HTML entities
        cleanHtml = cleanHtml
            .replace(/&lt;/g, '<')
            .replace(/&gt;/g, '>')
            .replace(/&amp;/g, '&')
            .replace(/&quot;/g, '""')
            .replace(/&#39;/g, ""'"");

        // 3. Regex match the tool block
        const toolPattern = /<tool\s+name\s*=\s*[""']?([^""'\s>]+)[""']?\s*(?:\/>|>\s*<\/tool\s*>|>([\s\S]*?)<\/tool\s*>)/i;
        const match = toolPattern.exec(cleanHtml);
        if (!match) return null;

        const toolName = match[1].trim();
        const innerXml = match[2] || '';
        const fullMatchText = match[0];

        // Helper to extract tags from inner XML
        const extractField = (xml, tag, nextTagPattern) => {
            try {
                const closedPattern = new RegExp('<' + tag + '[^>]*>([\\s\\S]*?)<\\/' + tag + '\\s*>', 'i');
                let m = closedPattern.exec(xml);
                if (m) return m[1].trim();

                const openPattern = new RegExp('<' + tag + '[^>]*>([\\s\\S]*?)(?:' + (nextTagPattern || '<') + '|$)', 'i');
                m = openPattern.exec(xml);
                return m ? m[1].trim() : '';
            } catch (err) {
                logToHost('Error in extractField for ' + tag + ': ' + err.message);
                return '';
            }
        };

        const path = extractField(innerXml, 'path', '<content|<start_line|<end_line');
        const content = extractField(innerXml, 'content', '</tool');
        const command = extractField(innerXml, 'command', '</tool');
        const target = extractField(innerXml, 'target', '<replacement|</tool');
        const replacement = extractField(innerXml, 'replacement', '</tool');
        const startLine = parseInt(extractField(innerXml, 'start_line')) || 0;
        const endLine = parseInt(extractField(innerXml, 'end_line')) || 0;

        return {
            name: toolName,
            path: path,
            content: content,
            command: command,
            target: target,
            replacement: replacement,
            startLine: startLine,
            endLine: endLine,
            raw: fullMatchText
        };
    }

    function scanForToolCalls() {
        try {
            if (window.buildModeDisabled) return;
            if (!document.body) return;

            const messages = document.querySelectorAll('.ds-markdown');
            if (!messages.length) return;

            const lastMsg = messages[messages.length - 1];
            if (!lastMsg) return;

            const container = getScrollContainer(lastMsg);
            if (container) {
                // If the user has scrolled up, do not run the tool
                const isAtBottom = (container.scrollHeight - container.scrollTop - container.clientHeight) < 150;
                if (!isAtBottom) return;
            }

            // Check if we already processed a tool call in this specific message node
            if (lastMsg.getAttribute('data-tool-executed') === 'true') return;

            const sig = getMessageSignature(lastMsg, container);
            if (executedSignatures.has(sig)) return;

            const toolCall = parseToolCall(lastMsg);
            if (toolCall) {
                // Mark this specific DOM node as executed before starting processing
                lastMsg.setAttribute('data-tool-executed', 'true');
                executedSignatures.add(sig);
                logToHost('Found tool call: ' + toolCall.name);

                window.chrome.webview.postMessage({
                    type: 'tool_call',
                    name: toolCall.name,
                    path: toolCall.path,
                    content: toolCall.content,
                    command: toolCall.command,
                    target: toolCall.target,
                    replacement: toolCall.replacement,
                    startLine: toolCall.startLine,
                    endLine: toolCall.endLine
                });
            }
        } catch (e) {
            logToHost('Error in scanForToolCalls: ' + e.message);
        }
    }

    // Handles the tool output returned by the host app
    window.injectToolResult = function(result) {
        logToHost('Received tool result from C#. Injecting into chat...');
        
        function findChatInput() {
            let el = document.querySelector('textarea');
            if (el) return el;
            el = document.querySelector('[contenteditable=""true""]');
            if (el) return el;
            const inputs = document.querySelectorAll('input, div, textarea');
            for (const input of inputs) {
                const ph = input.getAttribute('placeholder') || '';
                if (ph.toLowerCase().includes('message') || ph.toLowerCase().includes('deepseek')) {
                    return input;
                }
            }
            return null;
        }

        const inputEl = findChatInput();
        if (!inputEl) {
            logToHost('Error: Could not find Chat input box.');
            return;
        }

        const responseText = '### TOOL OUTPUT:\n' + result + '\n\nProceed to next step.';
        
        function insertTextIntoInput(el, text) {
            el.focus();
            let inserted = false;
            try {
                const selection = window.getSelection();
                if (el.tagName && (el.tagName.toLowerCase() === 'textarea' || el.tagName.toLowerCase() === 'input')) {
                    el.select();
                } else {
                    const range = document.createRange();
                    range.selectNodeContents(el);
                    selection.removeAllRanges();
                    selection.addRange(range);
                }
                
                if (document.execCommand('insertText', false, text)) {
                    logToHost('Text inserted via execCommand.');
                    inserted = true;
                }
            } catch (err) {
                logToHost('execCommand failed: ' + err.message);
            }
            
            if (!inserted) {
                if (el.tagName && (el.tagName.toLowerCase() === 'textarea' || el.tagName.toLowerCase() === 'input')) {
                    el.value = text;
                } else {
                    el.innerText = text;
                }
            }
            el.dispatchEvent(new Event('input', { bubbles: true }));
            el.dispatchEvent(new Event('change', { bubbles: true }));
        }

        insertTextIntoInput(inputEl, responseText);

        function findSendButton(el) {
            const textareaRect = el.getBoundingClientRect();
            if (textareaRect.width === 0) {
                logToHost('findSendButton: textarea is not visible or has zero size.');
                return null;
            }

            const buttons = Array.from(document.querySelectorAll('button, [role=""button""], div[class*=""send-button""]'));
            let nearbyButtons = [];
            
            for (const btn of buttons) {
                if (btn === el) continue;
                
                const rect = btn.getBoundingClientRect();
                if (rect.width === 0 || rect.height === 0) continue;
                
                // The Send button is always located near the bottom of the textarea (usually aligned with it horizontally or slightly below).
                const verticalDist = Math.abs(rect.bottom - textareaRect.bottom);
                const horizontalDist = rect.left - textareaRect.left;
                
                if (verticalDist < 120 && horizontalDist > -20 && rect.top > textareaRect.top - 50) {
                    nearbyButtons.push({ btn: btn, rect: rect });
                }
            }
            
            if (nearbyButtons.length === 0) {
                logToHost('findSendButton: no nearby buttons found.');
                return null;
            }
            
            // Sort by left coordinate ascending (leftmost to rightmost)
            nearbyButtons.sort((a, b) => a.rect.left - b.rect.left);
            
            // The Send button is always the rightmost one (the last candidate)
            logToHost('findSendButton: found ' + nearbyButtons.length + ' nearby buttons.');
            for (let i = 0; i < nearbyButtons.length; i++) {
                logToHost('Nearby ' + i + ': left=' + nearbyButtons[i].rect.left + ' text=""' + (nearbyButtons[i].btn.innerText || '').trim() + '"" class=""' + nearbyButtons[i].btn.className + '""');
            }
            
            const sendBtn = nearbyButtons[nearbyButtons.length - 1].btn;
            logToHost('findSendButton: selected rightmost button at left=' + nearbyButtons[nearbyButtons.length - 1].rect.left);
            return sendBtn;
        }

        setTimeout(() => {
            const sendButton = findSendButton(inputEl);
            if (sendButton) {
                sendButton.click();
                logToHost('Tool output sent successfully via button click.');
            } else {
                logToHost('Send button not found. Attempting keypress Enter event fallback...');
                const eventOpts = { bubbles: true, cancelable: true, key: 'Enter', code: 'Enter', keyCode: 13, which: 13, shiftKey: false, ctrlKey: false, altKey: false, metaKey: false };
                inputEl.dispatchEvent(new KeyboardEvent('keydown', eventOpts));
                inputEl.dispatchEvent(new KeyboardEvent('keypress', eventOpts));
                inputEl.dispatchEvent(new KeyboardEvent('keyup', eventOpts));
                logToHost('Keyboard Enter event sequence triggered.');
            }
        }, 300);
    };

    window.rerunLastToolCall = function() {
        if (!document.body) return 'Error: Page document not ready.';
        const messages = document.querySelectorAll('.ds-markdown');
        if (!messages.length) return 'Error: No messages found in the chat.';

        for (let i = messages.length - 1; i >= 0; i--) {
            const toolCall = parseToolCall(messages[i]);
            if (toolCall) {
                // Remove execution tag so scanning loop will execute it again
                messages[i].removeAttribute('data-tool-executed');
                const container = getScrollContainer(messages[i]);
                const sig = getMessageSignature(messages[i], container);
                executedSignatures.delete(sig);
                logToHost('Force re-triggering tool call: ' + toolCall.name);
                
                window.chrome.webview.postMessage({
                    type: 'tool_call',
                    name: toolCall.name,
                    path: toolCall.path,
                    content: toolCall.content,
                    command: toolCall.command,
                    target: toolCall.target,
                    replacement: toolCall.replacement,
                    startLine: toolCall.startLine,
                    endLine: toolCall.endLine
                });
                return 'Success: Re-triggered tool call ' + toolCall.name;
            }
        }
        return 'Error: No valid tool call blocks found in the message history.';
    };

    function init() {
        logToHost('DeepSeek Automation Bridge initialized.');
        setInterval(scanForToolCalls, 1500);
    }

    setTimeout(init, 2000);
})();";

            await _wvDeepSeek.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
            _wvDeepSeek.WebMessageReceived += WvDeepSeek_WebMessageReceived;
            _wvDeepSeek.NavigationCompleted += (s, args) =>
            {
                try
                {
                    _wvDeepSeek.CoreWebView2.ExecuteScriptAsync($"window.buildModeDisabled = {!_chkBuildMode.Checked};");
                }
                catch { }
            };
            _wvDeepSeek.CoreWebView2.Navigate("https://chat.deepseek.com");
            Log("Loaded DeepSeek interface in WebView2.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize WebView2: {ex.Message}\nMake sure Evergreen WebView2 Runtime is installed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Log($"[Error] WebView2 Init Failed: {ex.Message}");
        }
    }

    private async void WvDeepSeek_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            string json = e.WebMessageAsJson;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            if (!root.TryGetProperty("type", out var typeProp)) return;
            string type = typeProp.GetString() ?? "";

            if (type == "log")
            {
                string msg = root.GetProperty("message").GetString() ?? "";
                Log($"[Browser Bridge] {msg}");
            }
            else if (type == "tool_call")
            {
                string name = root.GetProperty("name").GetString() ?? "";
                string path = root.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
                string content = root.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
                string command = root.TryGetProperty("command", out var cmd) ? cmd.GetString() ?? "" : "";
                string target = root.TryGetProperty("target", out var tgt) ? tgt.GetString() ?? "" : "";
                string replacement = root.TryGetProperty("replacement", out var rep) ? rep.GetString() ?? "" : "";
                int startLine = root.TryGetProperty("startLine", out var sl) ? sl.GetInt32() : 0;
                int endLine = root.TryGetProperty("endLine", out var el) ? el.GetInt32() : 0;

                Log($"[Tool Execution Initiated] {name} (File Target: {path})");
                SetStatus($"Executing {name}...", Theme.Primary);

                string toolOutput = await Task.Run(async () =>
                {
                    return await ExecuteToolAsync(name, path, content, command, target, replacement, startLine, endLine);
                });

                Log($"[Tool Execution Complete] Result size: {toolOutput.Length} chars.");
                SetStatus("Ready", Theme.Success);

                // Send tool execution results back to the WebView2 script injection
                string escapedResult = JsonSerializer.Serialize(toolOutput);
                await _wvDeepSeek.CoreWebView2.ExecuteScriptAsync($"window.injectToolResult({escapedResult});");
            }
        }
        catch (Exception ex)
        {
            Log($"[Error parsing WebView2 Message]: {ex.Message}");
        }
    }

    private async Task<string> ExecuteToolAsync(string name, string relativePath, string content, string command, string target, string replacement, int startLine, int endLine)
    {
        if (string.IsNullOrEmpty(_workspacePath))
        {
            return "Error: Workspace has not been set yet. User must open a workspace folder first.";
        }

        string absolutePath = string.IsNullOrEmpty(relativePath) ? "" : Path.GetFullPath(Path.Combine(_workspacePath, relativePath));

        try
        {
            switch (name.ToLower())
            {
                case "list_directory":
                    return _toolSystem.ListDirectory(_workspacePath);

                case "read_file":
                    if (string.IsNullOrEmpty(absolutePath)) return "Error: Missing file path.";
                    return _toolSystem.ReadFile(absolutePath, startLine, endLine);

                case "write_file":
                    if (string.IsNullOrEmpty(absolutePath)) return "Error: Missing file path.";
                    string writeRes = _toolSystem.WriteFile(absolutePath, content);
                    RefreshFileTreeOnMainThread();
                    return writeRes;

                case "replace_text":
                    if (string.IsNullOrEmpty(absolutePath)) return "Error: Missing file path.";
                    return _toolSystem.ReplaceText(absolutePath, target, replacement);

                case "run_command":
                    if (string.IsNullOrEmpty(command)) return "Error: Missing command statement.";
                    _commandCts = new CancellationTokenSource();
                    string cmdResult = await _toolSystem.RunCommandAsync(command, _workspacePath, outStr => Log(outStr.TrimEnd()), _commandCts.Token);
                    RefreshFileTreeOnMainThread();
                    return cmdResult;

                case "execute_sql":
                    if (string.IsNullOrEmpty(command)) return "Error: Missing SQL query statement.";
                    return _toolSystem.ExecuteSql(_txtSqlConnection.Text, command);

                case "search_code":
                    if (string.IsNullOrEmpty(command)) return "Error: Missing search query.";
                    return _toolSystem.SearchCode(_workspacePath, command);

                case "web_fetch":
                    if (string.IsNullOrEmpty(command)) return "Error: Missing URL to fetch.";
                    return await _toolSystem.WebFetchAsync(command);

                case "get_db_schema":
                    return _toolSystem.GetDbSchema(_txtSqlConnection.Text);

                default:
                    return $"Error: Unknown tool '{name}' requested.";
            }
        }
        catch (Exception ex)
        {
            return $"Error executing tool {name}: {ex.Message}";
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

    private void ChkBuildMode_CheckedChanged(object? sender, EventArgs e)
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

        try
        {
            if (_wvDeepSeek.CoreWebView2 != null)
            {
                _wvDeepSeek.CoreWebView2.ExecuteScriptAsync($"window.buildModeDisabled = {!_chkBuildMode.Checked};");
            }
        }
        catch { }
    }

    private async void btnInitialize_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_workspacePath))
        {
            MessageBox.Show("Please open a workspace folder first.", "Workspace Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _chkBuildMode.Checked = true;

        string fileMap = _toolSystem.ListDirectory(_workspacePath);

        string activeFileContext = "";
        if (!string.IsNullOrEmpty(_activeFilePath) && File.Exists(_activeFilePath))
        {
            try
            {
                string relPath = Path.GetRelativePath(_workspacePath, _activeFilePath);
                string fileText = File.ReadAllText(_activeFilePath);
                activeFileContext = $"\n\n### ACTIVE FILE BEING EDITED:\n{relPath}\n\n[FILE CONTENT OF {Path.GetFileName(_activeFilePath)}]\n{fileText}";
            }
            catch { }
        }

        string prompt = $@"You are a local autonomous coding assistant connected to my developer workspace.
You can read and modify files, scan directories, and run commands.
My active workspace folder is: {_workspacePath}

Here is the current directory structure:
{fileMap}{activeFileContext}

    ### TIGHT TOOLKIT SPECIFICATIONS:
    You can invoke the following tools using XML blocks. You MUST wrap all tool calls inside a ```xml ... ``` code block. Output ONLY one tool block at a time, wait for the response (which will be returned as '### TOOL OUTPUT:'), and then decide the next step.

    1. List workspace directory files (returns file tree structures):
    ```xml
    <tool name=""list_directory""></tool>
    ```

    2. Read file contents (always specify a path; optional start_line and end_line for long files):
    ```xml
    <tool name=""read_file"">
      <path>relative/path/to/file.ext</path>
      <start_line>10</start_line>
      <end_line>50</end_line>
    </tool>
    ```

    3. Write a new file or fully overwrite an existing file:
    ```xml
    <tool name=""write_file"">
      <path>relative/path/to/file.ext</path>
      <content>
      full file content here
      </content>
    </tool>
    ```

    4. Replace a unique text block in an existing file:
    ```xml
    <tool name=""replace_text"">
      <path>relative/path/to/file.ext</path>
      <target>
      exact existing text block to replace
      </target>
      <replacement>
      new text block replacement
      </replacement>
    </tool>
    ```

    5. Run a shell command in the terminal (e.g. compile, test, or build):
    ```xml
    <tool name=""run_command"">
      <command>dotnet build</command>
    </tool>
    ```

    6. Execute SQL queries and commands on the active SQL Server database:
    ```xml
    <tool name=""execute_sql"">
      <command>SELECT TOP 10 * FROM INFORMATION_SCHEMA.TABLES</command>
    </tool>
    ```

    7. Search codebase (Grep) recursively for a query string:
    ```xml
    <tool name=""search_code"">
      <command>SearchQueryString</command>
    </tool>
    ```

    8. Fetch a URL and parse it to simplified text content (e.g. read API docs):
    ```xml
    <tool name=""web_fetch"">
      <command>https://example.com/docs</command>
    </tool>
    ```

    9. Export the database schema (tables, columns, types, primary keys) of the active SQL database:
    ```xml
    <tool name=""get_db_schema""></tool>
    ```

    ### RULES & GUIDELINES:
    - **One Action per Message**: Do not combine multiple tool calls in a single message.
    - **Wrap in Markdown Code Blocks**: Always wrap your XML tool block in a ```xml ... ``` code block. This is critical to ensure proper text parsing.
    - **Uniqueness Check**: When using replace_text, target must be completely unique. Include surrounding lines of context.
    - **No Indefinite Commands**: Never run commands that block indefinitely (like 'dotnet run' or 'npm run dev'). Only run build/compilation checks or tests.
    - **Thinking Process**: Start your response with a <think>...</think> block containing your reasoning.

    If you understand, please reply with a short confirmation summarizing the tools you have access to, and ask how you can help me.";

        Log("Injecting tool integration system prompt into DeepSeek chat...");
        string escapedPrompt = JsonSerializer.Serialize(prompt);
        
        string jsCode = $@"
(function() {{
    window.buildModeDisabled = false;
    function logToHost(msg) {{
        try {{
            window.chrome.webview.postMessage({{ type: 'log', message: '[Initialize JS] ' + msg }});
        }} catch(e) {{}}
    }}

    try {{
        logToHost('Script execution started.');

        function findChatInput() {{
            let el = document.querySelector('textarea');
            if (el) return el;
            el = document.querySelector('[contenteditable=""true""]');
            if (el) return el;
            const inputs = document.querySelectorAll('input, div, textarea');
            for (const input of inputs) {{
                const ph = input.getAttribute('placeholder') || '';
                if (ph.toLowerCase().includes('message') || ph.toLowerCase().includes('deepseek')) {{
                    return input;
                }}
            }}
            return null;
        }}

        const inputEl = findChatInput();
        if (!inputEl) {{
            logToHost('ERROR: Could not find chat input box element.');
            alert('Could not find chat input area. Make sure DeepSeek is fully loaded.');
            return;
        }}

        logToHost('Found chat input element: ' + inputEl.tagName + ' (id: ' + inputEl.id + ', class: ' + inputEl.className + ')');

        function insertTextIntoInput(el, text) {{
            el.focus();
            let inserted = false;
            try {{
                const selection = window.getSelection();
                if (el.tagName && (el.tagName.toLowerCase() === 'textarea' || el.tagName.toLowerCase() === 'input')) {{
                    el.select();
                }} else {{
                    const range = document.createRange();
                    range.selectNodeContents(el);
                    selection.removeAllRanges();
                    selection.addRange(range);
                }}
                
                if (document.execCommand('insertText', false, text)) {{
                    inserted = true;
                }}
            }} catch (err) {{
                logToHost('insertText error: ' + err.message);
            }}
            
            if (!inserted) {{
                if (el.tagName && (el.tagName.toLowerCase() === 'textarea' || el.tagName.toLowerCase() === 'input')) {{
                    el.value = text;
                }} else {{
                    el.innerText = text;
                }}
            }}
            el.dispatchEvent(new Event('input', {{ bubbles: true }}));
            el.dispatchEvent(new Event('change', {{ bubbles: true }}));
        }}

        insertTextIntoInput(inputEl, {escapedPrompt});
        logToHost('System prompt inserted successfully.');

        function findSendButton(el) {{
            const textareaRect = el.getBoundingClientRect();
            logToHost('Textarea Rect: left=' + textareaRect.left + ', top=' + textareaRect.top + ', width=' + textareaRect.width + ', height=' + textareaRect.height + ', bottom=' + textareaRect.bottom);
            if (textareaRect.width === 0) {{
                logToHost('findSendButton: textarea is not visible or has zero size.');
                return null;
            }}

            const buttons = Array.from(document.querySelectorAll('button, [role=""button""], div[class*=""send-button""]'));
            logToHost('Global query found ' + buttons.length + ' button/role candidates.');
            let nearbyButtons = [];
            
            for (let i = 0; i < buttons.length; i++) {{
                const btn = buttons[i];
                if (btn === el) continue;
                
                const rect = btn.getBoundingClientRect();
                if (rect.width === 0 || rect.height === 0) continue;
                
                const verticalDist = Math.abs(rect.bottom - textareaRect.bottom);
                const horizontalDist = rect.left - textareaRect.left;
                
                logToHost('Candidate ' + i + ': left=' + rect.left + ', bottom=' + rect.bottom + ', vDist=' + verticalDist + ', hDist=' + horizontalDist + ', text=""' + (btn.innerText || '').trim() + '""');

                if (verticalDist < 120 && horizontalDist > -20 && rect.top > textareaRect.top - 50) {{
                    nearbyButtons.push({{ btn: btn, rect: rect }});
                }}
            }}
            
            if (nearbyButtons.length === 0) {{
                logToHost('findSendButton: no nearby buttons matched geometric rules.');
                return null;
            }}
            
            nearbyButtons.sort((a, b) => a.rect.left - b.rect.left);
            
            const sendBtn = nearbyButtons[nearbyButtons.length - 1].btn;
            logToHost('findSendButton: selected button at left=' + nearbyButtons[nearbyButtons.length - 1].rect.left + ' (' + sendBtn.outerHTML.substring(0, 100) + ')');
            return sendBtn;
        }}

        setTimeout(() => {{
            try {{
                const sendButton = findSendButton(inputEl);
                if (sendButton) {{
                    logToHost('Clicking Send button...');
                    sendButton.click();
                }} else {{
                    logToHost('Send button not found. Using Enter key event fallback...');
                    const eventOpts = {{ bubbles: true, cancelable: true, key: 'Enter', code: 'Enter', keyCode: 13, which: 13, shiftKey: false, ctrlKey: false, altKey: false, metaKey: false }};
                    inputEl.dispatchEvent(new KeyboardEvent('keydown', eventOpts));
                    inputEl.dispatchEvent(new KeyboardEvent('keypress', eventOpts));
                    inputEl.dispatchEvent(new KeyboardEvent('keyup', eventOpts));
                }}
            }} catch(e) {{
                logToHost('Error in setTimeout click execution: ' + e.message);
            }}
        }}, 300);

    }} catch(err) {{
        logToHost('JS Runtime Error: ' + err.message + '\\nStack: ' + err.stack);
    }}
    }})();";

        try
        {
            await _wvDeepSeek.CoreWebView2.ExecuteScriptAsync(jsCode);
            Log("System prompt successfully sent to DeepSeek.");
        }
        catch (Exception ex)
        {
            Log($"[Error injecting prompt]: {ex.Message}");
        }
    }

    private async void btnRerunTool_Click(object? sender, EventArgs e)
    {
        Log("Requesting browser to locate and rerun the last tool call...");
        try
        {
            string js = "window.rerunLastToolCall();";
            string resultJson = await _wvDeepSeek.CoreWebView2.ExecuteScriptAsync(js);
            if (resultJson != null && resultJson != "null")
            {
                using var doc = JsonDocument.Parse(resultJson);
                string res = doc.RootElement.GetString() ?? "";
                if (!string.IsNullOrEmpty(res))
                {
                    Log($"[Rerun Tool] {res}");
                    if (res.StartsWith("Error"))
                    {
                        MessageBox.Show(res, "Rerun Tool Call", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[Error rerunning tool]: {ex.Message}");
        }
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
