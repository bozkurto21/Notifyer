using Notifyer.Rules;

namespace Notifyer;

public sealed class ReminderEngine : IDisposable
{
    private readonly AppConfig _config;
    private readonly QuietProcessRule _quietRule = new();
    private readonly FilmModeRule _filmRule = new();
    private readonly System.Windows.Forms.Timer _timer;
    private readonly object _gate = new();

    private DateTime _nextDueUtc;
    private bool _deferred;
    private bool _disposed;
    private int _statusTick;

    public event Action? StateChanged;

    public ReminderEngine(AppConfig config)
    {
        _config = config;
        _filmRule.SetEnabled(config.FilmModeEnabled);

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += OnTick;

        ScheduleNext();
        _timer.Start();
    }

    public bool IsPaused => _config.IsPaused;
    public bool FilmModeEnabled => _filmRule.Enabled;
    public bool IsDeferred => _deferred;
    public DateTime NextDueUtc => _nextDueUtc;

    public TimeSpan TimeUntilNext
    {
        get
        {
            var remaining = _nextDueUtc - DateTime.UtcNow;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }
    }

    public int CurrentIntervalMinutes =>
        _filmRule.Enabled ? _config.FilmModeMinutes : _config.IntervalMinutes;

    public void SetPaused(bool paused)
    {
        lock (_gate)
        {
            _config.IsPaused = paused;
            if (!paused && !_deferred)
                ScheduleNext();
        }

        RaiseStateChanged();
    }

    public void SetFilmMode(bool enabled)
    {
        lock (_gate)
        {
            _filmRule.SetEnabled(enabled);
            _config.FilmModeEnabled = enabled;
            _deferred = false;
            ScheduleNext();
        }

        RaiseStateChanged();
    }

    public void SetIntervalMinutes(int minutes)
    {
        lock (_gate)
        {
            _config.IntervalMinutes = Math.Max(1, minutes);
            ConfigStore.Save(_config);
            if (!_filmRule.Enabled)
            {
                _deferred = false;
                ScheduleNext();
            }
        }

        RaiseStateChanged();
    }

    public void ReloadQuietListFromDisk()
    {
        var fresh = ConfigStore.Load();
        lock (_gate)
        {
            _config.QuietProcesses = fresh.QuietProcesses;
            _config.Message = fresh.Message;
            _config.Sound = fresh.Sound;
            _config.BypassDoNotDisturb = fresh.BypassDoNotDisturb;
            _config.FilmModeMinutes = fresh.FilmModeMinutes;
            _config.IntervalMinutes = fresh.IntervalMinutes;
            _config.StartWithWindows = fresh.StartWithWindows;
        }

        RaiseStateChanged();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _timer.Stop();
        _timer.Dispose();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        lock (_gate)
        {
            if (_config.IsPaused)
                return;

            _statusTick++;
            if (_statusTick % 10 == 0)
                RaiseStateChanged();

            if (_deferred)
            {
                // Light poll while waiting for a quiet app to exit.
                if (_statusTick % 5 == 0 && !_quietRule.IsQuietActive(_config.QuietProcesses))
                    FireReminder(consumeFilmMode: true);

                return;
            }

            if (DateTime.UtcNow < _nextDueUtc)
                return;

            if (_quietRule.IsQuietActive(_config.QuietProcesses))
            {
                _deferred = true;
                RaiseStateChanged();
                return;
            }

            FireReminder(consumeFilmMode: true);
        }
    }

    private void FireReminder(bool consumeFilmMode)
    {
        ToastService.Show("Notifyer", _config.Message, _config.Sound, _config.BypassDoNotDisturb);

        _deferred = false;

        if (consumeFilmMode && _filmRule.Enabled)
        {
            _filmRule.ConsumeAfterFire();
            _config.FilmModeEnabled = false;
        }

        ScheduleNext();
        RaiseStateChanged();
    }

    private void ScheduleNext()
    {
        var minutes = _filmRule.Enabled ? _config.FilmModeMinutes : _config.IntervalMinutes;
        _nextDueUtc = DateTime.UtcNow.AddMinutes(minutes);
    }

    private void RaiseStateChanged()
    {
        // Marshal to UI thread if needed — Timer already on UI thread for WinForms.
        StateChanged?.Invoke();
    }
}
