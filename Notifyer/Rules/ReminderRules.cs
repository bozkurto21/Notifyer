using System.Diagnostics;

namespace Notifyer.Rules;

public interface IReminderRule
{
    string Name { get; }
}

/// <summary>
/// Blocks reminders while any configured process is running.
/// When a due reminder was deferred, fire as soon as quiet ends.
/// </summary>
public sealed class QuietProcessRule
{
    public bool IsQuietActive(IReadOnlyList<string> processNames)
    {
        if (processNames.Count == 0)
            return false;

        var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in processNames)
        {
            var name = entry.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? entry[..^4]
                : entry;
            wanted.Add(name);
        }

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (wanted.Contains(process.ProcessName))
                    return true;
            }
            catch
            {
                // Process may have exited; ignore.
            }
            finally
            {
                process.Dispose();
            }
        }

        return false;
    }
}

/// <summary>
/// When enabled, uses a long one-shot interval; after the next fire it turns itself off.
/// </summary>
public sealed class FilmModeRule
{
    public bool Enabled { get; private set; }

    public void SetEnabled(bool enabled) => Enabled = enabled;

    public void ConsumeAfterFire() => Enabled = false;
}
