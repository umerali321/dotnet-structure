using System.Net;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SkillsetsBackend.Application.Assignments.DTOs;
using SkillsetsBackend.Application.Assignments.Interfaces;
using SkillsetsBackend.Application.Auth.Interfaces;
using SkillsetsBackend.Application.Common;
using SkillsetsBackend.Application.CourseLibrary.Interfaces;
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
    private readonly IEmailSender _emailSender;
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
        IEmailSender emailSender,
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
        _emailSender = emailSender;
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

        await SendAssignmentEmailsAsync(distinctEmployeeIds, assignment, cancellationToken);

        var dto = await _queryService.GetDtoAsync(assignmentId, cancellationToken);
        return new CreateAssignmentResultDto(dto, []);
    }

    private async Task SendAssignmentEmailsAsync(IReadOnlyList<int> employeeIds, Assignment assignment, CancellationToken cancellationToken)
    {
        foreach (var employeeId in employeeIds)
        {
            try
            {
                var user = await _studentRepository.GetUserAsync(employeeId, cancellationToken);
                if (user?.Email is null)
                {
                    continue;
                }

                await _emailSender.SendAsync(
                    user.Email,
                    user.FirstName,
                    "New training assigned to you",
                    BuildAssignmentEmailBody(user.FirstName ?? "there", assignment),
                    purpose: "AssignmentCreated",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                // Best-effort - the assignment itself is already durably created by this point, and
                // SMTP isn't configured in every environment yet (see EmailSettings). A delivery
                // failure here must never roll back or fail the assignment.
                _logger.LogWarning(ex, "Failed to send assignment notification email to employee {EmployeeId}", employeeId);
            }
        }
    }

    private static string BuildAssignmentEmailBody(string firstName, Assignment assignment) => $$"""
        <div style="background-color:#f4f4f5;padding:32px 16px;font-family:'Segoe UI',Helvetica,Arial,sans-serif;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e7e5e3;">
            <tr>
              <td style="background:#c81322;padding:22px 28px;">
                <div style="color:#ffffff;font-size:18px;font-weight:700;letter-spacing:0.5px;">SKILLSETS</div>
                <div style="color:#ffd9dc;font-size:12px;margin-top:4px;">New training assigned</div>
              </td>
            </tr>
            <tr>
              <td style="padding:28px;">
                <p style="margin:0 0 16px;color:#1a1918;font-size:14px;line-height:1.6;">Hi {{WebUtility.HtmlEncode(firstName)}},</p>
                <p style="margin:0 0 20px;color:#1a1918;font-size:14px;line-height:1.6;">
                  New training has been assigned to you. Your 30-day Focus Session window is below - sign in to your
                  dashboard to see the full list of titles and get started.
                </p>
                <div style="background:#f7f6f5;border:1px solid #e7e5e3;border-radius:10px;padding:16px 20px;text-align:center;margin-bottom:20px;">
                  <span style="font-size:14px;color:#1a1918;">{{assignment.StartDate:MMM d, yyyy}} &ndash; {{assignment.EndDate:MMM d, yyyy}}</span>
                </div>
                <p style="margin:0;color:#6b6663;font-size:12px;line-height:1.6;">
                  Sign in to SkillSets and check "My Training" on your dashboard for the full details.
                </p>
              </td>
            </tr>
          </table>
        </div>
        """;
}
