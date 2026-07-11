using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace Notifyer;

public static class ToastService
{
    public static void Show(string title, string message, string soundId, bool bypassDoNotDisturb)
    {
        try
        {
            ShowCore(title, message, soundId, bypassDoNotDisturb);
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

    private static void ShowCore(string title, string message, string soundId, bool bypassDoNotDisturb)
    {
        var sound = ToastSounds.Resolve(soundId);

        // IMPORTANT: never use alarm / incomingCall scenarios for eye-rest toasts.
        // Those stay on screen, loop audio, and can steal focus from fullscreen video.
        // Play any selected system sound once (Loop=false), even Alarm/Call catalog entries.
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
                Loop = false
            });
        }

        var xml = builder.GetToastContent().GetXml();

        // "urgent" can break through Do Not Disturb without the modal alarm UX.
        if (bypassDoNotDisturb)
            xml.DocumentElement?.SetAttribute("scenario", "urgent");

        var toast = new ToastNotification(xml)
        {
            ExpirationTime = DateTimeOffset.Now.AddSeconds(8),
            Priority = bypassDoNotDisturb
                ? ToastNotificationPriority.High
                : ToastNotificationPriority.Default
        };

        ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
    }
}
