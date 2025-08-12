using System;
using System.IO;
using System.Text;

namespace Calcultor_API.Utils
{
    public class ServerLog
    {
        private static readonly object _lock = new();
        private static string? _logFilePath;

        public static bool WithTimestamp { get; set; } = true;

        public static void Init(string? filePath = null, bool append = true)
        {
            lock (_lock)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(filePath))
                        filePath = Path.Combine(AppContext.BaseDirectory, "logs", "server_log.txt");

                    var dir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);

                    // 존재 확인만으로도 경로 유효성 체크
                    if (!File.Exists(filePath))
                        File.WriteAllText(filePath, "", Encoding.UTF8);

                    _logFilePath = filePath;

                    // 첫 줄로 init 로그 남기기
                    Write($"[INIT] {DateTime.Now:yyyy-MM-dd HH:mm:ss} log file: {_logFilePath}");
                }
                catch (Exception ex)
                {
                    // 파일 생성 실패해도 서버는 동작해야 하므로 콘솔만 출력
                    Console.WriteLine($"[ServerLog INIT ERROR] {ex.Message}");
                    _logFilePath = null;
                }
            }
        }

        static string Now() => WithTimestamp ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " : "";
        private static void Write(string text)
        {
            lock (_lock)
            {
                try
                {
                    Console.WriteLine(text);
                    if (!string.IsNullOrEmpty(_logFilePath))
                        File.AppendAllText(_logFilePath!, Now() + text + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ServerLog WRITE ERROR] {ex.Message}");
                }
            }
        }

        private static string OpPretty(string op) =>
            op switch { "*" => "×", "/" => "÷", "-" => "−", _ => op };

        public static void Request(string op, string num1, string num2)
        {
            var log = $"요청 :\n{{\n" +
                      $"op = \"{OpPretty(op)}\"\n" +
                      $"num1 = \"{num1}\"\n" +
                      $"num2 = \"{num2}\"\n}}\n";
            Write(log);
        }

        public static void Response(string result)
        {
            var log = $"응답 :\n{{\nresult = \"{result}\"\n}}\n";
            Write(log);
        }

        public static void Error(string error)
        {
            var log = $"응답 :\n{{\nerror = \"{error}\"\n}}\n";
            Write(log);
        }

        public static void Error(string error, int? status = null)
        {
            var s = status.HasValue ? $" (HTTP {status})" : "";
            Write($"응답 :\n{{\n  error = \"{error}\"{(s == "" ? "" : $"\n  status = \"{status}\"")}\n}}");
        }

        public static void Meta(string url, int statusCode, string? contentType)
        {
            Write($"META : url={url}, status={statusCode}, contentType={contentType}");
        }
    }
}

