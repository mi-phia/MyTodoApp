using System.Text;
public static class AppLogger
{
    private static readonly string _logDirectory =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "MyTodoApp",
            "Logs");

    public static void Info(string message)
    {
        WriteLog("INFO", message);
    }

    public static void Error(string message)
    {
        WriteLog("ERROR", message);
    }
    private static void WriteLog(string level, string message)
    {
        Directory.CreateDirectory(_logDirectory);

        string fileName = $"app_{DateTime.Now:yyyyMMdd}.log";

        string logPath = Path.Combine(_logDirectory, fileName);

        string logMessage = $"{DateTime.Now:yyyy--MM--dd HH:mm:ss}[{level}]{message}";

        File.AppendAllText(
            logPath,
            logMessage + Environment.NewLine,
            Encoding.UTF8);
    }
}