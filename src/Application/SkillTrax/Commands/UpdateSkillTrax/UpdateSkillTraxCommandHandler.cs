using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;
using SkillsetsBackend.Application.SkillTrax.DTOs;
using SkillsetsBackend.Application.SkillTrax.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.SkillTrax.Commands.UpdateSkillTrax;

/// <summary>Manager/CompanyAdmin only. Reuses Permissions.SkillTrax.Create (editing a SkillTrax
/// you're already allowed to create is the same authority). Safe to allow unconditionally - see
/// the SkillTrax entity's own doc comment for why this never touches historical assignment data.</summary>
public class UpdateSkillTraxCommandHandler
{
    private readonly IValidator<UpdateSkillTraxCommand> _validator;
    private readonly ISkillTraxRepository _repository;
    private readonly ISkillTraxQueryService _queryService;
    private readonly ICourseLibraryQueryService _courseLibraryQueryService;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public UpdateSkillTraxCommandHandler(
        IValidator<UpdateSkillTraxCommand> validator,
        ISkillTraxRepository repository,
        ISkillTraxQueryService queryService,
        ICourseLibraryQueryService courseLibraryQueryService,
        IUserDirectory userDirectory,
        IPermissionService permissionService)
    {
        _validator = validator;
        _repository = repository;
        _queryService = queryService;
        _courseLibraryQueryService = courseLibraryQueryService;
        _userDirectory = userDirectory;
        _permissionService = permissionService;
    }

    public async Task<SkillTraxDto> Handle(int skillTraxId, UpdateSkillTraxCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.SkillTrax.Create, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to edit a SkillTrax.");
        }

        var skillTrax = await _repository.GetByIdAsync(skillTraxId, cancellationToken)
            ?? throw new NotFoundException("SkillTrax", skillTraxId);

        await StudentAuthorization.EnsureCanManageCompanyAsync(caller, skillTrax.CompanyId, _userDirectory, cancellationToken);

        var distinctCourseIds = command.CourseIds.Distinct().ToList();
        var courses = await _courseLibraryQueryService.GetCoursesByIdsAsync(distinctCourseIds, cancellationToken);
        var missing = distinctCourseIds.Where(id => !courses.Any(c => c.CourseId == id)).ToList();
        if (missing.Count > 0)
        {
            throw new AppValidationException(
                [new FluentValidation.Results.ValidationFailure(nameof(command.CourseIds), $"Course(s) not found or inactive: {string.Join(", ", missing)}")]);
        }

        skillTrax.Rename(command.Name.Trim());
        await _repository.UpdateAsync(skillTrax, distinctCourseIds, cancellationToken);

        return await _queryService.GetDetailAsync(skillTraxId, cancellationToken)
            ?? throw new InvalidOperationException("SkillTrax was updated but could not be re-read.");
    }
}
