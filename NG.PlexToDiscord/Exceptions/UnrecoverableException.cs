namespace NG.PlexToDiscord.Exceptions;

internal class UnrecoverableException : ApplicationException
{
    internal UnrecoverableException()
    {
    }

    internal UnrecoverableException(string message)
        : base(message)
    {
    }

    internal UnrecoverableException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
