using System.Text;
using System.Diagnostics;

namespace MyTodoApp
{
    public static class AppLogger
    {
        // ログファイルの保存場所
        private static readonly string _logDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "MyTodoApp",
                "Logs");

        // 情報ログ
        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        // エラーログ
        public static void Error(string message)
        {
            WriteLog("ERROR", message);
        }

        // ログの書き込み
        private static void WriteLog(
            string level,
            string message)
        {
            try
            {
                // 保存先フォルダがなければ作成
                Directory.CreateDirectory(_logDirectory);

                // ファイル名を作成
                string fileName =
                    $"app_{DateTime.Now:yyyyMMdd}.log";

                string logPath =
                    Path.Combine(_logDirectory, fileName);

                // ログの内容を作成
                string logMessage =
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}[{level}]{message}";

                // ファイルに追記
                File.AppendAllText(
                    logPath,
                    logMessage + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch (Exception ex) when (
                ex is IOException or
                UnauthorizedAccessException or
                System.Security.SecurityException)
            {
                // デバッグ用の出力にエラーを表示
                Trace.WriteLine(
                    $"ログの書き込みに失敗しました：{ex.Message}");
            }
        }
    }
}