using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace AnimalTower;

public class ContactForm : Form
{
    private ComboBox _categoryBox;
    private TextBox _messageBox;
    private Button _sendButton;
    private Button _copyButton;
    private Button _closeButton;
    private Label _statusLabel;

    public ContactForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        // Form Settings
        this.Text = "お問い合わせ (Contact)";
        this.Size = new Size(500, 450);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Font = new Font("Segoe UI", 10f);

        // Main Layout
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(20),
            AutoSize = true
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Title
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Category Label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Category Combo
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Message Label
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Message Box
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); // Buttons

        // Title
        var titleLabel = new Label
        {
            Text = "ご意見・お問い合わせ", // "We value your feedback!"
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(60, 60, 60),
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };

        // Category Selection
        var categoryLabel = new Label { Text = "カテゴリ (Category):", AutoSize = true, Anchor = AnchorStyles.BottomLeft };
        _categoryBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 300,
            Anchor = AnchorStyles.Left
        };
        // Categories: Bug Report, Feature Request, Other
        _categoryBox.Items.AddRange(new object[] { "不具合報告 (Bug)", "機能要望 (Feature)", "その他 (Other)" });
        _categoryBox.SelectedIndex = 0;

        // Message Input
        var messageLabel = new Label { Text = "内容 (Message):", AutoSize = true, Anchor = AnchorStyles.BottomLeft };
        _messageBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical
        };
        _messageBox.TextChanged += (s, e) => ValidateInput();

        // Button Panel
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        _closeButton = new Button { Text = "閉じる", Size = new Size(80, 35) }; // Close
        _closeButton.Click += (s, e) => this.Close();

        _copyButton = new Button { Text = "クリップボードにコピー", Size = new Size(180, 35) }; // Copy to Clipboard
        _copyButton.Click += CopyToClipboard_Click;

        _sendButton = new Button
        {
            Text = "メールを作成", // Create Email
            Size = new Size(120, 35),
            BackColor = Color.SteelBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _sendButton.FlatAppearance.BorderSize = 0;
        _sendButton.Click += SendEmail_Click;

        buttonPanel.Controls.Add(_closeButton);
        buttonPanel.Controls.Add(_copyButton);
        buttonPanel.Controls.Add(_sendButton);

        // Status Label (hidden initially)
        _statusLabel = new Label
        {
            Text = "",
            ForeColor = Color.Green,
            AutoSize = true,
            Dock = DockStyle.Bottom
        };

        // Add controls to layout
        mainLayout.Controls.Add(titleLabel, 0, 0);
        mainLayout.Controls.Add(categoryLabel, 0, 1);
        mainLayout.Controls.Add(_categoryBox, 0, 2);
        mainLayout.Controls.Add(messageLabel, 0, 3);
        mainLayout.Controls.Add(_messageBox, 0, 4);
        mainLayout.Controls.Add(buttonPanel, 0, 5);

        this.Controls.Add(mainLayout);
        this.Controls.Add(_statusLabel); // Add separately to stick to bottom if needed, or just handle via popup

        ValidateInput();
    }

    private void ValidateInput()
    {
        bool hasText = !string.IsNullOrWhiteSpace(_messageBox.Text);
        _sendButton.Enabled = hasText;
        _copyButton.Enabled = hasText;

        if (hasText)
        {
            _sendButton.BackColor = Color.SteelBlue;
        }
        else
        {
            _sendButton.BackColor = Color.LightGray;
        }
    }

    private string GetFormattedBody()
    {
        return $"[Category: {_categoryBox.SelectedItem}]\n\n{_messageBox.Text}";
    }

    private void SendEmail_Click(object? sender, EventArgs e)
    {
        try
        {
            string subject = Uri.EscapeDataString($"[Animal Tower] Feedback: {_categoryBox.SelectedItem}");
            string body = Uri.EscapeDataString(_messageBox.Text);

            // Note: Replace with actual support email
            string mailtoUrl = $"mailto:support@animaltower.com?subject={subject}&body={body}";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = mailtoUrl,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception)
        {
            MessageBox.Show("メールソフトを起動できませんでした。「クリップボードにコピー」をご利用ください。\n(Could not open default mail client.)", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopyToClipboard_Click(object? sender, EventArgs e)
    {
        try
        {
            Clipboard.SetText(GetFormattedBody());
            MessageBox.Show("内容をクリップボードにコピーしました！\n(Content copied to clipboard!)", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception)
        {
            MessageBox.Show("コピーに失敗しました。\n(Failed to copy.)", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
