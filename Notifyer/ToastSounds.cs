namespace Notifyer;

/// <summary>
/// Built-in Windows toast sounds (ms-winsoundevent).
/// </summary>
public static class ToastSounds
{
    public const string Off = "off";
    public const string Default = "default";

    public sealed record Option(string Id, string Label, string? Uri);

    public static IReadOnlyList<Option> All { get; } =
    [
        new(Off, "Kapalı", null),
        new(Default, "Default", "ms-winsoundevent:Notification.Default"),
        new("im", "IM", "ms-winsoundevent:Notification.IM"),
        new("mail", "Mail", "ms-winsoundevent:Notification.Mail"),
        new("reminder", "Reminder", "ms-winsoundevent:Notification.Reminder"),
        new("sms", "SMS", "ms-winsoundevent:Notification.SMS"),
        new("alarm", "Alarm", "ms-winsoundevent:Notification.Looping.Alarm"),
        new("alarm2", "Alarm 2", "ms-winsoundevent:Notification.Looping.Alarm2"),
        new("alarm3", "Alarm 3", "ms-winsoundevent:Notification.Looping.Alarm3"),
        new("alarm4", "Alarm 4", "ms-winsoundevent:Notification.Looping.Alarm4"),
        new("alarm5", "Alarm 5", "ms-winsoundevent:Notification.Looping.Alarm5"),
        new("alarm6", "Alarm 6", "ms-winsoundevent:Notification.Looping.Alarm6"),
        new("alarm7", "Alarm 7", "ms-winsoundevent:Notification.Looping.Alarm7"),
        new("alarm8", "Alarm 8", "ms-winsoundevent:Notification.Looping.Alarm8"),
        new("alarm9", "Alarm 9", "ms-winsoundevent:Notification.Looping.Alarm9"),
        new("alarm10", "Alarm 10", "ms-winsoundevent:Notification.Looping.Alarm10"),
        new("call", "Call", "ms-winsoundevent:Notification.Looping.Call"),
        new("call2", "Call 2", "ms-winsoundevent:Notification.Looping.Call2"),
        new("call3", "Call 3", "ms-winsoundevent:Notification.Looping.Call3"),
        new("call4", "Call 4", "ms-winsoundevent:Notification.Looping.Call4"),
        new("call5", "Call 5", "ms-winsoundevent:Notification.Looping.Call5"),
        new("call6", "Call 6", "ms-winsoundevent:Notification.Looping.Call6"),
        new("call7", "Call 7", "ms-winsoundevent:Notification.Looping.Call7"),
        new("call8", "Call 8", "ms-winsoundevent:Notification.Looping.Call8"),
        new("call9", "Call 9", "ms-winsoundevent:Notification.Looping.Call9"),
        new("call10", "Call 10", "ms-winsoundevent:Notification.Looping.Call10"),
    ];

    public static Option Resolve(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return All.First(o => o.Id == Default);

        var match = All.FirstOrDefault(o =>
            o.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? All.First(o => o.Id == Default);
    }
}
