namespace SpeechMidiKeyboard;

public sealed class TextPromptForm : Form
{
    private readonly TextBox _textBox;
    public string EnteredText => _textBox.Text;

    public TextPromptForm(string title, string prompt, int? maxLength = null)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(360, 120);

        var label = new Label { Text = prompt, Location = new Point(12, 12), AutoSize = true, TabStop = false };
        _textBox = new TextBox
        {
            Location = new Point(12, 36),
            Width = 320,
            AccessibleName = prompt
        };
        if (maxLength.HasValue)
        {
            _textBox.MaxLength = maxLength.Value;
        }

        var okButton = new Button
        {
            Text = "&OK",
            DialogResult = DialogResult.OK,
            Location = new Point(172, 75),
            Size = new Size(80, 30)
        };
        var cancelButton = new Button
        {
            Text = "&Mégse",
            DialogResult = DialogResult.Cancel,
            Location = new Point(258, 75),
            Size = new Size(80, 30)
        };

        Controls.Add(label);
        Controls.Add(_textBox);
        Controls.Add(okButton);
        Controls.Add(cancelButton);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}
