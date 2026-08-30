using System.Net;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SkillsetsBackend.Application.Assignments.DTOs;
using SkillsetsBackend.Application.Assignments.Interfaces;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;
using SkillsetsBackend.Application.Notifications;
using SkillsetsBackend.Application.Students;
using SkillsetsBackend.Application.Students.Interfaces;
using SkillsetsBackend.Domain.Assignments;
using SkillsetsBackend.Domain.Identity;
using AppValidationException = SkillsetsBackend.Application.Common.Exceptions.ValidationException;
using NotFoundException = SkillsetsBackend.Application.Common.Exceptions.NotFoundException;

namespace SkillsetsBackend.Application.Assignments.Commands.CreateAssignment;

/// <summary>Manager/CompanyAdmin only. Two-phase confirm for the duplicate/overlap safeguard
/// (blueprint #13): a first submit that finds overlaps returns warnings and creates nothing; the
/// caller resubmits with ConfirmDespiteWarnings=true to proceed anyway. Assignment notification
/// emails are sent best-effort - a delivery failure (e.g. SMTP not yet configured) never fails the
/// assignment itself, since the assignment is already durably created by that point.</summary>
public class CreateAssignmentCommandHandler
{
    private readonly IValidator<CreateAssignmentCommand> _validator;
    private readonly IAssignmentRepository _repository;
    private readonly IAssignmentQueryService _queryService;
    private readonly SkillsetsBackend.Application.SkillTrax.Interfaces.ISkillTraxQueryService _skillTraxQueryService;
    private readonly ICourseLibraryQueryService _courseLibraryQueryService;
    private readonly IUserDirectory _userDirectory;
    private readonly IStudentRepository _studentRepository;
    private readonly IPermissionService _permissionService;
    private readonly NotificationDispatcher _notifications;
    private readonly ILogger<CreateAssignmentCommandHandler> _logger;

    public CreateAssignmentCommandHandler(
        IValidator<CreateAssignmentCommand> validator,
        IAssignmentRepository repository,
        IAssignmentQueryService queryService,
        SkillsetsBackend.Application.SkillTrax.Interfaces.ISkillTraxQueryService skillTraxQueryService,
        ICourseLibraryQueryService courseLibraryQueryService,
        IUserDirectory userDirectory,
        IStudentRepository studentRepository,
        IPermissionService permissionService,
        NotificationDispatcher notifications,
        ILogger<CreateAssignmentCommandHandler> logger)
    {
        _validator = validator;
        _repository = repository;
        _queryService = queryService;
        _skillTraxQueryService = skillTraxQueryService;
        _courseLibraryQueryService = courseLibraryQueryService;
        _userDirectory = userDirectory;
        _studentRepository = studentRepository;
        _permissionService = permissionService;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<CreateAssignmentResultDto> Handle(CreateAssignmentCommand command, CallerContext caller, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors);
        }

        if (!caller.IsSuperAdmin && !await _permissionService.HasPermissionAsync(caller, Permissions.Assignments.Create, cancellationToken))
        {
            throw new UnauthorizedAccessException("You do not have permission to assign training.");
        }

        await StudentAuthorization.EnsureCanManageCompanyAsync(caller, command.CompanyId, _userDirectory, cancellationToken);

        // SuperAdmin has no Users row of its own (see AGENTS.md) - it acts on behalf of a real
        // Manager/Company Admin at the target company instead (command.ActingAsUserId), so the
        // resulting assignment shows up on that person's own side exactly as if they made it.
        var creatorUserId = await ActingAsResolver.ResolveCreatorUserIdAsync(
            caller, command.ActingAsUserId, command.CompanyId, _userDirectory, cancellationToken);

        var sourceType = Enum.Parse<AssignmentSourceType>(command.SourceType);
        List<long> courseIds;
        int? sourceSkillTraxId = null;

