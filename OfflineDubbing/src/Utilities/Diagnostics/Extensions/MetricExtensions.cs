using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.Utilities.Diagnostics.Extensions
{
    public static class MetricExtensions
    {
        public static bool TrackValue(this Metric metric, double metricValue, List<string> dimensionValues)
        {
            switch (dimensionValues.Count)
            {
                case 10:
                    return metric.TrackValue(
                        metricValue,
                        dimensionValues[0],
                        dimensionValues[1],
                        dimensionValues[2],
                        dimensionValues[3],
                        dimensionValues[4],
                        dimensionValues[5],
                        dimensionValues[6],
                        dimensionValues[7],
                        dimensionValues[8],
                        dimensionValues[9]);
                case 9:
                    return metric.TrackValue(
                        metricValue,
                        dimensionValues[0],
                        dimensionValues[1],
                        dimensionValues[2],
                        dimensionValues[3],
                        dimensionValues[4],
                        dimensionValues[5],
                        dimensionValues[6],
                        dimensionValues[7],
                        dimensionValues[8]);
                case 8:
                    return metric.TrackValue(
                        metricValue,
                        dimensionValues[0],
                        dimensionValues[1],
                        dimensionValues[2],
                        dimensionValues[3],
                        dimensionValues[4],
                        dimensionValues[5],
                        dimensionValues[6],
                        dimensionValues[7]);
                case 7:
                    return metric.TrackValue(
                        metricValue,
                        dimensionValues[0],
                        dimensionValues[1],
                        dimensionValues[2],
                        dimensionValues[3],
                        dimensionValues[4],
                        dimensionValues[5],
                        dimensionValues[6]);
                case 6:
                    return metric.TrackValue(
                        metricValue,
                        dimensionValues[0],
                        dimensionValues[1],
                        dimensionValues[2],
                        dimensionValues[3],
                        dimensionValues[4],
                        dimensionValues[5]);
                case 5:
                    return metric.TrackValue(
                        metricValue,
                        dimensionValues[0],
                        dimensionValues[1],
                        dimensionValues[2],
                        dimensionValues[3],
                        dimensionValues[4]);
                case 4:
                    return metric.TrackValue(metricValue, dimensionValues[0], dimensionValues[1], dimensionValues[2], dimensionValues[3]);
                case 3:
                    return metric.TrackValue(metricValue, dimensionValues[0], dimensionValues[1], dimensionValues[2]);
                case 2:
                    return metric.TrackValue(metricValue, dimensionValues[0], dimensionValues[1]);
                case 1:
                    return metric.TrackValue(metricValue, dimensionValues[0]);
                default:
                    throw new ArgumentOutOfRangeException("Number of dimension values must be between 1 and 10.");
            }
        }
    }
}
