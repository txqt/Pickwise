namespace Pickwise.Services;

public sealed class LocalDiagnosticLog
{
    private readonly string _directory = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Pickwise");

    public string Path => System.IO.Path.Combine(_directory, "diagnostic.log");
    public string CrashPath => System.IO.Path.Combine(_directory, "crash.log");

    public void Info(string message) => Write("INFO", message);
    public void Error(string message, Exception exception) => Write("ERROR", $"{message}{Environment.NewLine}{exception}");
    public void Crash(string message, Exception exception) => Write(CrashPath, "CRASH", $"{message}{Environment.NewLine}{exception}");

    private void Write(string level, string message) => Write(Path, level, message);

    private void Write(string path, string level, string message)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.AppendAllText(path, $"{DateTimeOffset.Now:O} {level} {message}{Environment.NewLine}");
    }
}
