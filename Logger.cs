using System;
using System.IO;
using Serilog;

namespace BatchImageCropper
{
    public static class Logger
    {
        static Logger()
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "batchimagecropper-.txt");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(logPath, 
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();
        }

        public static void Information(string message, params object[] args)
        {
            Log.Information(message, args);
        }

        public static void Warning(string message, params object[] args)
        {
            Log.Warning(message, args);
        }

        public static void Error(string message, params object[] args)
        {
            Log.Error(message, args);
        }

        public static void Error(Exception ex, string message, params object[] args)
        {
            Log.Error(ex, message, args);
        }

        public static void Debug(string message, params object[] args)
        {
            Log.Debug(message, args);
        }

        public static void CloseAndFlush()
        {
            Log.CloseAndFlush();
        }
    }
}
