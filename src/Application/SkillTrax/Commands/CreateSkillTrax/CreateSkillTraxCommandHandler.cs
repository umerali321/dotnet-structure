using FluentValidation;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;
using SkillsetsBackend.Application.SkillTrax.DTOs;
using SkillsetsBackend.Application.SkillTrax.Interfaces;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;

namespace SkillsetsBackend.Application.SkillTrax.Commands.CreateSkillTrax;

/// <summary>Manager/CompanyAdmin only. Course membership is set once here and never edited
/// afterward (no Edit in the initial release) - see SkillTraxCourse.</summary>
public class CreateSkillTraxCommandHandler
{
    private readonly IValidator<CreateSkillTraxCommand> _validator;
    private readonly ISkillTraxRepository _repository;
    private readonly ISkillTraxQueryService _queryService;
    private readonly ICourseLibraryQueryService _courseLibraryQueryService;
    private readonly IUserDirectory _userDirectory;
    private readonly IPermissionService _permissionService;

    public CreateSkillTraxCommandHandler(
        IValidator<CreateSkillTraxCommand> validator,
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

    public async Task<SkillTraxDto> Handle(CreateSkillTraxCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.SkillTrax.Create, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to create a SkillTrax.");
        }

        await StudentAuthorization.EnsureCanManageCompanyAsync(caller, command.CompanyId, _userDirectory, cancellationToken);

        // SuperAdmin has no Users row of its own (see AGENTS.md) - it acts on behalf of a real
        // Manager/Company Admin at the target company instead (command.ActingAsUserId), so the
        // resulting SkillTrax is indistinguishable from one that person created themselves.
        var creatorUserId = await ActingAsResolver.ResolveCreatorUserIdAsync(
            caller, command.ActingAsUserId, command.CompanyId, _userDirectory, cancellationToken);

        var distinctCourseIds = command.CourseIds.Distinct().ToList();
        var courses = await _courseLibraryQueryService.GetCoursesByIdsAsync(distinctCourseIds, cancellationToken);
        var coursesById = courses.ToDictionary(c => c.CourseId);
        var missing = distinctCourseIds.Where(id => !coursesById.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            throw new AppValidationException(
                [new FluentValidation.Results.ValidationFailure(nameof(command.CourseIds), $"Course(s) not found or inactive: {string.Join(", ", missing)}")]);
        }

        var skillTrax = Domain.Assignments.SkillTrax.Create(creatorUserId, command.CompanyId, command.Name.Trim());
        var skillTraxId = await _repository.CreateAsync(skillTrax, distinctCourseIds, cancellationToken);

        // Re-read via the query service rather than building the DTO by hand here - CreatedByEmail
        // must reflect the real creator (creatorUserId, which is the acting-as Manager/CompanyAdmin
        // when the caller is SuperAdmin), not the caller's own email.
        return await _queryService.GetDetailAsync(skillTraxId, cancellationToken)
            ?? throw new InvalidOperationException("SkillTrax was created but could not be re-read.");
    }
}
