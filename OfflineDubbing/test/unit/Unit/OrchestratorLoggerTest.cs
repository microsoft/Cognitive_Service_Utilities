using AIPlatform.TestingFramework;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Diagnostics.Extensions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace AIPlatform.EvaluationFramework.Test.Unit
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class OrchestratorLoggerTest
    {
        [TestMethod]
        public void LogEventLogsToConsoleAndTracksEvent()
        {
            // Setup
            var telemetryChannelMock = new TelemetryChannelMock();
            var telemetryConfig = new TelemetryConfiguration
            {
                TelemetryChannel = telemetryChannelMock,
                InstrumentationKey = "TestKey",
            };
            var telemetryClient = new TelemetryClient(telemetryConfig);
            var loggerMock = new Mock<ILogger<TestingFrameworkOrchestrator>>();
            var orchestratorLogger = new OrchestratorLogger<TestingFrameworkOrchestrator>(telemetryClient, loggerMock.Object);

            // Run Test
            var eventName = "TestEvent";
            var properties = new Dictionary<string, string>
            {
                { "Property1", "Value1" },
                { "Property2", "Value2" }
            };
            var metrics = new Dictionary<string, double>
            {
                { "Metric1", 1 },
                { "Metric2", 2 }
            };
            orchestratorLogger.LogEvent(eventName, properties, metrics, true);

            // Validate expected event was sent with the right arguments
            var strComparer = StringComparer.Create(CultureInfo.InvariantCulture, false);
            Assert.AreEqual(1, telemetryChannelMock.SentEvents.Count());
            Assert.AreEqual(eventName, telemetryChannelMock.SentEvents.FirstOrDefault().Name);
            Assert.IsTrue(properties.IsEqualToDictionary(telemetryChannelMock.SentEvents.FirstOrDefault().Properties, strComparer));
            Assert.IsTrue(metrics.IsEqualToDictionary(telemetryChannelMock.SentEvents.FirstOrDefault().Metrics));

            // Validate Log was called with the right arguments
            var expectedString = "LogType: Event, eventName: TestEvent, properties: {Property1: Value1, Property2: Value2}, " +
                "metrics: {Metric1: 1, Metric2: 2}";
            loggerMock.Verify(mock =>
                mock.Log(
                    LogLevel.Information,
                    0,
                    It.Is<It.IsAnyType>((@object, _) => @object.ToString() == expectedString),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once());
        }

        [TestMethod]
        public void LogExceptionLogsToConsoleAndTracksException()
        {
            // Setup
            var telemetryChannelMock = new TelemetryChannelMock();
            var telemetryConfig = new TelemetryConfiguration
            {
                TelemetryChannel = telemetryChannelMock,
                InstrumentationKey = "TestKey",
            };
            var telemetryClient = new TelemetryClient(telemetryConfig);
            var loggerMock = new Mock<ILogger<TestingFrameworkOrchestrator>>();
            var orchestratorLogger = new OrchestratorLogger<TestingFrameworkOrchestrator>(telemetryClient, loggerMock.Object);

            // Run Test
            var properties = new Dictionary<string, string>
            {
                { "Property1", "Value1" },
                { "Property2", "Value2" }
            };
            var metrics = new Dictionary<string, double>
            {
                { "Metric1", 1 },
                { "Metric2", 2 }
            };
            var exceptionMessage = "TestException";
            orchestratorLogger.LogException(new Exception(exceptionMessage), properties, metrics, true);

            // Validate expected exception was sent with the right arguments
            var strComparer = StringComparer.Create(CultureInfo.InvariantCulture, false);
            Assert.AreEqual(1, telemetryChannelMock.SentExceptions.Count());
            Assert.AreEqual(exceptionMessage, telemetryChannelMock.SentExceptions.FirstOrDefault().Exception.Message);
            Assert.IsTrue(properties.IsEqualToDictionary(telemetryChannelMock.SentExceptions.FirstOrDefault().Properties, strComparer));
            Assert.IsTrue(metrics.IsEqualToDictionary(telemetryChannelMock.SentExceptions.FirstOrDefault().Metrics));

            // Validate Log was called with the right arguments
            var expectedString = "LogType: Exception, Message: TestException, properties: {Property1: Value1, Property2: Value2}, " +
                "metrics: {Metric1: 1, Metric2: 2}";
            loggerMock.Verify(mock =>
                mock.Log(
                    LogLevel.Error,
                    0,
                    It.Is<It.IsAnyType>((@object, _) => @object.ToString() == expectedString),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once());
        }

        [TestMethod]
        public void LogMetricLogsToConsoleAndTracksMetricValue()
        {
            // Setup
            var telemetryChannelMock = new TelemetryChannelMock();
            var telemetryConfig = new TelemetryConfiguration
            {
                TelemetryChannel = telemetryChannelMock,
                InstrumentationKey = "TestKey",
            };
            var telemetryClient = new Microsoft.ApplicationInsights.TelemetryClient(telemetryConfig);
            var loggerMock = new Mock<ILogger<TestingFrameworkOrchestrator>>();
            var orchestratorLogger = new OrchestratorLogger<TestingFrameworkOrchestrator>(telemetryClient, loggerMock.Object);

            // Run Test
            var metricNamespace = "testNamespace";
            var metricId = "testMetricId";
            var metrics = new List<int> { 5, 0, 10 };
            foreach (var metric in metrics)
            {
                orchestratorLogger.LogMetric(metricNamespace, metricId, metric, true);
            }
            orchestratorLogger.FlushTelemetry();

            // Validate metric was sent with the expected params and aggregated data            
            Assert.AreEqual(metricNamespace, telemetryChannelMock.SentMetrics.FirstOrDefault().MetricNamespace);
            Assert.AreEqual(metricId, telemetryChannelMock.SentMetrics.FirstOrDefault().Name);
            Assert.AreEqual(metrics.Count, telemetryChannelMock.SentMetrics.FirstOrDefault().Count);
            Assert.AreEqual(metrics.Max(), telemetryChannelMock.SentMetrics.FirstOrDefault().Max);
            Assert.AreEqual(metrics.Min(), telemetryChannelMock.SentMetrics.FirstOrDefault().Min);
            Assert.AreEqual(metrics.Sum(), telemetryChannelMock.SentMetrics.FirstOrDefault().Sum);

            // Validate Log was called with the right arguments
            var expectedString = "LogType: Metric, metricNamespace: testNamespace, metricId: testMetricId, metricValue: 5";
            loggerMock.Verify(mock =>
                mock.Log(
                    LogLevel.Information,
                    0,
                    It.Is<It.IsAnyType>((@object, _) => @object.ToString() == expectedString),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once());
        }

        [TestMethod]
        public void LogMetricWithDimensionsLogsToConsoleAndTracksMetricValue()
        {
            // Setup
            var telemetryChannelMock = new TelemetryChannelMock();
            var telemetryConfig = new TelemetryConfiguration
            {
                TelemetryChannel = telemetryChannelMock,
                InstrumentationKey = "TestKey",
            };
            var telemetryClient = new Microsoft.ApplicationInsights.TelemetryClient(telemetryConfig);
            var loggerMock = new Mock<ILogger<TestingFrameworkOrchestrator>>();
            var orchestratorLogger = new OrchestratorLogger<TestingFrameworkOrchestrator>(telemetryClient, loggerMock.Object);

            // Run Test
            var metricNamespace = "testNamespace";
            var metricId = "testMetricId";
            var metrics = new List<int> { 5, 0, 10 };
            var dimensions = new Dictionary<string, string>
            {
                { "Dimension1", "Value1" },
                { "Dimension2", "Value2" }
            };
            
            foreach (var metric in metrics)
            {
                orchestratorLogger.LogMetric(metricNamespace, metricId, metric, dimensions, true);
            }
            orchestratorLogger.FlushTelemetry();

            // Validate metric was sent with the expected params and aggregated data
            Assert.AreEqual(metricNamespace, telemetryChannelMock.SentMetrics.FirstOrDefault().MetricNamespace);
            Assert.AreEqual(metricId, telemetryChannelMock.SentMetrics.FirstOrDefault().Name);
            Assert.AreEqual(metrics.Count, telemetryChannelMock.SentMetrics.FirstOrDefault().Count);            
            Assert.AreEqual(metrics.Max(), telemetryChannelMock.SentMetrics.FirstOrDefault().Max);
            Assert.AreEqual(metrics.Min(), telemetryChannelMock.SentMetrics.FirstOrDefault().Min);
            Assert.AreEqual(metrics.Sum(), telemetryChannelMock.SentMetrics.FirstOrDefault().Sum);

            // Validate Log was called with the right arguments
            var expectedString = "LogType: Metric, metricNamespace: testNamespace, metricId: testMetricId, metricValue: 5, " +
                "dimensions: {Dimension1: Value1, Dimension2: Value2}";
            loggerMock.Verify(mock =>
                mock.Log(
                    LogLevel.Information,
                    0,
                    It.Is<It.IsAnyType>((@object, _) => @object.ToString() == expectedString),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once());
        }
    }
}
