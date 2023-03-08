using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public class TelemetryChannelMock : ITelemetryChannel
{
    public bool? DeveloperMode { get; set; }
    public string EndpointAddress { get; set; }

    public ConcurrentBag<ITelemetry> SentTelemetry = new ConcurrentBag<ITelemetry>();

    public void Send(ITelemetry item)
    {
        SentTelemetry.Add(item);
    }

    public IEnumerable<EventTelemetry> SentEvents => GetSentTelemetry<EventTelemetry>();

    public IEnumerable<MetricTelemetry> SentMetrics => GetSentTelemetry<MetricTelemetry>();

    public IEnumerable<ExceptionTelemetry> SentExceptions => GetSentTelemetry<ExceptionTelemetry>();

    public IEnumerable<TraceTelemetry> SentTraces => GetSentTelemetry<TraceTelemetry>();

    private IEnumerable<T> GetSentTelemetry<T>() where T : ITelemetry
    {
        return SentTelemetry
        .Where(t => t is T)
        .Cast<T>();
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}