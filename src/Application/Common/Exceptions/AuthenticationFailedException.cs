namespace SkillsetsBackend.Application.Common.Exceptions;

public class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message)
        : base(message)
    {
    }
}
