
using AIPlatform.TestingFramework.Utilities.Diagnostics.Extensions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Metrics;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.Utilities.Diagnostics
{
    public class OrchestratorLogger<TCategoryName> : IOrchestratorLogger<TCategoryName>
    {
        protected readonly TelemetryClient telemetry;
        protected readonly ILogger<TCategoryName> logger;

        public OrchestratorLogger(TelemetryClient telemetry, ILogger<TCategoryName> logger)
        {
            this.telemetry = telemetry;
            this.logger = logger;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(logLevel, eventId, state, exception, formatter);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logger.IsEnabled(logLevel);
        }

        public void LogEvent(string eventName, IDictionary<string, string> properties = default, IDictionary<string, double> metrics = default, bool logToConsole = false)
        {
            telemetry.TrackEvent(eventName, properties, metrics);

            var allProperties = properties != default
                ? properties.Union(telemetry.Context.GlobalProperties).ToDictionary(k => k.Key, v => v.Value)
                : telemetry.Context.GlobalProperties;

            if (logToConsole)
            {
                LogEventToConsole(
                    eventName,
                    allProperties,
                    metrics);
            }
        }

        public void LogException(Exception exception, IDictionary<string, string> properties = default, IDictionary<string, double> metrics = default, bool logToConsole = false)
        {
            telemetry.TrackException(exception, properties, metrics);

            var allProperties = properties != default
                ? properties.Union(telemetry.Context.GlobalProperties).ToDictionary(k => k.Key, v => v.Value)
                : telemetry.Context.GlobalProperties;           

            if (logToConsole)
            {
                LogExceptionToConsole(
                    exception,
                    allProperties,
                    metrics);
            }
        }

        public void LogMetric(string metricNamespace, string metricId, double metricValue, bool logToConsole = false)
        {
            var metricIdentifier = new MetricIdentifier(metricNamespace, metricId);
            telemetry.GetMetric(metricIdentifier).TrackValue(metricValue);

            if (logToConsole)
            {
                LogMetricToConsole(metricNamespace, metricId, metricValue);
            }
        }

        public bool LogMetric(string metricNamespace, string metricId, double metricValue, IDictionary<string, string> dimensions = default, bool logToConsole = false)
        {
            var dimensionNames = dimensions.Keys.ToList();
            var dimensionValues = dimensions.Values.ToList();

            var metricIdentifier = new MetricIdentifier(metricNamespace, metricId, dimensionNames);
            var valueAdded = telemetry.GetMetric(metricIdentifier).TrackValue(metricValue, dimensionValues);

            if (valueAdded && logToConsole)
            {
                LogMetricToConsole(metricNamespace, metricId, metricValue, dimensions);
            }
            else if (!valueAdded)
            {
                this.LogWarning($"Unable to add metric value {metricValue} to the metric series indicated by namespace {metricNamespace}, id {metricId}, and dimensions {dimensions.ToConsoleString()}. This could be because a dimension cap was reached.");
            }

            return valueAdded;
        }

        public void AddGlobalProperty(string key, string value)
        {   
            if (!telemetry.Context.GlobalProperties.ContainsKey(key))
            {
                telemetry.Context.GlobalProperties.Add(key, default(String));
            }

            telemetry.Context.GlobalProperties[key] = value;
        }

        public void AddGlobalProperties(IDictionary<string, string> keyValues)
        {
            foreach (KeyValuePair<string, string> kvp in keyValues)
            {
                this.AddGlobalProperty(kvp.Key, kvp.Value);
            }
        }

        public T TrackDuration<T>(string eventName, Func<T> callback, bool logToConsole = false)
        {
            Dictionary<string, double> metrics = new Dictionary<string, double>();

            Stopwatch stopwatch = Stopwatch.StartNew();
            T returnedValue = (T)callback.Invoke();
            var duration = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();

            metrics.Add(eventName, duration);
            LogEvent(eventName, null, metrics, logToConsole);
            return returnedValue;
        }

        public void TrackDuration(string eventName, Action callback, bool logToConsole = false)
        {
            Dictionary<string, double> metrics = new Dictionary<string, double>();

            Stopwatch stopwatch = Stopwatch.StartNew();
            callback.Invoke();
            var duration = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();

            metrics.Add(eventName, duration);
            LogEvent(eventName, null, metrics, logToConsole);
        }

        public async Task TrackDurationAsync(string eventName, Func<Task> callback, bool logToConsole = false)
        {
            Dictionary<string, double> metrics = new Dictionary<string, double>();

            Stopwatch stopwatch = Stopwatch.StartNew();
            await callback.Invoke();
            var duration = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();

            metrics.Add(eventName, duration);
            LogEvent(eventName, null, metrics, logToConsole);
        }

        public void LogConfusionMatrix<T>(string eventName, T[,] matrix)
        {
            var vals = new Dictionary<string, object>().AddIfNotNull(nameof(eventName), eventName);
            LogToConsole(ITelemetryType.Event, LogLevel.Information, vals);

            for (int rowIter = 0; rowIter < matrix.GetLength(0); rowIter++)
            {
                for (int colIter = 0; colIter < matrix.GetLength(1); colIter++)
                {
                    Console.Write(matrix[rowIter, colIter] + "\t");
                }
                Console.WriteLine();
            }

        }

        public bool TryGetPropertyValue(string key, out string value)
        {
            return telemetry.Context.GlobalProperties.TryGetValue(key, out value);
        }

        public void FlushTelemetry()
        {
            telemetry.Flush();
        }

        private void LogExceptionToConsole(Exception exception, IDictionary<string, string> properties = default, IDictionary<string, double> metrics = default)
        {
            var vals = new Dictionary<string, object>()
                .AddIfNotNull(nameof(exception.Message), exception.Message)
                .AddIfNotNull(nameof(exception.Source), exception.Source)
                .AddIfNotNull(nameof(exception.StackTrace), exception.StackTrace)
                .AddIfNotNull(nameof(exception.TargetSite), exception.TargetSite)
                .AddIfNotNull(nameof(exception.InnerException), exception.InnerException)
                .AddDictionaryStringIfNotNullOrEmpty(nameof(properties), properties)
                .AddDictionaryStringIfNotNullOrEmpty(nameof(metrics), metrics);

            LogToConsole(ITelemetryType.Exception, LogLevel.Error, vals);
        }

        private void LogEventToConsole(string eventName, IDictionary<string, string> properties = default, IDictionary<string, double> metrics = default)
        {
            var vals = new Dictionary<string, object>()
                .AddIfNotNull(nameof(eventName), eventName)
                .AddDictionaryStringIfNotNullOrEmpty(nameof(properties), properties)
                .AddDictionaryStringIfNotNullOrEmpty(nameof(metrics), metrics);

            LogToConsole(ITelemetryType.Event, LogLevel.Information, vals);
        }

        private void LogMetricToConsole(string metricNamespace,
            string metricId,
            double metricValue,
            IDictionary<string, string> dimensions = default)
        {
            var vals = new Dictionary<string, object>()
                .AddIfNotNull(nameof(metricNamespace), metricNamespace)
                .AddIfNotNull(nameof(metricId), metricId)
                .AddIfNotNull(nameof(metricValue), metricValue)
                .AddDictionaryStringIfNotNullOrEmpty(nameof(dimensions), dimensions);

            LogToConsole(ITelemetryType.Metric, LogLevel.Information, vals);
        }

        private void LogToConsole(ITelemetryType type, LogLevel logLevel, IDictionary<string, object> values)
        {
            var log = $"LogType: {Enum.GetName(typeof(ITelemetryType), type)}";

            foreach(var title in values.Keys)
            {
                if (values.TryGetValue(title, out var value))
                {
                    log += $", {title}: {value}";
                }
            }

            this.Log(logLevel, log);
        }
    }

    public class ReplaySafeOrchestratorLogger<TCategoryName> : IOrchestratorLogger<TCategoryName>
    {
        protected readonly IOrchestratorLogger<TCategoryName> logger;
        protected readonly IDurableOrchestrationContext context;

        public ReplaySafeOrchestratorLogger(IDurableOrchestrationContext context, IOrchestratorLogger<TCategoryName> logger)
        {
            this.logger = logger;
            this.context = context;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return logger.BeginScope(state);
        }
        
        public bool IsEnabled(LogLevel logLevel)
        {
            return logger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!context.IsReplaying)
            {
                logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }

        public void AddGlobalProperty(string key, string value)
        {
            if (!context.IsReplaying)
            {
                logger.AddGlobalProperty(key, value);
            }
        }

        public void AddGlobalProperties(IDictionary<string, string> keyValues)
        {
            if (!context.IsReplaying)
            {
                logger.AddGlobalProperties(keyValues);
            }
        }

        public bool TryGetPropertyValue(string key, out string value)
        {
            return logger.TryGetPropertyValue(key, out value);
        }

        public T TrackDuration<T>(string eventName, Func<T> callback, bool logToConsole = false)
        {
            if (!context.IsReplaying)
            {
                return logger.TrackDuration(eventName, callback, logToConsole);
            }
            return default(T);
        }

        public void TrackDuration(string eventName, Action callback, bool logToConsole = false)
        {
            if (!context.IsReplaying)
            {
                logger.TrackDuration(eventName, callback, logToConsole);
            }
        }

        public void LogConfusionMatrix<T>(string eventName, T[,] matrix)
        {
            if (!context.IsReplaying)
            {
                logger.LogConfusionMatrix(eventName, matrix);
            }
        }

        public void FlushTelemetry()
        {
            logger.FlushTelemetry();
        }

        public void LogEvent(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null, bool logToConsole = false)
        {
            if (!context.IsReplaying)
            {
                logger.LogEvent(eventName, properties, metrics, logToConsole);
            }
        }

        public void LogException(Exception exception, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null, bool logToConsole = false)
        {
            if (!context.IsReplaying)
            {
                logger.LogException(exception, properties, metrics, logToConsole);
            }
        }

        public void LogMetric(string metricNamespace, string metricId, double metricValue, bool logToConsole = false)
        {
            if (!context.IsReplaying)
            {
                logger.LogMetric(metricNamespace, metricId, metricValue, logToConsole);
            }
        }

        public bool LogMetric(string metricNamespace, string metricId, double metricValue, IDictionary<string, string> dimensions = null, bool logToConsole = false)
        {
            if (!context.IsReplaying)
            {
                return logger.LogMetric(metricNamespace, metricId, metricValue, dimensions, logToConsole);
            }

            return false;
        }
    }

}
