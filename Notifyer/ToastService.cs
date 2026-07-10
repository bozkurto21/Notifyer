using Microsoft.Toolkit.Uwp.Notifications;

namespace Notifyer;

public static class ToastService
{
    public static void Show(string title, string message, bool soundEnabled)
    {
        var builder = new ToastContentBuilder()
            .AddText(title)
            .AddText(message);

        if (!soundEnabled)
            builder.AddAudio(new ToastAudio { Silent = true });

        builder.Show(toast =>
        {
            toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(5);
        });
    }
}