        if (sourceType == AssignmentSourceType.SingleCourse)
        {
            var courses = await _courseLibraryQueryService.GetCoursesByIdsAsync([command.CourseId!.Value], cancellationToken);
            if (courses.Count == 0)
            {
                throw new NotFoundException("Course", command.CourseId.Value);
            }

            courseIds = [command.CourseId.Value];
        }
        else
        {
            var skillTrax = await _skillTraxQueryService.GetDetailAsync(command.SkillTraxId!.Value, cancellationToken)
                ?? throw new NotFoundException("SkillTrax", command.SkillTraxId.Value);

            if (skillTrax.CompanyId != command.CompanyId)
            {
                throw new AppValidationException(
                    [new FluentValidation.Results.ValidationFailure(nameof(command.SkillTraxId), "That SkillTrax belongs to a different company.")]);
            }

            sourceSkillTraxId = skillTrax.SkillTraxId;
            courseIds = skillTrax.Courses.Select(c => c.CourseId).ToList();
        }

        var distinctEmployeeIds = command.EmployeeUserIds.Distinct().ToList();
        foreach (var employeeId in distinctEmployeeIds)
        {
            await StudentAuthorization.EnsureCanManageStudentAsync(caller, employeeId, _userDirectory, _studentRepository, cancellationToken);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overlaps = await _repository.FindActiveOverlapsAsync(distinctEmployeeIds, courseIds, today, cancellationToken);
        if (overlaps.Count > 0 && !command.ConfirmDespiteWarnings)
        {
            var warnings = overlaps
                .Select(o => $"{o.StudentName} already has \"{o.CourseTitle}\" in an active or scheduled assignment.")
                .ToList();
            return new CreateAssignmentResultDto(null, warnings);
        }

        var assignment = Assignment.Create(creatorUserId, command.CompanyId, sourceType, sourceSkillTraxId, command.StartDate);
        var assignmentId = await _repository.CreateAsync(assignment, distinctEmployeeIds, courseIds, cancellationToken);

        await SendAssignmentEmailsAsync(distinctEmployeeIds, assignment, courseIds, cancellationToken);

        var dto = await _queryService.GetDtoAsync(assignmentId, cancellationToken);
        return new CreateAssignmentResultDto(dto, []);
    }

    /// <summary>Sent only after the assignment is durably created, and only to the employees on it.
    /// Best-effort: SMTP isn't configured in every environment, and a delivery problem must never
    /// roll back an assignment that already exists.</summary>
    private async Task SendAssignmentEmailsAsync(
        IReadOnlyList<int> employeeIds,
        Assignment assignment,
        IReadOnlyList<long> courseIds,
        CancellationToken cancellationToken)
    {
        // Look the titles up once for the whole batch rather than per recipient - every employee on
        // this assignment is being told about the same courses.
        IReadOnlyList<string> courseTitles;
        try
        {
            var courses = await _courseLibraryQueryService.GetCoursesByIdsAsync(courseIds, cancellationToken);
            courseTitles = courses.Select(c => c.CourseTitle).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load course titles for assignment {AssignmentId} notification", assignment.AssignmentId);
            courseTitles = [];
        }

        foreach (var employeeId in employeeIds)
        {
            try
            {
                var user = await _studentRepository.GetUserAsync(employeeId, cancellationToken);
                if (user?.Email is null)
                {
                    continue;
                }

                var roles = await _userDirectory.GetActiveCompanyRolesAsync(employeeId, cancellationToken);
                var companyName = roles.FirstOrDefault(r => r.CompanyId == assignment.CompanyId)?.CompanyName ?? "your company";

                await _notifications.SendAssignmentAsync(
                    new AssignmentNotification(
                        user.Email,
                        user.FirstName,
                        companyName,
                        courseTitles,
                        assignment.StartDate,
                        assignment.EndDate),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send assignment notification email to employee {EmployeeId}", employeeId);
            }
        }
    }
}
