namespace NG.PlexToDiscord.Exceptions;

internal class RecoverableException : ApplicationException
{
    internal RecoverableException()
    {
    }

    internal RecoverableException(string message)
        : base(message)
    {
    }

    internal RecoverableException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
