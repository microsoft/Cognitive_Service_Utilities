using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.Utilities.Diagnostics
{
    public interface IOrchestratorLogger<out TCategoryName> : IOrchestratorLogger
    {
    }

    public interface IOrchestratorLogger : ILogger
    {
        /// <summary>
        ///  Send an EventTelemetry for display in Diagnostic Search and in the Analytics Portal.
        ///  Optionally log to console as well.
        /// </summary>
        /// <param name="eventName">A name for the event.</param>
        /// <param name="properties">Named string values you can use to search and classify events.</param>
        /// <param name="metrics">Measurements associated with this event.</param>
        /// <param name="logToConsole">Whether to also log this event to console. False by default</param>
        public void LogEvent(string eventName, IDictionary<string, string> properties = default, IDictionary<string, double> metrics = default, bool logToConsole = false);

        /// <summary>
        ///  Send an ExceptionTelemetry for display in Diagnostic Search.
        ///  Optionally log to console as well.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="properties">Named string values you can use to classify and search for this exception.</param>
        /// <param name="metrics">Additional values associated with this exception.</param>
        /// <param name="logToConsole">Whether to also log this exception to console. False by default</param>
        public void LogException(Exception exception, IDictionary<string, string> properties = default, IDictionary<string, double> metrics = default, bool logToConsole = false);

        /// <summary>
        ///  Track the specified metric with the specified dimensions and dimension values.
        ///  An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        ///  Optionally log to console as well.
        /// </summary>
        /// <param name="metricNamespace">The namespace of the metric.</param>
        /// <param name="metricId">The ID (name) of the metric.</param>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="logToConsole">Whether to also log this metric to console. False by default</param>
        public void LogMetric(string metricNamespace, string metricId, double metricValue, bool logToConsole = false);

        /// <summary>
        ///  Track the specified metric with the specified dimensions and dimension values.
        ///  An aggregate representing tracked values will be automatically sent to the cloud ingestion endpoint at the end of each aggregation period.
        ///  Optionally log to console as well.
        /// </summary>
        /// <param name="metricNamespace">The namespace of the metric.</param>
        /// <param name="metricId">The ID (name) of the metric.</param>
        /// <param name="metricValue">The value to be aggregated.</param>
        /// <param name="dimensions">Name-value pairs for the metric dimensions.</param>
        /// <param name="logToConsole">Whether to also log this metric to console. False by default</param>
        /// <returns>True if the specified value was added to the MetricSeries indicated by the specified dimension name;
        /// False if the indicated series could not be created because a dimension cap or a metric series cap was reached.</returns>
        public bool LogMetric(string metricNamespace, string metricId, double metricValue, IDictionary<string, string> dimensions = default, bool logToConsole = false);

        /// <summary>
        /// Adds a global property to the orchestrator logger.
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        public void AddGlobalProperty(string key, string value);
        
        /// <summary>
        /// Adds a set of global properties to the orchestrator logger.
        /// </summary>
        /// <param name="keyValues"> Name-value pair of the new properties.</param>
        public void AddGlobalProperties(IDictionary<string, string> keyValues);

        /// <summary>
        /// Boolean method to check if a global property with the specified key is present.
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Referenced value object</param>
        /// <returns>True if a global property value is found for a specified key, false otherwise.</returns>
        public bool TryGetPropertyValue(string key, out string value);

        /// <summary>
        /// Tracker method to clock the runtime of a callback function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventName">Event name</param>
        /// <param name="callback">Callback function</param>
        /// <param name="logToConsole">Boolean indicating whether to also log to console</param>
        /// <returns></returns>
        public T TrackDuration<T>(string eventName, Func<T> callback, bool logToConsole = false);

        /// <summary>
        /// Tracker method to clock the runtime of a callback action.
        /// </summary>
        /// <param name="eventName">Event name</param>
        /// <param name="callback">Callback action</param>
        /// <param name="logToConsole">Boolean indicating whether to also log to console</param>
        public void TrackDuration(string eventName, Action callback, bool logToConsole = false);

        /// <summary>
        /// Logs confusion matrix to console
        /// </summary>
        /// <param name="eventName">Event name</param>
        /// <param name="matrix">Matrix to be logged</param>
        public void LogConfusionMatrix<T>(string eventName, T[,] matrix);

        /// <summary>
        ///  Flushes the in-memory buffer for telemetry and any metrics being pre-aggregated.
        /// </summary>
        public void FlushTelemetry();
    }
}
