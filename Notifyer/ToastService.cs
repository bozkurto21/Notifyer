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
        var isLoopingSound = sound.Uri?.Contains(".Looping.", StringComparison.Ordinal) == true;

        // Windows only allows looping audio with alarm / incomingCall scenarios.
        // Mixing Loop=true with "urgent" or default throws (unhandled on tray click).
        string? scenario = null;
        if (isLoopingSound)
        {
            scenario = sound.Id.StartsWith("call", StringComparison.OrdinalIgnoreCase)
                ? "incomingCall"
                : "alarm";
        }
        else if (bypassDoNotDisturb)
        {
            scenario = "urgent";
        }

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
                Loop = isLoopingSound
            });
        }

        if (scenario is "alarm" or "incomingCall")
        {
            builder.AddButton(new ToastButton()
                .SetContent("Tamam")
                .SetDismissActivation());
        }

        var xml = builder.GetToastContent().GetXml();
        if (scenario is not null)
            xml.DocumentElement?.SetAttribute("scenario", scenario);

        var toast = new ToastNotification(xml)
        {
            ExpirationTime = DateTimeOffset.Now.AddMinutes(5),
            Priority = bypassDoNotDisturb || scenario is not null
                ? ToastNotificationPriority.High
                : ToastNotificationPriority.Default
        };

        // Ensure COM/AUMID registration (same path ToastContentBuilder.Show uses).
        ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
    }
}
