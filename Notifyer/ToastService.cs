using Microsoft.Toolkit.Uwp.Notifications;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Notifyer;

public static class ToastService
{
    public static void Show(string title, string message, string soundId, bool bypassDoNotDisturb)
    {
        var sound = ToastSounds.Resolve(soundId);
        var builder = new ToastContentBuilder()
            .AddText(title)
            .AddText(message);

        if (sound.Id == ToastSounds.Off || sound.Uri is null)
        {
            builder.AddAudio(new ToastAudio { Silent = true });
        }
        else
        {
            builder.AddAudio(new ToastAudio
            {
                Src = new Uri(sound.Uri),
                // Looping alarm/call sounds only loop while the toast is on screen.
                Loop = sound.Uri.Contains(".Looping.", StringComparison.Ordinal)
            });
        }

        if (bypassDoNotDisturb)
        {
            // Dismiss action helps alarm-like toasts behave correctly on Focus Assist.
            builder.AddButton(new ToastButton()
                .SetContent("Tamam")
                .AddArgument("action", "dismiss")
                .SetDismissActivation());
        }

        var xml = builder.GetToastContent().GetXml();

        if (bypassDoNotDisturb)
        {
            // "urgent" = Important notification — breaks through Do Not Disturb / game DND
            // on Windows 10 19041+ and Windows 11. Toolkit has no enum for this yet.
            xml.DocumentElement?.SetAttribute("scenario", "urgent");
        }

        var toast = new ToastNotification(xml)
        {
            ExpirationTime = DateTimeOffset.Now.AddMinutes(5),
            Priority = ToastNotificationPriority.High
        };

        ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
    }
}
