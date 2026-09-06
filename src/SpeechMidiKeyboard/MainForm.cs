using SpeechMidiKeyboard.Keyboard;
using SpeechMidiKeyboard.Services;
using SpeechMidiKeyboard.Settings;

namespace SpeechMidiKeyboard;

public sealed class MainForm : Form
{
    private readonly AppSettings _settings;
    private IVoiceEngine _engine;
    private readonly TextBox _bufferBox;
    private readonly TextBox _freeTextBox;

    public MainForm(AppSettings settings)
    {
        _settings = settings;
        _engine = VoiceEngineFactory.Create(_settings.Engine);

        Text = "Speech Synthesizer MIDI Keyboard";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(640, 360);
        AccessibleName = "Speech Synthesizer MIDI Keyboard főablak";

        var menu = BuildMenu();
        MainMenuStrip = menu;
        Controls.Add(menu);

        var bufferLabel = new Label
        {
            Text = "Szótag/betű puffer (ezt szólaltatja meg a MIDI billentyűzet):",
            Location = new Point(12, 40),
            AutoSize = true
        };
        _bufferBox = new TextBox
        {
            Location = new Point(12, 62),
            Width = 600,
            Text = _settings.CurrentBuffer,
            AccessibleName = "Szótag/betű puffer"
        };
        _bufferBox.TextChanged += (_, _) =>
        {
            _settings.CurrentBuffer = _bufferBox.Text;
            _settings.Save();
        };

        var freeTextLabel = new Label
        {
            Text = "Kimondandó szöveg:",
            Location = new Point(12, 100),
            AutoSize = true
        };
        _freeTextBox = new TextBox
        {
            Location = new Point(12, 122),
            Width = 600,
            Height = 120,
            Multiline = true,
            AccessibleName = "Kimondandó szöveg"
        };

        var speakButton = new Button
        {
            Text = "&Kimondás",
            Location = new Point(12, 250),
            Size = new Size(120, 32),
            AccessibleName = "Szöveg kimondása"
        };
        speakButton.Click += (_, _) =>
            _engine.Speak(_freeTextBox.Text, _settings.VoiceName, _settings.Rate, _settings.Volume, 0);

        var keyboardButton = new Button
        {
            Text = "MIDI &billentyűzet (Ctrl+K)",
            Location = new Point(150, 250),
            Size = new Size(200, 32),
            AccessibleName = "MIDI billentyűzet megnyitása"
        };
        keyboardButton.Click += (_, _) => OpenKeyboard();

        var settingsButton = new Button
        {
            Text = "&Beállítások",
            Location = new Point(370, 250),
            Size = new Size(140, 32),
            AccessibleName = "Beállítások megnyitása"
        };
        settingsButton.Click += (_, _) => OpenSettings();

        Controls.Add(bufferLabel);
        Controls.Add(_bufferBox);
        Controls.Add(freeTextLabel);
        Controls.Add(_freeTextBox);
        Controls.Add(speakButton);
        Controls.Add(keyboardButton);
        Controls.Add(settingsButton);

        KeyPreview = true;
        KeyDown += MainForm_KeyDown;
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip { AccessibleName = "Főmenü" };

        var fileMenu = new ToolStripMenuItem("&Fájl");
        var exitItem = new ToolStripMenuItem("&Kilépés", null, (_, _) => Close())
        {
            ShortcutKeys = Keys.Alt | Keys.F4
        };
        fileMenu.DropDownItems.Add(exitItem);

        var toolsMenu = new ToolStripMenuItem("&Eszközök");
        var settingsItem = new ToolStripMenuItem("&Beállítások...", null, (_, _) => OpenSettings())
        {
            ShortcutKeys = Keys.Control | Keys.OemPeriod,
            ShortcutKeyDisplayString = "Ctrl+."
        };
        var keyboardItem = new ToolStripMenuItem("MIDI &billentyűzet", null, (_, _) => OpenKeyboard())
        {
            ShortcutKeys = Keys.Control | Keys.K
        };
        var addSyllableItem = new ToolStripMenuItem("&Szótag hozzáadása a pufferhez...", null, (_, _) => AddSyllable())
        {
            ShortcutKeys = Keys.Control | Keys.D1,
            ShortcutKeyDisplayString = "Ctrl+1"
        };
        var addLetterItem = new ToolStripMenuItem("&Betű hozzáadása a pufferhez...", null, (_, _) => AddLetter())
        {
            ShortcutKeys = Keys.Control | Keys.D2,
            ShortcutKeyDisplayString = "Ctrl+2"
        };
        toolsMenu.DropDownItems.Add(settingsItem);
        toolsMenu.DropDownItems.Add(keyboardItem);
        toolsMenu.DropDownItems.Add(addSyllableItem);
        toolsMenu.DropDownItems.Add(addLetterItem);

        var helpMenu = new ToolStripMenuItem("&Súgó");
        var docsItem = new ToolStripMenuItem("&Dokumentáció és billentyűparancsok...", null, (_, _) => ShowHelp());
        var aboutItem = new ToolStripMenuItem("&Névjegy...", null, (_, _) => ShowAbout());
        helpMenu.DropDownItems.Add(docsItem);
        helpMenu.DropDownItems.Add(aboutItem);

        menu.Items.Add(fileMenu);
        menu.Items.Add(toolsMenu);
        menu.Items.Add(helpMenu);
        return menu;
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.K)
        {
            OpenKeyboard();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.D1)
        {
            AddSyllable();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.D2)
        {
            AddLetter();
            e.Handled = true;
        }
    }

    private void OpenKeyboard()
    {
        using var keyboardForm = new PianoKeyboardForm(_engine, _settings);
        keyboardForm.ShowDialog(this);
    }

    private void OpenSettings()
    {
        using var settingsForm = new SettingsForm(_settings);
        if (settingsForm.ShowDialog(this) == DialogResult.OK)
        {
            _engine.Dispose();
            _engine = VoiceEngineFactory.Create(_settings.Engine);
        }
    }

    private void AddSyllable()
    {
        using var prompt = new TextPromptForm("Szótag hozzáadása", "Add meg a hozzáadandó szótagot:");
        if (prompt.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(prompt.EnteredText))
        {
            _bufferBox.Text += prompt.EnteredText;
        }
    }

    private void AddLetter()
    {
        using var prompt = new TextPromptForm("Betű hozzáadása", "Add meg a hozzáadandó betűt:", maxLength: 1);
        if (prompt.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(prompt.EnteredText))
        {
            _bufferBox.Text += prompt.EnteredText;
        }
    }

    private static void ShowHelp()
    {
        MessageBox.Show(
            "Billentyűparancsok:\r\n" +
            "Ctrl+K: MIDI billentyűzet megnyitása\r\n" +
            "Ctrl+1: szótag hozzáadása a pufferhez\r\n" +
            "Ctrl+2: betű hozzáadása a pufferhez\r\n\r\n" +
            "A MIDI billentyűzeten belül:\r\n" +
            "Z S X D C V G B H N J M , L . : hangok lejátszása\r\n" +
            "Jobb Shift: hangmagasság feljebb bend\r\n" +
            "Bal Shift: hangmagasság lejjebb bend\r\n" +
            "0-9: oktáv váltás\r\n" +
            "F2-F12: hangmagasság-eltolási pont kiválasztása\r\n" +
            "Escape: kilépés a billentyűzetből, alapértékek visszaállítása\r\n\r\n" +
            "A teljes dokumentáció és az inspirációk listája a projekt README.md-jében található.",
            "Dokumentáció",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void ShowAbout()
    {
        MessageBox.Show(
            "Speech Synthesizer MIDI Keyboard\r\n" +
            "Ihletforrások: Gakuen Pocket Miku, VocalWriter, UTAU, böngészős JS szótag-billentyűzetek, " +
            "a hallhassam/DEX projekt (infoalap.hu).",
            "Névjegy",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _engine.Dispose();
        base.OnFormClosed(e);
    }
}
