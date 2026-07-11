namespace Notifyer;

public sealed class TrayApp : ApplicationContext
{
    private readonly AppConfig _config;
    private readonly ReminderEngine _engine;
    private readonly NotifyIcon _tray;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _filmItem;
    private readonly ToolStripMenuItem _soundMenu;
    private readonly ToolStripMenuItem _bypassDndItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _statusItem;
    private readonly Icon _icon;

    public TrayApp(AppConfig config)
    {
        _config = config;
        _engine = new ReminderEngine(config);
        _engine.StateChanged += UpdateStatusText;

        _icon = CreateTrayIcon();

        _statusItem = new ToolStripMenuItem("Durum") { Enabled = false };
        _pauseItem = new ToolStripMenuItem("Duraklat", null, OnTogglePause);
        _filmItem = new ToolStripMenuItem("Film Modu", null, OnToggleFilmMode) { CheckOnClick = true };
        _soundMenu = BuildSoundMenu();
        _bypassDndItem = new ToolStripMenuItem("Do Not Disturb’ı aş", null, OnToggleBypassDnd)
        {
            CheckOnClick = true,
            Checked = config.BypassDoNotDisturb
        };
        _startupItem = new ToolStripMenuItem("Windows ile başlat", null, OnToggleStartup)
        {
            CheckOnClick = true,
            Checked = config.StartWithWindows
        };

        var intervalMenu = new ToolStripMenuItem("Aralık");
        foreach (var minutes in new[] { 15, 20, 25, 30, 45, 60 })
        {
            var m = minutes;
            var item = new ToolStripMenuItem($"{m} dk", null, (_, _) => SetInterval(m))
            {
                Checked = config.IntervalMinutes == m,
                Tag = m
            };
            intervalMenu.DropDownItems.Add(item);
        }

        var customItem = new ToolStripMenuItem("Özel…", null, OnCustomInterval);
        intervalMenu.DropDownItems.Add(new ToolStripSeparator());
        intervalMenu.DropDownItems.Add(customItem);

        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_pauseItem);
        menu.Items.Add(_filmItem);
        menu.Items.Add(intervalMenu);
        menu.Items.Add(_soundMenu);
        menu.Items.Add(_bypassDndItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Config’i aç", null, OnOpenConfig));
        menu.Items.Add(new ToolStripMenuItem("Config’i yeniden yükle", null, OnReloadConfig));
        menu.Items.Add(new ToolStripMenuItem("Test bildirimi", null, OnTestToast));
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Çıkış", null, OnExit));

        _tray = new NotifyIcon
        {
            Icon = _icon,
            Text = "Notifyer",
            Visible = true,
            ContextMenuStrip = menu
        };

        _tray.DoubleClick += (_, _) => OnOpenConfig(null, EventArgs.Empty);

        StartupService.Apply(config.StartWithWindows);
        UpdateStatusText();
    }

    private ToolStripMenuItem BuildSoundMenu()
    {
        var menu = new ToolStripMenuItem("Ses");
        string? lastGroup = null;

        foreach (var option in ToastSounds.All)
        {
            var group = option.Id switch
            {
                ToastSounds.Off => "off",
                ToastSounds.Default or "im" or "mail" or "reminder" or "sms" => "basic",
                _ when option.Id.StartsWith("alarm", StringComparison.Ordinal) => "alarm",
                _ => "call"
            };

            if (lastGroup is not null && lastGroup != group)
                menu.DropDownItems.Add(new ToolStripSeparator());
            lastGroup = group;

            var id = option.Id;
            var item = new ToolStripMenuItem(option.Label, null, (_, _) => SetSound(id))
            {
                Checked = option.Id.Equals(_config.Sound, StringComparison.OrdinalIgnoreCase),
                Tag = option.Id
            };
            menu.DropDownItems.Add(item);
        }

        return menu;
    }

    private void SetSound(string soundId)
    {
        _config.Sound = ToastSounds.Resolve(soundId).Id;
        ConfigStore.Save(_config);
        RefreshSoundMenuChecks();

        // Preview so the user hears the chosen system sound immediately.
        ToastService.Show(
            "Notifyer",
            $"Ses: {ToastSounds.Resolve(_config.Sound).Label}",
            _config.Sound,
            bypassDoNotDisturb: false);
    }

