namespace Notifyer;

internal static class Program
{
    private const string MutexName = "Local\\NotifyerSingleInstance";

    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
            return;

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        var config = ConfigStore.Load();
        Application.Run(new TrayApp(config));
    }
}
