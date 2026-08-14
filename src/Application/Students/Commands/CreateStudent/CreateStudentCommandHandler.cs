using FluentValidation;
using FluentValidation.Results;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.Skillsoft.Interfaces;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.Students.Commands.CreateStudent;

/// <summary>SkillportProvisioned means the Skillport account was created - the 30-day session itself stays dormant until the student first enters the course library.</summary>
public record CreateStudentResult(int UserId, bool SkillportRequested, bool SkillportProvisioned, string? SkillportError);

public class CreateStudentCommandHandler
{
    private readonly IValidator<CreateStudentCommand> _validator;
    private readonly IStudentRepository _repository;
    private readonly IUserDirectory _userDirectory;
    private readonly ISkillportSessionService _skillportSessionService;

    public CreateStudentCommandHandler(
        IValidator<CreateStudentCommand> validator,
        IStudentRepository repository,
        IUserDirectory userDirectory,
        ISkillportSessionService skillportSessionService)
    {
        _validator = validator;
        _repository = repository;
        _userDirectory = userDirectory;
        _skillportSessionService = skillportSessionService;
    }

    public async Task<CreateStudentResult> Handle(CreateStudentCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (!caller.IsSuperAdmin && caller.Role != Roles.Manager)
        {
            throw new UnauthorizedAccessException("Only SuperAdmin and company managers can create students.");
        }

        if (!caller.IsSuperAdmin)
        {
            await StudentAuthorization.EnsureCanManageCompanyAsync(caller, command.CompanyId, _userDirectory, cancellationToken);
        }

        if (await _repository.IdentifierInUseAsync(command.Email, command.Username, excludeUserId: null, cancellationToken))
        {
            throw new AppValidationException(
            [
                new ValidationFailure(nameof(command.Username), "Email or username is already in use."),
            ]);
        }

        var user = AppUser.CreateStudent(command.Email, command.Phone, command.FirstName, command.LastName, command.Username, command.Password);

        var userId = await _repository.CreateStudentAsync(
            user, command.StudentType, caller.Email, command.CompanyId, command.StartDate, cancellationToken);

        if (!command.CreateInSkillport)
        {
            return new CreateStudentResult(userId, SkillportRequested: false, SkillportProvisioned: false, SkillportError: null);
        }

        // Best-effort: the student account is already created above regardless of what happens here.
        // This only creates the Skillport account (dormant) - the session activates on their first visit.
        try
        {
            var provisionResult = await _skillportSessionService.EnsureDormantAccountAsync(userId, command.CompanyId, cancellationToken);

            return new CreateStudentResult(userId, SkillportRequested: true, provisionResult.Success, provisionResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            return new CreateStudentResult(userId, SkillportRequested: true, SkillportProvisioned: false, ex.Message);
        }
    }
}
