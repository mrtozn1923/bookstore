using Anthropic.Exceptions;
using Anthropic.Models;

namespace Bookstore.Api.Ai;

public enum ClaudeFailureKind
{
    InvalidRequest,
    Unauthorized,
    SpendCapReached,
    RateLimited,
    Overloaded,
    ServerError,
    Network,
    StreamInterrupted,
    Unexpected,
}

public sealed record ClaudeFailure(ClaudeFailureKind Kind, bool RetryLater, int? StatusCode, string? RequestId, string? ErrorCode);

public static class ClaudeFailureClassifier
{
    // Most specific types first: the spend-cap and ramp-limit exceptions derive from AnthropicRateLimitException.
    public static ClaudeFailure Classify(Exception exception) => exception switch
    {
        AnthropicEnforcedSpendLimitReachedException e => From(e, ClaudeFailureKind.SpendCapReached, retryLater: false),
        AnthropicRateLimitException e => From(e, ClaudeFailureKind.RateLimited, retryLater: true),
        AnthropicOverloadedException e => From(e, ClaudeFailureKind.Overloaded, retryLater: true),
        Anthropic5xxException e => From(e, ClaudeFailureKind.ServerError, retryLater: true),
        AnthropicUnauthorizedException or AnthropicForbiddenException => From((AnthropicApiException)exception, ClaudeFailureKind.Unauthorized, retryLater: false),
        Anthropic4xxException e => From(e, ClaudeFailureKind.InvalidRequest, retryLater: false),
        AnthropicSseException => new(ClaudeFailureKind.StreamInterrupted, RetryLater: true, null, null, null),
        AnthropicIOException => new(ClaudeFailureKind.Network, RetryLater: true, null, null, null),
        _ => new(ClaudeFailureKind.Unexpected, RetryLater: false, null, null, null),
    };

    private static ClaudeFailure From(AnthropicApiException e, ClaudeFailureKind kind, bool retryLater) =>
        new(kind, retryLater, (int)e.StatusCode, e.RequestID, e.ErrorCode);
}
