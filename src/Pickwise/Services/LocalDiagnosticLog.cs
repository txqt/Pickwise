namespace Pickwise.Services;

public sealed class LocalDiagnosticLog
{
    private static readonly object Gate = new();
    private readonly string _directory = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Pickwise");

    public string Path => System.IO.Path.Combine(_directory, "diagnostic.log");
    public string CrashPath => System.IO.Path.Combine(_directory, "crash.log");

    public void Info(string message)
    {
        if (!IsInfoEnabled(Environment.GetEnvironmentVariable("PICKWISE_DIAGNOSTIC_INFO")))
        {
            return;
        }

        Write("INFO", message);
    }

    public void Error(string message, Exception exception) => Write("ERROR", $"{message}{Environment.NewLine}{exception}");
    public void Crash(string message, Exception exception) => Write(CrashPath, "CRASH", $"{message}{Environment.NewLine}{exception}");

    public static bool IsInfoEnabled(string? value) =>
        string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);

    private void Write(string level, string message) => Write(Path, level, message);

    private void Write(string path, string level, string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                File.AppendAllText(path, $"{DateTimeOffset.Now:O} {level} {message}{Environment.NewLine}");
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
