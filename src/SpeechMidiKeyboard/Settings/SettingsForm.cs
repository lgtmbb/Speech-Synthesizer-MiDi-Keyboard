using SpeechMidiKeyboard.Services;

namespace SpeechMidiKeyboard.Settings;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly ComboBox _engineCombo;
    private readonly ComboBox _voiceCombo;
    private readonly NumericUpDown _rateUpDown;
    private readonly NumericUpDown _volumeUpDown;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;

        Text = "Beállítások";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 260);
        AccessibleName = "Beállítások";

        var engineLabel = new Label { Text = "&Motor:", Location = new Point(12, 15), AutoSize = true, TabIndex = 0, TabStop = false };
        _engineCombo = new ComboBox
        {
            Location = new Point(140, 12),
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList,
            AccessibleName = "Beszédmotor",
            TabIndex = 1
        };
        _engineCombo.Items.AddRange(new object[] { "SAPI5", "SAPI4 (nem támogatott)", "OneCore" });
        _engineCombo.SelectedIndex = (int)_settings.Engine;
        _engineCombo.SelectedIndexChanged += (_, _) => RepopulateVoices();

        var voiceLabel = new Label { Text = "&Hang:", Location = new Point(12, 50), AutoSize = true, TabIndex = 2, TabStop = false };
        _voiceCombo = new ComboBox
        {
            Location = new Point(140, 47),
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList,
            AccessibleName = "Hang kiválasztása",
            TabIndex = 3
        };

        var rateLabel = new Label { Text = "&Sebesség (-10..10):", Location = new Point(12, 90), AutoSize = true, TabIndex = 4, TabStop = false };
        _rateUpDown = new NumericUpDown
        {
            Location = new Point(190, 87),
            Width = 80,
            Minimum = -10,
            Maximum = 10,
            Value = Math.Clamp(_settings.Rate, -10, 10),
            AccessibleName = "Beszédsebesség",
            TabIndex = 5
        };

        var volumeLabel = new Label { Text = "&Hangerő (0..100):", Location = new Point(12, 125), AutoSize = true, TabIndex = 6, TabStop = false };
        _volumeUpDown = new NumericUpDown
        {
            Location = new Point(190, 122),
            Width = 80,
            Minimum = 0,
            Maximum = 100,
            Value = Math.Clamp(_settings.Volume, 0, 100),
            AccessibleName = "Hangerő",
            TabIndex = 7
        };

        var okButton = new Button
        {
            Text = "&Mentés",
            DialogResult = DialogResult.OK,
            Location = new Point(190, 210),
            Size = new Size(100, 32),
            TabIndex = 8
        };
        okButton.Click += (_, _) => SaveAndClose();

        var cancelButton = new Button
        {
            Text = "&Mégse",
            DialogResult = DialogResult.Cancel,
            Location = new Point(300, 210),
            Size = new Size(100, 32),
            TabIndex = 9
        };

        Controls.AddRange(new Control[]
        {
            engineLabel, _engineCombo, voiceLabel, _voiceCombo,
            rateLabel, _rateUpDown, volumeLabel, _volumeUpDown,
            okButton, cancelButton
        });
        AcceptButton = okButton;
        CancelButton = cancelButton;

        RepopulateVoices();
    }

    private void RepopulateVoices()
    {
        _voiceCombo.Items.Clear();
        var kind = (VoiceEngineKind)_engineCombo.SelectedIndex;

        if (kind == VoiceEngineKind.Sapi4)
        {
            _voiceCombo.Items.Add("SAPI4 nem elérhető ezen a Windows verzión");
            _voiceCombo.SelectedIndex = 0;
            _voiceCombo.Enabled = false;
            return;
        }

        _voiceCombo.Enabled = true;
        using IVoiceEngine engine = VoiceEngineFactory.Create(kind);
        foreach (var voice in engine.GetVoices())
        {
            _voiceCombo.Items.Add(voice);
        }

        if (_voiceCombo.Items.Count == 0)
        {
            _voiceCombo.Items.Add("Nincs telepített hang ehhez a motorhoz");
            _voiceCombo.SelectedIndex = 0;
            _voiceCombo.Enabled = false;
            return;
        }

        var indexToSelect = 0;
        for (var i = 0; i < _voiceCombo.Items.Count; i++)
        {
            if (_voiceCombo.Items[i] is VoiceInfo vi && vi.Id == _settings.VoiceName)
            {
                indexToSelect = i;
                break;
            }
        }
        _voiceCombo.SelectedIndex = indexToSelect;
    }

    private void SaveAndClose()
    {
        _settings.Engine = (VoiceEngineKind)_engineCombo.SelectedIndex;
        _settings.VoiceName = _voiceCombo.SelectedItem is VoiceInfo vi ? vi.Id : null;
        _settings.Rate = (int)_rateUpDown.Value;
        _settings.Volume = (int)_volumeUpDown.Value;
        _settings.Save();
    }
}
