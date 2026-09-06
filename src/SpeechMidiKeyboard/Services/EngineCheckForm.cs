namespace SpeechMidiKeyboard.Services;

/// <summary>
/// Shown at startup only when something is missing. Fully keyboard/NVDA operable:
/// plain WinForms Label + Buttons, Tab order set explicitly, default/cancel buttons wired.
/// </summary>
public sealed class EngineCheckForm : Form
{
    public EngineCheckForm(EngineAvailabilityReport report, VoiceInstallChecker checker)
    {
        Text = "Beszédhangok ellenőrzése";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(460, 220);
        AccessibleName = "Beszédhangok ellenőrzése";

        var messageLines = new List<string>();
        if (!report.Sapi5Available)
            messageLines.Add("• Nincs telepítve egyetlen SAPI5 hang sem.");
        if (!report.OneCoreAvailable)
            messageLines.Add("• Nincs telepítve egyetlen OneCore (modern Windows) hang sem.");
        if (!report.Sapi4Available)
            messageLines.Add("• SAPI4 (régi) motor nem található ezen a gépen. A Microsoft ezt " +
                              "már nem terjeszti hivatalosan Windows 10/11 alá, ezért a program " +
                              "ezt nem tudja automatikusan feltelepíteni. A program SAPI5/OneCore " +
                              "hangokkal is teljes értékűen működik.");

        var label = new Label
        {
            Text = "A program elindult, de a következő hiányosságokat találta:\r\n\r\n" +
                   string.Join("\r\n", messageLines) +
                   "\r\n\r\nMegnyitod most a Windows beszédhang-beállításokat a telepítéshez?",
            Location = new Point(12, 12),
            Size = new Size(436, 150),
            AccessibleName = "Ellenőrzés eredménye",
            TabIndex = 0,
            TabStop = false
        };

        var yesButton = new Button
        {
            Text = "&Igen, megnyitás",
            DialogResult = DialogResult.Yes,
            Location = new Point(190, 175),
            Size = new Size(140, 32),
            TabIndex = 1
        };
        yesButton.Click += (_, _) =>
        {
            if (!report.Sapi5Available || !report.OneCoreAvailable)
            {
                checker.OpenWindowsSpeechSettings();
            }
        };

        var noButton = new Button
        {
            Text = "&Nem, most nem",
            DialogResult = DialogResult.No,
            Location = new Point(336, 175),
            Size = new Size(112, 32),
            TabIndex = 2
        };

        Controls.Add(label);
        Controls.Add(yesButton);
        Controls.Add(noButton);
        AcceptButton = yesButton;
        CancelButton = noButton;
    }
}
