namespace ExpoPushNotifications.Exceptions;

/// <summary>
/// Base exception for all Expo SDK errors.
/// </summary>
public class ExpoException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpoException"/> class.
    /// </summary>
    public ExpoException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpoException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ExpoException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpoException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ExpoException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
