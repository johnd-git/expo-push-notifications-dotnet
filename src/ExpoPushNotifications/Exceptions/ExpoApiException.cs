using System.Net;

namespace ExpoPushNotifications.Exceptions;

/// <summary>
/// Exception thrown when the Expo API returns an error response.
/// </summary>
public class ExpoApiException : ExpoException
{
    /// <summary>
    /// The HTTP status code returned by the API.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// The error code returned by the API, if available.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Additional error data returned by the API.
    /// </summary>
    public object? ErrorData { get; }

    /// <summary>
    /// The raw response text from the API.
    /// </summary>
    public string? ResponseText { get; }

    /// <summary>
    /// Additional errors returned in the same response.
    /// </summary>
    public IReadOnlyList<ExpoApiException>? OtherErrors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpoApiException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errorCode">The error code from the API.</param>
    /// <param name="errorData">Additional error data.</param>
    /// <param name="responseText">The raw response text.</param>
    /// <param name="otherErrors">Additional errors from the response.</param>
    public ExpoApiException(
        string message,
        HttpStatusCode statusCode,
        string? errorCode = null,
        object? errorData = null,
        string? responseText = null,
        IReadOnlyList<ExpoApiException>? otherErrors = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ErrorData = errorData;
        ResponseText = responseText;
        OtherErrors = otherErrors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpoApiException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="innerException">The inner exception.</param>
    public ExpoApiException(
        string message,
        HttpStatusCode statusCode,
        Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Returns true if this is a rate limit error (HTTP 429).
    /// </summary>
    public bool IsRateLimitError => StatusCode == HttpStatusCode.TooManyRequests;

    /// <summary>
    /// Returns true if this is an authentication error (HTTP 401).
    /// </summary>
    public bool IsAuthenticationError => StatusCode == HttpStatusCode.Unauthorized;
}