    private void RefreshSoundMenuChecks()
    {
        foreach (ToolStripItem item in _soundMenu.DropDownItems)
        {
            if (item is ToolStripMenuItem mi && mi.Tag is string id)
                mi.Checked = id.Equals(_config.Sound, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void SetInterval(int minutes)
    {
        _engine.SetIntervalMinutes(minutes);
        if (_tray.ContextMenuStrip is null)
            return;

        foreach (ToolStripItem item in _tray.ContextMenuStrip.Items)
        {
            if (item is not ToolStripMenuItem { Text: "Aralık" } intervalMenu)
                continue;

            foreach (ToolStripItem sub in intervalMenu.DropDownItems)
            {
                if (sub is ToolStripMenuItem mi && mi.Tag is int tag)
                    mi.Checked = tag == minutes;
            }
        }
    }

    private void OnCustomInterval(object? sender, EventArgs e)
    {
        using var form = new Form
        {
            Text = "Aralık (dakika)",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            ClientSize = new Size(260, 100),
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = true,
            TopMost = true
        };

        var input = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 24 * 60,
            Value = Math.Clamp(_config.IntervalMinutes, 1, 24 * 60),
            Location = new Point(20, 20),
            Width = 220
        };

        var ok = new Button
        {
            Text = "Tamam",
            DialogResult = DialogResult.OK,
            Location = new Point(90, 55),
            Width = 80
        };

        form.Controls.Add(input);
        form.Controls.Add(ok);
        form.AcceptButton = ok;

        if (form.ShowDialog() == DialogResult.OK)
            SetInterval((int)input.Value);
    }

    private void OnTogglePause(object? sender, EventArgs e)
    {
        _engine.SetPaused(!_engine.IsPaused);
    }

    private void OnToggleFilmMode(object? sender, EventArgs e)
    {
        _engine.SetFilmMode(_filmItem.Checked);
    }

    private void OnToggleBypassDnd(object? sender, EventArgs e)
    {
        _config.BypassDoNotDisturb = _bypassDndItem.Checked;
        ConfigStore.Save(_config);
    }

    private void OnToggleStartup(object? sender, EventArgs e)
    {
        _config.StartWithWindows = _startupItem.Checked;
        ConfigStore.Save(_config);
        StartupService.Apply(_config.StartWithWindows);
    }

    private void OnOpenConfig(object? sender, EventArgs e) => ConfigStore.OpenInEditor();

    private void OnTestToast(object? sender, EventArgs e)
    {
        ToastService.Show("Notifyer", _config.Message, _config.Sound, _config.BypassDoNotDisturb);
    }

    private void OnReloadConfig(object? sender, EventArgs e)
    {
        _engine.ReloadQuietListFromDisk();
        _bypassDndItem.Checked = _config.BypassDoNotDisturb;
        _startupItem.Checked = _config.StartWithWindows;
        RefreshSoundMenuChecks();
        StartupService.Apply(_config.StartWithWindows);
        SetInterval(_config.IntervalMinutes);
        UpdateStatusText();
        _tray.ShowBalloonTip(2000, "Notifyer", "Config yeniden yüklendi.", ToolTipIcon.Info);
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _tray.Visible = false;
        _engine.Dispose();
        _tray.Dispose();
        _icon.Dispose();
        Application.Exit();
    }

    private void UpdateStatusText()
    {
        if (_tray.ContextMenuStrip?.InvokeRequired == true)
        {
            _tray.ContextMenuStrip.BeginInvoke(UpdateStatusText);
            return;
        }

        _filmItem.Checked = _engine.FilmModeEnabled;
        _pauseItem.Text = _engine.IsPaused ? "Devam et" : "Duraklat";

        string status;
        if (_engine.IsPaused)
            status = $"Duraklatıldı — kalan: {FormatRemaining(_engine.TimeUntilNext)}";
        else if (_engine.IsDeferred)
            status = "Sessiz uygulama — çıkışta hatırlatılacak";
        else if (_engine.FilmModeEnabled)
            status = $"Film modu — sonraki: {FormatRemaining(_engine.TimeUntilNext)}";
        else
            status = $"Sonraki: {FormatRemaining(_engine.TimeUntilNext)} ({_engine.CurrentIntervalMinutes} dk)";

        _statusItem.Text = status;
        _tray.Text = status.Length <= 63 ? status : status[..63];
    }

    private static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining.TotalHours >= 1)
            return $"{(int)remaining.TotalHours}s {remaining.Minutes}dk";
        if (remaining.TotalMinutes >= 1)
            return $"{(int)remaining.TotalMinutes}dk {remaining.Seconds}sn";
        return $"{remaining.Seconds}sn";
    }

    private static Icon CreateTrayIcon()
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var brush = new SolidBrush(Color.FromArgb(32, 120, 180));
            g.FillEllipse(brush, 1, 1, 14, 14);
            using var eye = new SolidBrush(Color.White);
            g.FillEllipse(eye, 4, 5, 8, 6);
            using var pupil = new SolidBrush(Color.FromArgb(20, 40, 70));
            g.FillEllipse(pupil, 6, 6, 4, 4);
        }

        var hIcon = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        bmp.Dispose();
        return icon;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _engine.Dispose();
            _tray.Dispose();
            _icon.Dispose();
        }

        base.Dispose(disposing);
    }
}
