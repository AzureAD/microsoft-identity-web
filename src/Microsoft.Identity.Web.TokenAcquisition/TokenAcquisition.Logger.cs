// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Identity.Web
{
    /// <summary>
    /// LoggingMessage class for TokenAcquisition.
    /// </summary>
    /*
     * Used by Microsoft.Identity.Web
     * Any changes to this member (including removal) can cause runtime failures.
     * Treat as a public member.
     */
    internal partial class TokenAcquisition
    {
        internal static class Logger
        {
            private static readonly Action<ILogger, string, Exception?> s_tokenAcquisitionError =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    LoggingEventId.TokenAcquisitionError,
                    "[MsIdWeb] An error occured during token acquisition: {MsalErrorMessage}");

            private static readonly Action<ILogger, long, long, long, string, string, string, Exception?> s_tokenAcquisitionMsalAuthenticationResultTime =
                LoggerMessage.Define<long, long, long, string, string, string>(
                    LogLevel.Debug,
                    LoggingEventId.TokenAcquisitionMsalAuthenticationResultTime,
                    "[MsIdWeb] Time to get token with MSAL: " +
                    "DurationTotalInMs: {DurationTotalInMs} " +
                    "DurationInHttpInMs: {DurationInHttpInMs} " +
                    "DurationInCacheInMs: {DurationInCacheInMs} " +
                    "TokenSource: {TokenSource} " +
                    "CorrelationId: {CorrelationId} " +
                    "CacheRefreshReason: {CacheRefreshReason} ");

            /// <summary>
            /// Logger for handling MSAL exceptions in TokenAcquisition.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="msalErrorMessage">Specific log message from TokenAcquisition.</param>
            /// <param name="ex">Exception from MSAL.NET.</param>
            public static void TokenAcquisitionError(
                ILogger logger,
                string msalErrorMessage,
                Exception? ex)
            {
                s_tokenAcquisitionError(logger, msalErrorMessage, ex);
            }

            /// <summary>
            /// Logger for handling information specific to MSAL in token acquisition.
            /// </summary>
            /// <param name="logger">ILogger.</param>
            /// <param name="durationTotalInMs">durationTotalInMs.</param>
            /// <param name="durationInHttpInMs">durationInHttpInMs.</param>
            /// <param name="durationInCacheInMs">durationInCacheInMs.</param>
            /// <param name="tokenSource">cache or IDP.</param>
            /// <param name="correlationId">correlationId.</param>
            /// <param name="cacheRefreshReason">cacheRefreshReason.</param>
            /// <param name="ex">Exception from MSAL.NET.</param>
            public static void TokenAcquisitionMsalAuthenticationResultTime(
                ILogger logger,
                long durationTotalInMs,
                long durationInHttpInMs,
                long durationInCacheInMs,
                string tokenSource,
                string correlationId,
                string cacheRefreshReason,
                Exception? ex)
            {
                s_tokenAcquisitionMsalAuthenticationResultTime(
                    logger,
                    durationTotalInMs,
                    durationInHttpInMs,
                    durationInCacheInMs,
                    tokenSource,
                    correlationId,
                    cacheRefreshReason,
                    ex);
            }

            // --- Agent User FIC logging ---

            private static readonly Action<ILogger, string, string, Exception?> s_agentUserFicFlowDetected =
                LoggerMessage.Define<string, string>(
                    LogLevel.Information,
                    LoggingEventId.AgentUserFicFlowDetected,
                    "[MsIdWeb] Agent User FIC flow detected for agent '{AgentAppId}' with user identifier type '{IdentifierType}'.");

            private static readonly Action<ILogger, string, string, Exception?> s_agentUserFicSilentSuccess =
                LoggerMessage.Define<string, string>(
                    LogLevel.Debug,
                    LoggingEventId.AgentUserFicSilentSuccess,
                    "[MsIdWeb] Agent User FIC silent token acquisition succeeded for agent '{AgentAppId}' in tenant '{TenantId}'.");

            private static readonly Action<ILogger, string, string, string, Exception?> s_agentUserFicSilentFailure =
                LoggerMessage.Define<string, string, string>(
                    LogLevel.Information,
                    LoggingEventId.AgentUserFicSilentFailure,
                    "[MsIdWeb] Agent User FIC silent token acquisition failed for agent '{AgentAppId}' in tenant '{TenantId}': {Reason}. Falling back to 3-leg acquisition.");

            private static readonly Action<ILogger, string, string, Exception?> s_agentUserFicAcquisitionComplete =
                LoggerMessage.Define<string, string>(
                    LogLevel.Information,
                    LoggingEventId.AgentUserFicAcquisitionComplete,
                    "[MsIdWeb] Agent User FIC 3-leg acquisition complete for agent '{AgentAppId}', token source: {TokenSource}.");

            private static readonly Action<ILogger, string, Exception?> s_agentCcaCreated =
                LoggerMessage.Define<string>(
                    LogLevel.Information,
                    LoggingEventId.AgentCcaCreated,
                    "[MsIdWeb] Created new agent CCA for cache key '{CcaCacheKey}'.");

            private static readonly Action<ILogger, int, int, Exception?> s_agentCcaEviction =
                LoggerMessage.Define<int, int>(
                    LogLevel.Information,
                    LoggingEventId.AgentCcaEviction,
                    "[MsIdWeb] Agent CCA cache cleared {EvictedCount} entries (exceeded size threshold). Remaining: {RemainingCount}.");

            public static void AgentUserFicFlowDetected(ILogger logger, string agentAppId, string identifierType)
                => s_agentUserFicFlowDetected(logger, agentAppId, identifierType, null);

            public static void AgentUserFicSilentSuccess(ILogger logger, string agentAppId, string tenantId)
                => s_agentUserFicSilentSuccess(logger, agentAppId, tenantId, null);

            public static void AgentUserFicSilentFailure(ILogger logger, string agentAppId, string tenantId, string reason, Exception? ex)
                => s_agentUserFicSilentFailure(logger, agentAppId, tenantId, reason, ex);

            public static void AgentUserFicAcquisitionComplete(ILogger logger, string agentAppId, string tokenSource)
                => s_agentUserFicAcquisitionComplete(logger, agentAppId, tokenSource, null);

            public static void AgentCcaCreated(ILogger logger, string ccaCacheKey)
                => s_agentCcaCreated(logger, ccaCacheKey, null);

            public static void AgentCcaEviction(ILogger logger, int evictedCount, int remainingCount)
                => s_agentCcaEviction(logger, evictedCount, remainingCount, null);
        }
    }
}
