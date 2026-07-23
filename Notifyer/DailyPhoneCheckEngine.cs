namespace Notifyer;

/// <summary>
/// Fixed wall-clock reminders to check the phone. Completely independent of
/// eye-rest pause / film mode / quiet apps — always fires on schedule when enabled.
/// </summary>
public sealed class DailyPhoneCheckEngine : IDisposable
{
    private readonly AppConfig _config;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly object _gate = new();
    private readonly HashSet<string> _firedToday = new(StringComparer.Ordinal);
    private DateTime _day = DateTime.Today;
    private bool _disposed;

    public DailyPhoneCheckEngine(AppConfig config)
    {
        _config = config;
        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += OnTick;

        lock (_gate)
            MarkPastSlotsAsFired(DateTime.Now);

        _timer.Start();
    }

    public void ReloadFromConfig()
    {
        lock (_gate)
        {
            // Keep today's fire state; only re-mark newly past slots if times changed.
            MarkPastSlotsAsFired(DateTime.Now);
        }
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
            if (!_config.PhoneCheckEnabled)
                return;

            var now = DateTime.Now;
            if (now.Date != _day)
            {
                _day = now.Date;
                _firedToday.Clear();
            }

            foreach (var slot in ParseSlots(_config.PhoneCheckTimes))
            {
                var key = SlotKey(slot);
                if (_firedToday.Contains(key))
                    continue;

                if (now >= _day + slot)
                    Fire(key);
            }
        }
    }

    private void Fire(string key)
    {
        _firedToday.Add(key);
        ToastService.Show(
            "Notifyer",
            _config.PhoneCheckMessage,
            _config.PhoneCheckSound,
            _config.PhoneCheckBypassDoNotDisturb,
            persistUntilDismissed: true);
    }

    /// <summary>
    /// App started mid-day: do not dump missed morning slots at once.
    /// </summary>
    private void MarkPastSlotsAsFired(DateTime now)
    {
        _day = now.Date;
        foreach (var slot in ParseSlots(_config.PhoneCheckTimes))
        {
            if (now >= _day + slot)
                _firedToday.Add(SlotKey(slot));
        }
    }

    private static string SlotKey(TimeSpan slot) =>
        $"{(int)slot.TotalHours:D2}:{slot.Minutes:D2}";

    internal static List<TimeSpan> ParseSlots(IEnumerable<string>? times)
    {
        var slots = new List<TimeSpan>();
        if (times is null)
            return slots;

        foreach (var raw in times)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            if (TimeSpan.TryParse(raw.Trim(), out var ts) &&
                ts >= TimeSpan.Zero &&
                ts < TimeSpan.FromDays(1))
            {
                slots.Add(new TimeSpan(ts.Hours, ts.Minutes, 0));
            }
        }

        return slots
            .Distinct()
            .OrderBy(t => t)
            .ToList();
    }
}
