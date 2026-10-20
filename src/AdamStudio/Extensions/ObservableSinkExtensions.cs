using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace AdamStudio.Extensions
{
    public sealed class ObservableSink : ILogEventSink
    {
        private readonly ObservableCollection<string> mLogLines;
        private readonly LoggingLevelSwitch? mLevelSwitch;
        private readonly LogEventLevel mMinimumLevel;

        public ObservableSink(ObservableCollection<string> logLines, LogEventLevel minimumLevel = LogEventLevel.Information, LoggingLevelSwitch? levelSwitch = null)
        {
            mLogLines = logLines;
            mMinimumLevel = minimumLevel;
            mLevelSwitch = levelSwitch;
        }

        public void Emit(LogEvent logEvent)
        {
            // Учитываем и статический минимум, и переключатель (если есть)
            var effectiveLevel = mLevelSwitch?.MinimumLevel ?? mMinimumLevel;
            if (logEvent.Level < effectiveLevel) return;

            var message = logEvent.RenderMessage();
            var formatted = $"[{logEvent.Timestamp:HH:mm:ss}] [{logEvent.Level}] {message}";

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                mLogLines.Add(formatted);
                if (mLogLines.Count > 1000)
                    mLogLines.RemoveAt(0);
            });
        }
    }

    public static class ObservableSinkExtensions
    {
        /// <summary>
        /// Регистрирует ObservableSink. Уровень можно задать:
        ///   - minimumLevel (статический)
        ///   - levelSwitch (динамический — можно менять в runtime)
        /// </summary>
        public static LoggerConfiguration ObservableSink(
            this LoggerSinkConfiguration sinkConfiguration, ObservableCollection<string> logLines, LogEventLevel minimumLevel = LogEventLevel.Information, LoggingLevelSwitch? levelSwitch = null)
        {
            // Если switch не передан — создаём свой, чтобы уровень можно было
            // менять в runtime через App.OutputPanelLevelSwitch
            var actualSwitch = levelSwitch ?? new LoggingLevelSwitch(minimumLevel);

            // ВАЖНО: передаём ТРИ аргумента — sink, LogEventLevel, LoggingLevelSwitch
            // (не два, иначе компилятор выберет перегрузку с LogEventLevel вторым параметром)
            return sinkConfiguration.Sink(
                new ObservableSink(logLines, minimumLevel, actualSwitch),
                minimumLevel,        // ← второй аргумент: LogEventLevel
                actualSwitch);       // ← третий аргумент: LoggingLevelSwitch
        }
    }
}
