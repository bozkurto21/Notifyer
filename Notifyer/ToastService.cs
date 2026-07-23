using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace Notifyer;

public static class ToastService
{
    public static void Show(
        string title,
        string message,
        string soundId,
        bool bypassDoNotDisturb,
        bool persistUntilDismissed = false)
    {
        try
        {
            ShowCore(title, message, soundId, bypassDoNotDisturb, persistUntilDismissed);
        }
        catch (Exception ex)
        {
            // Never take down the tray app over a toast failure.
            try
            {
                MessageBox.Show(
                    $"Bildirim gösterilemedi:\n{ex.Message}",
                    "Notifyer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch
            {
                // ignore UI failures
            }
        }
    }

    private static void ShowCore(
        string title,
        string message,
        string soundId,
        bool bypassDoNotDisturb,
        bool persistUntilDismissed)
    {
        var sound = ToastSounds.Resolve(soundId);

        // IMPORTANT: never use alarm / incomingCall scenarios for eye-rest toasts.
        // Those stay on screen, loop audio, and can steal focus from fullscreen video.
        // Play any selected system sound once (Loop=false), even Alarm/Call catalog entries.
        var builder = new ToastContentBuilder()
            .AddText(title)
            .AddText(message);

        if (persistUntilDismissed)
        {
            // Reminder stays on-screen until the user acts — requires at least one button.
            builder
                .SetToastScenario(ToastScenario.Reminder)
                .AddButton(new ToastButton()
                    .SetContent("Tamam")
                    .AddArgument("action", "dismiss")
                    .SetBackgroundActivation());
        }

        if (sound.Id == ToastSounds.Off || sound.Uri is null)
        {
            builder.AddAudio(new ToastAudio { Silent = true });
        }
        else
        {
            builder.AddAudio(new ToastAudio
            {
                Src = new Uri(sound.Uri),
                Loop = false
            });
        }

        var xml = builder.GetToastContent().GetXml();

        // Eye-rest: "urgent" can break through DND without the modal reminder UX.
        // Persistent phone-check already uses scenario=reminder; don't overwrite it.
        if (!persistUntilDismissed && bypassDoNotDisturb)
            xml.DocumentElement?.SetAttribute("scenario", "urgent");

        var toast = new ToastNotification(xml)
        {
            ExpirationTime = persistUntilDismissed
                ? DateTimeOffset.Now.AddHours(12)
                : DateTimeOffset.Now.AddSeconds(8),
            Priority = bypassDoNotDisturb || persistUntilDismissed
                ? ToastNotificationPriority.High
                : ToastNotificationPriority.Default
        };

        ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
    }
}
