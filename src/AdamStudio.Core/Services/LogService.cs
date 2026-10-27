using Serilog;
using Serilog.Events;
using System.Collections.ObjectModel;

namespace AdamStudio.Core.Services
{
    public interface ILogService
    {
        ObservableCollection<string> LogLines { get; }

        void Log(string message, LogEventLevel level = LogEventLevel.Information);
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? ex = null);
        void LogDebug(string message);
    }

    public interface ILogService<T> : ILogService
    {
        string Category { get; }
    }

    public sealed class LogService : ILogService
    {
        private readonly ILogger mLogger;

        public ObservableCollection<string> LogLines { get; }

        public LogService(ILogger logger, ObservableCollection<string> logLines)
        {
            mLogger = logger;
            LogLines = logLines;
        }

        public void Log(string message, LogEventLevel level = LogEventLevel.Information) => mLogger.Write(level, message);
        public void LogInformation(string message) => mLogger.Information(message);
        public void LogWarning(string message) => mLogger.Warning(message);
        public void LogError(string message, Exception? ex = null)
        {
            if (ex is null) 
                mLogger.Error(message);
            else 
                mLogger.Error(ex, message);
        }
        public void LogDebug(string message) => mLogger.Debug(message);
    }

    public sealed class LogService<T> : ILogService<T>
    {
        private readonly ILogger mLogger;
        public string Category => typeof(T).Name;

        public LogService(ILogger logger, ILogService baseLogService)
        {
            mLogger = logger.ForContext<T>();
            LogLines = baseLogService.LogLines;
        }

        public ObservableCollection<string> LogLines { get; }

        public void Log(string message, LogEventLevel level = LogEventLevel.Information) => mLogger.Write(level, message);
        public void LogInformation(string message) => mLogger.Information(message);
        public void LogWarning(string message) => mLogger.Warning(message);
        public void LogError(string message, Exception? ex = null)
        {
            if (ex is null) 
                mLogger.Error(message);
            else 
                mLogger.Error(ex, message);
        }
        public void LogDebug(string message) => mLogger.Debug(message);
    }
}
