namespace SkillsetsBackend.Application.Common.Exceptions;

/// <summary>A request conflicts with the current state of a resource (e.g. an exclusivity rule) -
/// distinct from ValidationException (bad input) and NotFoundException (missing resource).</summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
