using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace AIPlatform.TestingFramework.Utilities.Diagnostics.Extensions
{
    /// <summary>
    /// Defines convenient overloads for calling the context methods, for all the contexts.
    /// </summary>
    public static class DurableContextExtensions
    {
        /// <summary>
        /// Returns an instance of IOrchestratorLogger that is replay safe, ensuring the logger logs and sends telemetry only when the orchestrator
        /// is not replaying that line of code.
        /// </summary>
        /// <param name="context">The context object.</param>
        /// <param name="logger">An instance of IOrchestratorLogger.</param>
        /// <returns>An instance of a replay safe IOrchestratorLogger.</returns>
        public static IOrchestratorLogger<TCategoryName> CreateReplaySafeLogger<TCategoryName>(this IDurableOrchestrationContext context, IOrchestratorLogger<TCategoryName> logger)
        {
            return new ReplaySafeOrchestratorLogger<TCategoryName>(context, logger);
        }
    }
}